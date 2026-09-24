using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class User : BaseEntity
{
    public void UpdatePublicIdentity(string displayName, string? avatarUrl)
    {
        DisplayName = displayName.Trim(); AvatarUrl = avatarUrl; UpdatedAt = DateTimeOffset.UtcNow;
    }
    private User() { }

    private User(string email, string displayName)
    {
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
        DisplayName = displayName;
    }

    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string TimeZoneId { get; private set; } = "UTC";
    public string? AvatarUrl { get; private set; }
    public string? RefreshTokenHash { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationCodeHash { get; private set; }
    public DateTimeOffset? EmailVerificationExpiresAt { get; private set; }
    public DateTimeOffset? EmailVerificationLastSentAt { get; private set; }
    public int EmailVerificationFailedAttempts { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;
    public bool IsSuspended { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTimeOffset? SuspendedAt { get; private set; }
    public DateTimeOffset? LastActiveAt { get; private set; }
    public int SessionVersion { get; private set; }

    public static User Create(string email, string displayName) => new(email.Trim().ToLowerInvariant(), displayName.Trim());

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public void PrepareEmailVerification(string codeHash, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        EmailVerificationCodeHash = codeHash;
        EmailVerificationExpiresAt = expiresAt;
        EmailVerificationLastSentAt = now;
        EmailVerificationFailedAttempts = 0;
        UpdatedAt = now;
    }
    public void RecordFailedEmailVerification(DateTimeOffset now) { EmailVerificationFailedAttempts++; UpdatedAt = now; }
    public void VerifyEmail(DateTimeOffset now)
    {
        IsEmailVerified = true;
        EmailVerificationCodeHash = null;
        EmailVerificationExpiresAt = null;
        EmailVerificationFailedAttempts = 0;
        UpdatedAt = now;
    }
    public void TrustExistingEmail() { IsEmailVerified = true; }
    public void SetTimeZone(string timeZoneId, DateTimeOffset now) { TimeZoneId = timeZoneId; UpdatedAt = now; }

    public void SetRefreshToken(string tokenHash, DateTimeOffset expiresAt)
    {
        RefreshTokenHash = tokenHash;
        RefreshTokenExpiresAt = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool HasValidRefreshToken(string tokenHash, DateTimeOffset now) =>
        RefreshTokenHash == tokenHash && RefreshTokenExpiresAt > now;

    public void RevokeRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkActive(DateTimeOffset now)
    {
        LastActiveAt = now;
    }

    public void Suspend(string reason, DateTimeOffset now)
    {
        IsSuspended = true;
        SuspensionReason = reason.Trim();
        SuspendedAt = now;
        RevokeAllSessions(now);
    }

    public void Activate(DateTimeOffset now)
    {
        IsSuspended = false;
        SuspensionReason = null;
        SuspendedAt = null;
        SessionVersion++;
        UpdatedAt = now;
    }

    public void SetModerationSuspension(bool suspended, string reason, DateTimeOffset now)
    {
        IsSuspended = suspended;
        SuspensionReason = suspended ? reason.Trim() : null;
        SuspendedAt = suspended ? now : null;
        UpdatedAt = now;
    }

    public void ChangeRole(UserRole role, DateTimeOffset now)
    {
        if (Role == role) return;
        Role = role;
        RevokeAllSessions(now);
    }

    public void RevokeAllSessions(DateTimeOffset now)
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAt = null;
        SessionVersion++;
        UpdatedAt = now;
    }
}
