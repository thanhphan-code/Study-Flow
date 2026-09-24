using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Auth.Interfaces;
using StudyFlow.Application.Common.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace StudyFlow.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> Register(RegisterRequest request, IValidator<RegisterRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await authService.RegisterAsync(request, cancellationToken);
        if (!result.IsSuccess) return ToResponse(result);
        return result.Value!.Auth is null ? Accepted(new RegistrationPendingResponse(result.Value.Email, result.Value.ExpiresAt, result.Value.ResendAfterSeconds)) : WriteAuthResponse(result.Value.Auth);
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, IValidator<VerifyEmailRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return ToAuthResponse(await authService.VerifyEmailAsync(request, cancellationToken));
    }

    [HttpPost("resend-email-otp")]
    [EnableRateLimiting("auth-otp")]
    public async Task<IActionResult> ResendEmailOtp(ResendEmailOtpRequest request, IValidator<ResendEmailOtpRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await authService.ResendEmailOtpAsync(request, cancellationToken);
        return result.IsSuccess ? Accepted(result.Value) : ToResponse(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, IValidator<LoginRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return ToAuthResponse(await authService.LoginAsync(request, cancellationToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue("studyflow_refresh", out var token) || string.IsNullOrWhiteSpace(token))
            return Unauthorized(new ApiError("INVALID_REFRESH_TOKEN", "Refresh token is missing."));
        return ToAuthResponse(await authService.RefreshAsync(token, cancellationToken));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var userId)) return Unauthorized(new ApiError("INVALID_TOKEN", "Access token is invalid."));
        return ToResponse(await authService.GetCurrentUserAsync(userId, cancellationToken));
    }

    [Authorize]
    [HttpPut("timezone")]
    public async Task<IActionResult> UpdateTimeZone(UpdateTimeZoneRequest request, IValidator<UpdateTimeZoneRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var userId)) return Unauthorized(new ApiError("INVALID_TOKEN", "Access token is invalid."));
        return ToResponse(await authService.UpdateTimeZoneAsync(userId, request.TimeZoneId, cancellationToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue("studyflow_refresh", out var token) && !string.IsNullOrWhiteSpace(token))
            await authService.LogoutAsync(token, cancellationToken);
        Response.Cookies.Delete("studyflow_refresh", new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth"
        });
        return NoContent();
    }

    private IActionResult ToAuthResponse(Result<AuthResponse> result)
    {
        if (!result.IsSuccess) return ToResponse(result);
        return WriteAuthResponse(result.Value!);
    }
    private IActionResult WriteAuthResponse(AuthResponse auth)
    {
        Response.Cookies.Append("studyflow_refresh", auth.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/api/auth"
        });
        return Ok(new AuthPayload(auth.User, auth.AccessToken, auth.AccessTokenExpiresAt));
    }

    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var error = result.Error!;
        var body = new ApiError(error.Code, error.Message);
        return error.Type switch
        {
            ErrorType.Unauthorized => Unauthorized(body),
            ErrorType.Conflict => Conflict(body),
            ErrorType.NotFound => NotFound(body),
            _ => BadRequest(body)
        };
    }
}

public sealed record AuthPayload(UserDto User, string AccessToken, DateTimeOffset AccessTokenExpiresAt);
