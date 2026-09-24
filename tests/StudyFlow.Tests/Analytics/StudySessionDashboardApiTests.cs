using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;

namespace StudyFlow.Tests.Analytics;

public sealed class StudySessionDashboardApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public StudySessionDashboardApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ReviewWithinCompletedSession_IsIncludedInDashboard()
    {
        using var client = _factory.CreateClient(); var (set, card) = await CreateStudyDataAsync(client);
        var started = await (await client.PostAsJsonAsync("/api/study-sessions", new { studySetId = set.Id, mode = "Flashcard" })).Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = started.GetProperty("id").GetGuid();
        _factory.Clock.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", new { rating = "Good", studySessionId = sessionId })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/study-sessions/{sessionId}/complete", null)).StatusCode);

        var dashboard = await client.GetFromJsonAsync<JsonElement>("/api/dashboard");
        Assert.Equal(1, dashboard.GetProperty("cardsReviewedToday").GetInt32());
        Assert.Equal(100, dashboard.GetProperty("accuracyToday").GetDecimal());
        Assert.True(dashboard.GetProperty("studyTimeSecondsToday").GetInt32() >= 120);
        Assert.Equal(set.Id, dashboard.GetProperty("recentStudySets")[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task EmptySession_IsNotIncludedInDashboard()
    {
        using var client = _factory.CreateClient(); var (set, _) = await CreateStudyDataAsync(client);
        var started = await (await client.PostAsJsonAsync("/api/study-sessions", new { studySetId = set.Id, mode = "Review" })).Content.ReadFromJsonAsync<JsonElement>();
        await client.PostAsync($"/api/study-sessions/{started.GetProperty("id").GetGuid()}/complete", null);
        var dashboard = await client.GetFromJsonAsync<JsonElement>("/api/dashboard");
        Assert.Equal(0, dashboard.GetProperty("cardsReviewedToday").GetInt32());
        Assert.Equal(0, dashboard.GetProperty("recentStudySets").GetArrayLength());
    }

    [Fact]
    public async Task UserCannotStartSessionForAnotherUsersStudySet()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient();
        var (set, _) = await CreateStudyDataAsync(owner); await AuthenticateAsync(attacker);
        var response = await attacker.PostAsJsonAsync("/api/study-sessions", new { studySetId = set.Id, mode = "Flashcard" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<(StudySetDto Set, FlashcardDto Card)> CreateStudyDataAsync(HttpClient client)
    {
        await AuthenticateAsync(client);
        var subject = await (await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Analytics", null))).Content.ReadFromJsonAsync<SubjectDto>();
        var set = await (await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets", new CreateStudySetRequest("Daily set", null))).Content.ReadFromJsonAsync<StudySetDto>();
        var card = await (await client.PostAsJsonAsync($"/api/study-sets/{set!.Id}/flashcards", new CreateFlashcardRequest("Question", "Answer", null))).Content.ReadFromJsonAsync<FlashcardDto>();
        return (set, card!);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"analytics-{Guid.NewGuid():N}@studyflow.test", "Password1", "Learner"));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }
}
