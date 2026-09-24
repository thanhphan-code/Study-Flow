using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using StudyFlow.Application.Admin;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Domain.Enums;
using StudyFlow.Tests.Auth;

namespace StudyFlow.Tests.Admin;

public sealed class AdminApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory factory;
    public AdminApiTests(AuthApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task RegularUser_CannotAccessAdminEndpoints()
    {
        using var client = factory.CreateClient();
        var token = await RegisterAndGetToken(client, $"regular-{Guid.NewGuid():N}@studyflow.test", "Regular User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/overview")).StatusCode);
    }

    [Fact]
    public async Task Admin_CanSearchSuspendRevokeAndAudit_WhileSelfProtectionIsEnforced()
    {
        var adminEmail = $"root-{Guid.NewGuid():N}@studyflow.test";
        await using var configuredFactory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:BootstrapEmails:0"] = adminEmail })));
        using var adminClient = configuredFactory.CreateClient();
        using var targetClient = configuredFactory.CreateClient();

        var adminToken = await RegisterAndGetToken(adminClient, adminEmail, "Root Admin");
        var targetEmail = $"target-{Guid.NewGuid():N}@studyflow.test";
        var targetToken = await RegisterAndGetToken(targetClient, targetEmail, "Target Learner");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        targetClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", targetToken);

        var overview = await adminClient.GetAsync("/api/admin/overview");
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
        var list = await adminClient.GetFromJsonAsync<JsonElement>($"/api/admin/users?search={Uri.EscapeDataString(targetEmail)}");
        var targetId = list.GetProperty("items")[0].GetProperty("id").GetGuid();
        var adminId = (await adminClient.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("id").GetGuid();

        var suspend = await adminClient.PutAsJsonAsync($"/api/admin/users/{targetId}/status", new UpdateUserStatusRequest(true, "Vi phạm quy tắc dữ liệu trong kiểm thử"));
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await targetClient.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await targetClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(targetEmail, "Password1"))).StatusCode);

        var selfSuspend = await adminClient.PutAsJsonAsync($"/api/admin/users/{adminId}/status", new UpdateUserStatusRequest(true, "Không được tự khóa"));
        Assert.Equal(HttpStatusCode.Conflict, selfSuspend.StatusCode);
        var audit = await adminClient.GetFromJsonAsync<JsonElement>("/api/admin/audit");
        Assert.Contains(audit.GetProperty("items").EnumerateArray(), item => item.GetProperty("action").GetString() == "USER_SUSPENDED");
    }

    [Fact]
    public async Task RoleChange_RevokesExistingSession_AndNewLoginReceivesAdminRole()
    {
        var adminEmail = $"role-root-{Guid.NewGuid():N}@studyflow.test";
        await using var configuredFactory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:BootstrapEmails:0"] = adminEmail })));
        using var adminClient = configuredFactory.CreateClient();
        using var promotedClient = configuredFactory.CreateClient();
        var adminToken = await RegisterAndGetToken(adminClient, adminEmail, "Role Admin");
        var promotedEmail = $"promoted-{Guid.NewGuid():N}@studyflow.test";
        var oldToken = await RegisterAndGetToken(promotedClient, promotedEmail, "Promoted User");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var list = await adminClient.GetFromJsonAsync<JsonElement>($"/api/admin/users?search={Uri.EscapeDataString(promotedEmail)}");
        var promotedId = list.GetProperty("items")[0].GetProperty("id").GetGuid();

        var changed = await adminClient.PutAsJsonAsync($"/api/admin/users/{promotedId}/role", new UpdateUserRoleRequest(UserRole.Admin, "Bổ nhiệm quản trị viên kiểm thử"));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        promotedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await promotedClient.GetAsync("/api/auth/me")).StatusCode);
        promotedClient.DefaultRequestHeaders.Authorization = null;
        var login = await promotedClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(promotedEmail, "Password1"));
        var payload = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Admin", payload.GetProperty("user").GetProperty("role").GetString());
    }

    private static async Task<string> RegisterAndGetToken(HttpClient client, string email, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1", displayName));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
    }
}
