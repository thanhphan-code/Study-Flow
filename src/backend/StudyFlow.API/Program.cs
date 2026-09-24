using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StudyFlow.API.Errors;
using StudyFlow.Application;
using StudyFlow.Infrastructure;
using StudyFlow.Infrastructure.Authentication;
using System.Threading.RateLimiting;
using StudyFlow.API.Battles;
using StudyFlow.Application.Battles;
using StudyFlow.API.Social;
using StudyFlow.Application.Social;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISocialNotifier, SocialNotifier>();
builder.Services.AddSingleton<IBattleNotifier, BattleNotifier>();
builder.Services.AddHostedService<BattleWorker>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, token) => await context.HttpContext.Response.WriteAsJsonAsync(
        new ApiError("RATE_LIMITED", "Bạn thao tác quá nhanh. Hãy chờ một phút rồi thử lại."), token);
    options.AddPolicy("battles", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 180, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("social", context => RateLimitPartition.GetFixedWindowLimiter(
        (context.User.FindFirst("sub")?.Value ?? "anonymous") + (HttpMethods.IsGet(context.Request.Method) ? ":read" : ":write"),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = HttpMethods.IsGet(context.Request.Method) ? 180 : 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth-otp", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("admin", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
if (jwt.Key.Length < 32) throw new InvalidOperationException("JWT key must be at least 32 characters.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if ((context.Request.Path.StartsWithSegments("/api/battle-hub") || context.Request.Path.StartsWithSegments("/api/social-hub")) && context.Request.Query.TryGetValue("access_token", out var token)) context.Token = token;
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiError("UNAUTHORIZED", "Authentication is required."));
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var publicAuthEndpoint = context.Request.Path.StartsWithSegments("/api/auth/login") ||
        context.Request.Path.StartsWithSegments("/api/auth/register") ||
        context.Request.Path.StartsWithSegments("/api/auth/verify-email") ||
        context.Request.Path.StartsWithSegments("/api/auth/resend-email-otp") ||
        context.Request.Path.StartsWithSegments("/api/auth/refresh") ||
        context.Request.Path.StartsWithSegments("/api/auth/logout");
    if (!publicAuthEndpoint && Guid.TryParse(context.User.FindFirst("sub")?.Value, out var authenticatedUserId))
    {
        var database = context.RequestServices.GetRequiredService<StudyFlowDbContext>();
        var authenticatedUser = await database.Users.SingleOrDefaultAsync(x => x.Id == authenticatedUserId, context.RequestAborted);
        var tokenVersion = context.User.FindFirst("session_version")?.Value;
        var profileSuspended = await database.UserProfiles.AnyAsync(x => x.UserId == authenticatedUserId && x.IsSuspended, context.RequestAborted);
        if (authenticatedUser?.IsSuspended == true || profileSuspended)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new ApiError("SUSPENDED", "Tài khoản đã bị tạm ngưng."));
            return;
        }
        if (authenticatedUser is null || tokenVersion != authenticatedUser.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new ApiError("SESSION_REVOKED", "Phiên đăng nhập đã bị thu hồi."));
            return;
        }
        var now = context.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();
        if (authenticatedUser.LastActiveAt is null || authenticatedUser.LastActiveAt < now.AddMinutes(-5))
        {
            authenticatedUser.MarkActive(now);
            await database.SaveChangesAsync(context.RequestAborted);
        }
    }
    await next();
});
app.UseAuthorization();
app.UseRateLimiter();
app.MapHub<BattleHub>("/api/battle-hub", options => options.CloseOnAuthenticationExpiration = true);
app.MapHub<SocialHub>("/api/social-hub", options => options.CloseOnAuthenticationExpiration = true).RequireRateLimiting("social");
app.MapControllers();
app.Run();

public partial class Program;
