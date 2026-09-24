namespace StudyFlow.Application.Auth.DTOs;

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string TimeZoneId = "UTC");
public sealed record RegistrationPendingResponse(string Email, DateTimeOffset ExpiresAt, int ResendAfterSeconds);
public sealed record RegistrationResponse(string Email, DateTimeOffset ExpiresAt, int ResendAfterSeconds, AuthResponse? Auth = null);
public sealed record VerifyEmailRequest(string Email, string Code);
public sealed record ResendEmailOtpRequest(string Email);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record UpdateTimeZoneRequest(string TimeZoneId);
public sealed record UserDto(Guid Id, string Email, string DisplayName, string? AvatarUrl, string TimeZoneId, bool IsEmailVerified);
public sealed record AuthResponse(UserDto User, string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
