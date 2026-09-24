using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Common.Models;

namespace StudyFlow.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<Result<RegistrationResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<Result<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);
    Task<Result<RegistrationPendingResponse>> ResendEmailOtpAsync(ResendEmailOtpRequest request, CancellationToken cancellationToken);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<UserDto>> UpdateTimeZoneAsync(Guid userId, string timeZoneId, CancellationToken cancellationToken);
}
