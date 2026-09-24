using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;

namespace StudyFlow.Tests.Subjects;

public sealed class SubjectApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public SubjectApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SubjectCrud_WhenUserOwnsSubject_CompletesLifecycle()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsync(client);
        var create = await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Physics", "Mechanics"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var subject = await create.Content.ReadFromJsonAsync<SubjectDto>();
        Assert.NotNull(subject);

        var listed = await client.GetFromJsonAsync<List<SubjectDto>>("/api/subjects");
        Assert.Contains(listed!, x => x.Id == subject.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/subjects/{subject.Id}")).StatusCode);

        var update = await client.PutAsJsonAsync($"/api/subjects/{subject.Id}", new UpdateSubjectRequest("Advanced Physics", null));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Advanced Physics", (await update.Content.ReadFromJsonAsync<SubjectDto>())!.Name);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/subjects/{subject.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/subjects/{subject.Id}")).StatusCode);
    }

    [Fact]
    public async Task CreateSubject_WhenNameIsEmpty_ReturnsValidationError()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsync(client);
        var response = await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("", null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("VALIDATION_ERROR", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SubjectEndpoints_WhenUserIsNotOwner_ReturnNotFound()
    {
        using var owner = _factory.CreateClient();
        using var otherUser = _factory.CreateClient();
        await AuthenticateAsync(owner);
        await AuthenticateAsync(otherUser);
        var create = await owner.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Private subject", null));
        var subject = await create.Content.ReadFromJsonAsync<SubjectDto>();

        Assert.Equal(HttpStatusCode.NotFound, (await otherUser.GetAsync($"/api/subjects/{subject!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherUser.PutAsJsonAsync($"/api/subjects/{subject.Id}", new UpdateSubjectRequest("Stolen", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherUser.DeleteAsync($"/api/subjects/{subject.Id}")).StatusCode);
        var listed = await otherUser.GetFromJsonAsync<List<SubjectDto>>("/api/subjects");
        Assert.DoesNotContain(listed!, x => x.Id == subject.Id);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var email = $"subject-{Guid.NewGuid():N}@studyflow.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password1", "Subject Owner"));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }
}
