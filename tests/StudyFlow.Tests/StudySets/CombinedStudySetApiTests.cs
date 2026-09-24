using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;

namespace StudyFlow.Tests.StudySets;
public sealed class CombinedStudySetApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory; public CombinedStudySetApiTests(AuthApiFactory factory) => _factory = factory;
    [Fact]
    public async Task CombinedSet_ReferencesSourceCards_WithoutCopyingProgressOrContent()
    {
        using var client = _factory.CreateClient(); var subject = await CreateSubjectAsync(client); var first = await CreateSetAsync(client, subject.Id, "9.1"); var second = await CreateSetAsync(client, subject.Id, "9.2"); var card = await CreateCardAsync(client, first.Id, "Momentum"); await CreateCardAsync(client, second.Id, "Impulse");
        var response = await client.PostAsJsonAsync($"/api/subjects/{subject.Id}/combined-study-sets", new CreateCombinedStudySetRequest("Chapter 9 Review", null, [first.Id, second.Id])); Assert.Equal(HttpStatusCode.Created, response.StatusCode); var combined = await response.Content.ReadFromJsonAsync<StudySetDto>(); Assert.Equal("Combined", combined!.Type.ToString()); Assert.Equal(2, combined.TotalCards);
        var cards = await client.GetFromJsonAsync<List<FlashcardDto>>($"/api/study-sets/{combined.Id}/flashcards"); Assert.Equal(2, cards!.Count); Assert.Contains(cards, x => x.Id == card.Id);
        await client.PutAsJsonAsync($"/api/flashcards/{card.Id}", new UpdateFlashcardRequest("Linear momentum", "mass × velocity", null)); cards = await client.GetFromJsonAsync<List<FlashcardDto>>($"/api/study-sets/{combined.Id}/flashcards"); Assert.Contains(cards!, x => x.Id == card.Id && x.FrontText == "Linear momentum");
        var deleteSource = await client.DeleteAsync($"/api/study-sets/{first.Id}"); Assert.Equal(HttpStatusCode.Conflict, deleteSource.StatusCode); Assert.Contains("STUDY_SET_IN_COMBINED_SET", await deleteSource.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task CombinedSet_RejectsNestedAndOtherUsersSources()
    {
        using var owner = _factory.CreateClient(); using var other = _factory.CreateClient(); var subject = await CreateSubjectAsync(owner); var first = await CreateSetAsync(owner, subject.Id, "A"); var second = await CreateSetAsync(owner, subject.Id, "B"); var combined = await (await owner.PostAsJsonAsync($"/api/subjects/{subject.Id}/combined-study-sets", new CreateCombinedStudySetRequest("AB", null, [first.Id, second.Id]))).Content.ReadFromJsonAsync<StudySetDto>(); var otherSubject = await CreateSubjectAsync(other); var foreign = await CreateSetAsync(other, otherSubject.Id, "Foreign");
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/api/subjects/{subject.Id}/combined-study-sets", new CreateCombinedStudySetRequest("Nested", null, [combined!.Id, first.Id]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/study-together", new StudyTogetherRequest([first.Id, foreign.Id]))).StatusCode);
    }
    private static async Task<SubjectDto> CreateSubjectAsync(HttpClient client) { var auth = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"combined-{Guid.NewGuid():N}@studyflow.test", "Password1", "Owner")); var json = await auth.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString()); return (await (await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Physics", null))).Content.ReadFromJsonAsync<SubjectDto>())!; }
    private static async Task<StudySetDto> CreateSetAsync(HttpClient client, Guid subjectId, string title) => (await (await client.PostAsJsonAsync($"/api/subjects/{subjectId}/study-sets", new CreateStudySetRequest(title, null))).Content.ReadFromJsonAsync<StudySetDto>())!;
    private static async Task<FlashcardDto> CreateCardAsync(HttpClient client, Guid setId, string value) => (await (await client.PostAsJsonAsync($"/api/study-sets/{setId}/flashcards", new CreateFlashcardRequest(value, $"Answer {value}", null))).Content.ReadFromJsonAsync<FlashcardDto>())!;
}
