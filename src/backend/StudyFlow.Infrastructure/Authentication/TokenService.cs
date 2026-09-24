using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Authentication;

internal sealed class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("session_version", user.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
        };
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: expiresAt.UtcDateTime, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string Token, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (token, Hash(token), DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays));
    }

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
