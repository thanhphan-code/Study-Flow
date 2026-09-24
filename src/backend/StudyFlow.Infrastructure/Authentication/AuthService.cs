using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Auth.Interfaces;
using StudyFlow.Application.Common.Models;
using StudyFlow.Domain.Entities;
using StudyFlow.Infrastructure.Persistence;

namespace StudyFlow.Infrastructure.Authentication;

internal sealed class AuthService(StudyFlowDbContext dbContext, IPasswordHasher<User> passwordHasher, TokenService tokenService,
    TimeProvider timeProvider, IEmailVerificationSender emailSender, IOptions<EmailOptions> emailOptions, IOptions<AdminOptions> adminOptions) : IAuthService
{
    private const int OtpLifetimeMinutes = 10;
    private const int ResendCooldownSeconds = 60;
    private const int MaximumAttempts = 5;
    public async Task<Result<RegistrationResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existing = await dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (existing is not null)
            return Result<RegistrationResponse>.Failure("EMAIL_ALREADY_EXISTS", existing.IsEmailVerified ? "Email này đã được đăng ký." : "Email này đang chờ xác minh. Hãy nhập mã OTP hoặc yêu cầu gửi lại.", ErrorType.Conflict);

        var user = User.Create(request.Email, request.DisplayName);
        var now = timeProvider.GetUtcNow();
        user.SetTimeZone(request.TimeZoneId, now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(new UserProfile { UserId = user.Id, Username = "u_" + user.Id.ToString("N")[..28], DisplayName = user.DisplayName });
        if (!emailOptions.Value.RequireVerification)
        {
            user.TrustExistingEmail();
            var auth = await IssueTokensAsync(user, cancellationToken);
            return Result<RegistrationResponse>.Success(new(user.Email, now, 0, auth.Value));
        }
        var pending = await SendCodeAsync(user, now, cancellationToken);
        if (!pending.IsSuccess) return Result<RegistrationResponse>.Failure(pending.Error!.Code, pending.Error.Message, pending.Error.Type);
        return Result<RegistrationResponse>.Success(new(pending.Value!.Email, pending.Value.ExpiresAt, pending.Value.ResendAfterSeconds));
    }

    public async Task<Result<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == request.Email.Trim().ToUpperInvariant(), cancellationToken);
        if (user is null) return Result<AuthResponse>.Failure("INVALID_OTP", "Mã OTP không hợp lệ hoặc đã hết hạn.", ErrorType.Validation);
        if (user.IsEmailVerified) return Result<AuthResponse>.Failure("EMAIL_ALREADY_VERIFIED", "Email đã được xác minh. Bạn có thể đăng nhập.", ErrorType.Conflict);
        if (user.EmailVerificationExpiresAt <= now || user.EmailVerificationCodeHash is null)
            return Result<AuthResponse>.Failure("OTP_EXPIRED", "Mã OTP đã hết hạn. Hãy yêu cầu mã mới.", ErrorType.Validation);
        if (user.EmailVerificationFailedAttempts >= MaximumAttempts)
            return Result<AuthResponse>.Failure("OTP_LOCKED", "Bạn đã nhập sai quá nhiều lần. Hãy yêu cầu mã mới.", ErrorType.Conflict);
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(user.EmailVerificationCodeHash), Convert.FromHexString(HashOtp(user.Email, request.Code))))
        {
            user.RecordFailedEmailVerification(now); await dbContext.SaveChangesAsync(cancellationToken);
            return Result<AuthResponse>.Failure("INVALID_OTP", $"Mã OTP không đúng. Bạn còn {MaximumAttempts - user.EmailVerificationFailedAttempts} lần thử.", ErrorType.Validation);
        }
        user.VerifyEmail(now);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<Result<RegistrationPendingResponse>> ResendEmailOtpAsync(ResendEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == request.Email.Trim().ToUpperInvariant(), cancellationToken);
        // Same outward response for missing/verified addresses prevents account enumeration.
        if (user is null || user.IsEmailVerified) return Result<RegistrationPendingResponse>.Success(new(request.Email.Trim().ToLowerInvariant(), now.AddMinutes(OtpLifetimeMinutes), ResendCooldownSeconds));
        var wait = ResendCooldownSeconds - (int)(now - user.EmailVerificationLastSentAt.GetValueOrDefault(DateTimeOffset.MinValue)).TotalSeconds;
        if (wait > 0) return Result<RegistrationPendingResponse>.Failure("OTP_RESEND_TOO_SOON", $"Vui lòng chờ {wait} giây trước khi gửi lại mã.", ErrorType.Conflict);
        return await SendCodeAsync(user, now, cancellationToken);
    }

    private async Task<Result<RegistrationPendingResponse>> SendCodeAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var expires = now.AddMinutes(OtpLifetimeMinutes);
        user.PrepareEmailVerification(HashOtp(user.Email, code), expires, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        try { await emailSender.SendOtpAsync(user.Email, user.DisplayName, code, OtpLifetimeMinutes, cancellationToken); }
        catch (Exception)
        {
            return Result<RegistrationPendingResponse>.Failure("EMAIL_SEND_FAILED", "Không thể gửi email xác minh. Vui lòng thử lại sau hoặc liên hệ quản trị viên.", ErrorType.Conflict);
        }
        return Result<RegistrationPendingResponse>.Success(new(user.Email, expires, ResendCooldownSeconds));
    }
    private string HashOtp(string email, string code)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(emailOptions.Value.AppPassword.Length > 0 ? emailOptions.Value.AppPassword : "studyflow-test-otp-key"));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{email.ToUpperInvariant()}:{code}")));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is null)
            return Result<AuthResponse>.Failure("INVALID_CREDENTIALS", "Email or password is incorrect.", ErrorType.Unauthorized);
        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return Result<AuthResponse>.Failure("INVALID_CREDENTIALS", "Email or password is incorrect.", ErrorType.Unauthorized);
        if (!user.IsEmailVerified)
            return Result<AuthResponse>.Failure("EMAIL_NOT_VERIFIED", "Email chưa được xác minh. Hãy nhập mã OTP đã gửi đến hộp thư.", ErrorType.Conflict);
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = TokenService.Hash(refreshToken);
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.RefreshTokenHash == hash, cancellationToken);
        if (user is null || !user.IsEmailVerified || !user.HasValidRefreshToken(hash, DateTimeOffset.UtcNow))
            return Result<AuthResponse>.Failure("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.", ErrorType.Unauthorized);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null ? Result<UserDto>.Failure("USER_NOT_FOUND", "User was not found.", ErrorType.NotFound) : Result<UserDto>.Success(ToDto(user));
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = TokenService.Hash(refreshToken);
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.RefreshTokenHash == hash, cancellationToken);
        if (user is null) return;
        user.RevokeRefreshToken();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<UserDto>> UpdateTimeZoneAsync(Guid userId, string timeZoneId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Result<UserDto>.Failure("USER_NOT_FOUND", "User was not found.", ErrorType.NotFound);
        user.SetTimeZone(timeZoneId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<UserDto>.Success(ToDto(user));
    }

    private async Task<Result<AuthResponse>> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        if (adminOptions.Value.IsBootstrapAdmin(user.Email)) user.ChangeRole(StudyFlow.Domain.Enums.UserRole.Admin, timeProvider.GetUtcNow());
        if (user.IsSuspended || await dbContext.UserProfiles.AnyAsync(x => x.UserId == user.Id && x.IsSuspended, cancellationToken))
        {
            user.RevokeRefreshToken();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<AuthResponse>.Failure("SUSPENDED", "Tài khoản đã bị tạm ngưng.", ErrorType.Unauthorized);
        }
        var access = tokenService.CreateAccessToken(user);
        var refresh = tokenService.CreateRefreshToken();
        user.SetRefreshToken(refresh.Hash, refresh.ExpiresAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<AuthResponse>.Success(new AuthResponse(ToDto(user), access.Token, refresh.Token, access.ExpiresAt));
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.TimeZoneId, user.IsEmailVerified, user.Role.ToString(), user.IsSuspended);
}
