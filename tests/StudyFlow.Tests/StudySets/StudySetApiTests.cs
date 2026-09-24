using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;

namespace StudyFlow.Tests.StudySets;

public sealed class StudySetApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public StudySetApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task StudySetCrud_WhenUserOwnsSubject_CompletesLifecycle()
    {
        using var client = _factory.CreateClient(); await AuthenticateAsync(client); var subject = await CreateSubjectAsync(client);
        var create = await client.PostAsJsonAsync($"/api/subjects/{subject.Id}/study-sets", new CreateStudySetRequest("Chapter 1", "Motion"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var studySet = await create.Content.ReadFromJsonAsync<StudySetDto>(); Assert.NotNull(studySet); Assert.Equal(subject.Id, studySet.SubjectId);
        Assert.Contains((await client.GetFromJsonAsync<List<StudySetDto>>($"/api/subjects/{subject.Id}/study-sets"))!, x => x.Id == studySet.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/study-sets/{studySet.Id}")).StatusCode);
        var update = await client.PutAsJsonAsync($"/api/study-sets/{studySet.Id}", new UpdateStudySetRequest("Chapter 1 — Kinematics", null));
        Assert.Equal("Chapter 1 — Kinematics", (await update.Content.ReadFromJsonAsync<StudySetDto>())!.Title);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/study-sets/{studySet.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/study-sets/{studySet.Id}")).StatusCode);
    }

    [Fact]
    public async Task CreateStudySet_InAnotherUsersSubject_ShouldReturn404()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient();
        await AuthenticateAsync(owner); await AuthenticateAsync(attacker); var subject = await CreateSubjectAsync(owner);
        var response = await attacker.PostAsJsonAsync($"/api/subjects/{subject.Id}/study-sets", new CreateStudySetRequest("Unauthorized", null));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("SUBJECT_NOT_FOUND", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StudySetEndpoints_WhenUserIsNotOwner_ReturnNotFound()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient();
        await AuthenticateAsync(owner); await AuthenticateAsync(attacker); var subject = await CreateSubjectAsync(owner);
        var created = await owner.PostAsJsonAsync($"/api/subjects/{subject.Id}/study-sets", new CreateStudySetRequest("Private set", null));
        var studySet = await created.Content.ReadFromJsonAsync<StudySetDto>();
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.GetAsync($"/api/study-sets/{studySet!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.PutAsJsonAsync($"/api/study-sets/{studySet.Id}", new UpdateStudySetRequest("Changed", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.DeleteAsync($"/api/study-sets/{studySet.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.GetAsync($"/api/subjects/{subject.Id}/study-sets")).StatusCode);
    }

    [Fact]
    public async Task DeleteSubject_WhenStudySetsExist_ReturnsConflict()
    {
        using var client = _factory.CreateClient(); await AuthenticateAsync(client); var subject = await CreateSubjectAsync(client);
        await client.PostAsJsonAsync($"/api/subjects/{subject.Id}/study-sets", new CreateStudySetRequest("Chapter 1", null));
        var response = await client.DeleteAsync($"/api/subjects/{subject.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("SUBJECT_HAS_STUDY_SETS", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateStudySet_WhenTitleIsEmpty_ReturnsValidationError()
    {
        using var client = _factory.CreateClient(); await AuthenticateAsync(client); var subject = await CreateSubjectAsync(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/subjects/{subject.Id}/study-sets", new CreateStudySetRequest("", null))).StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"set-{Guid.NewGuid():N}@studyflow.test", "Password1", "Set Owner"));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }

    private static async Task<SubjectDto> CreateSubjectAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Physics", null));
        return (await response.Content.ReadFromJsonAsync<SubjectDto>())!;
    }
}
