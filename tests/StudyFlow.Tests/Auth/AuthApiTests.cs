using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Infrastructure.Persistence;
using StudyFlow.Infrastructure.Authentication;
using StudyFlow.Application.Auth.Interfaces;
using Microsoft.Extensions.Options;

namespace StudyFlow.Tests.Auth;

public sealed class AuthApiTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;
    public AuthApiTests(AuthApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Me_WithoutAccessToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@studyflow.test";
        var request = new RegisterRequest(email, "Password1", "Student");
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/auth/register", request)).StatusCode);
        var duplicate = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var email = $"invalid-{Guid.NewGuid():N}@studyflow.test";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1", "Student"));
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword1"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("student@test.dev", "weak", "Student"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("VALIDATION_ERROR", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Me_WithValidAccessToken_ReturnsCurrentUser()
    {
        var email = $"me-{Guid.NewGuid():N}@studyflow.test";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1", "Current User"));
        var json = await register.Content.ReadFromJsonAsync<JsonElement>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(email, await response.Content.ReadAsStringAsync());
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UserCanSetValidTimeZone_ButInvalidZoneIsRejected()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"timezone-{Guid.NewGuid():N}@studyflow.test", "Password1", "Timezone User"));
        var json = await register.Content.ReadFromJsonAsync<JsonElement>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        var updated = await _client.PutAsJsonAsync("/api/auth/timezone", new UpdateTimeZoneRequest("Asia/Ho_Chi_Minh"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode); var user = await updated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Asia/Ho_Chi_Minh", user.GetProperty("timeZoneId").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync("/api/auth/timezone", new UpdateTimeZoneRequest("Mars/Olympus"))).StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Refresh_WithRotatedToken_RejectsPreviousToken()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"refresh-{Guid.NewGuid():N}@studyflow.test", "Password1", "Student"));
        var cookie = register.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        firstRequest.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(firstRequest)).StatusCode);
        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replayRequest.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(replayRequest)).StatusCode);
    }
}

public sealed class EmailOtpApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory factory;
    public EmailOtpApiTests(AuthApiFactory factory) => this.factory = factory;

    private (HttpClient Client, FakeEmailVerificationSender Sender) Create()
    {
        var sender = new FakeEmailVerificationSender();
        var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<EmailOptions>(options => options.RequireVerification = true);
            services.RemoveAll<IEmailVerificationSender>();
            services.AddSingleton<IEmailVerificationSender>(sender);
        }));
        return (configured.CreateClient(), sender);
    }

    [Fact]
    public async Task Register_RequiresCorrectOtp_BeforeLoginAndIssuesSessionAfterVerification()
    {
        var (client, sender) = Create(); using (client)
        {
            var email = $"otp-{Guid.NewGuid():N}@gmail.com";
            var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1", "OTP Student"));
            Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);
            var pending = await register.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(email, pending.GetProperty("email").GetString()); Assert.Equal(60, pending.GetProperty("resendAfterSeconds").GetInt32());
            Assert.Equal(email, sender.Recipient); Assert.Matches("^[0-9]{6}$", sender.Code);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password1"))).StatusCode);
            for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(email, "000000"))).StatusCode);
            var verify = await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(email, sender.Code));
            Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
            var auth = await verify.Content.ReadFromJsonAsync<JsonElement>(); Assert.True(auth.GetProperty("user").GetProperty("isEmailVerified").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(auth.GetProperty("accessToken").GetString())); Assert.True(verify.Headers.Contains("Set-Cookie"));
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(email, sender.Code))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password1"))).StatusCode);
        }
    }

    [Fact]
    public async Task Otp_Expires_LocksAfterFiveFailures_AndResendRotatesCode()
    {
        var (client, sender) = Create(); using (client)
        {
            var email = $"otp-security-{Guid.NewGuid():N}@gmail.com";
            await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1", "OTP Security"));
            var firstCode = sender.Code;
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/resend-email-otp", new ResendEmailOtpRequest(email))).StatusCode);
            factory.Clock.Advance(TimeSpan.FromSeconds(61));
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/auth/resend-email-otp", new ResendEmailOtpRequest(email))).StatusCode);
            Assert.NotEqual(firstCode, sender.Code);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(email, firstCode))).StatusCode);
            for (var i = 0; i < 4; i++) await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(email, "999999"));
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(email, sender.Code))).StatusCode);
            factory.Clock.Advance(TimeSpan.FromSeconds(61)); await client.PostAsJsonAsync("/api/auth/resend-email-otp", new ResendEmailOtpRequest(email));
            factory.Clock.Advance(TimeSpan.FromMinutes(11));
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(email, sender.Code))).StatusCode);
        }
    }

    [Fact]
    public async Task Resend_ForUnknownEmail_DoesNotRevealAccountExistence()
    {
        var (client, _) = Create(); using (client)
        {
            var response = await client.PostAsJsonAsync("/api/auth/resend-email-otp", new ResendEmailOtpRequest("unknown@gmail.com"));
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }
    }
}

public sealed class FakeEmailVerificationSender : IEmailVerificationSender
{
    public string Recipient { get; private set; } = "";
    public string Code { get; private set; } = "";
    public Task SendOtpAsync(string recipient, string displayName, string code, int expiresInMinutes, CancellationToken cancellationToken)
    { Recipient = recipient; Code = code; return Task.CompletedTask; }
}

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"studyflow-tests-{Guid.NewGuid():N}";
    public TestTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 11, 8, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<StudyFlowDbContext>>();
            services.RemoveAll<StudyFlowDbContext>();
            services.RemoveAll<TimeProvider>();
            services.Configure<EmailOptions>(options => options.RequireVerification = false);
            var databaseServices = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            services.AddDbContext<StudyFlowDbContext>(options => options
                .UseInMemoryDatabase(_databaseName)
                .UseInternalServiceProvider(databaseServices));
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}

public sealed class TestTimeProvider(DateTimeOffset current) : TimeProvider
{
    private DateTimeOffset _current = current;
    public override DateTimeOffset GetUtcNow() => _current;
    public void Advance(TimeSpan duration) => _current = _current.Add(duration);
}
