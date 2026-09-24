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

public sealed class LearningHistoryApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public LearningHistoryApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ConsecutiveActiveDays_ProduceCurrentAndLongestStreak()
    {
        using var client = _factory.CreateClient(); var data = await CreateStudyDataAsync(client);
        await CompleteSessionAsync(client, data.Set.Id, data.Card.Id);
        _factory.Clock.Advance(TimeSpan.FromDays(1));
        await CompleteSessionAsync(client, data.Set.Id, data.Card.Id);

        var streak = await client.GetFromJsonAsync<JsonElement>("/api/progress/streak");
        Assert.Equal(2, streak.GetProperty("currentStreak").GetInt32());
        Assert.Equal(2, streak.GetProperty("longestStreak").GetInt32());
        Assert.Equal(2, streak.GetProperty("totalActiveDays").GetInt32());
    }

    [Fact]
    public async Task History_FillsInactiveDaysAndExcludesOtherUsers()
    {
        using var learner = _factory.CreateClient(); using var other = _factory.CreateClient();
        var learnerData = await CreateStudyDataAsync(learner); var otherData = await CreateStudyDataAsync(other);
        await CompleteSessionAsync(learner, learnerData.Set.Id, learnerData.Card.Id);
        await CompleteSessionAsync(other, otherData.Set.Id, otherData.Card.Id);

        var history = await learner.GetFromJsonAsync<JsonElement>("/api/progress/history?days=7");
        Assert.Equal(7, history.GetProperty("days").GetArrayLength());
        Assert.Equal(1, history.GetProperty("activeDays").GetInt32());
        Assert.Equal(1, history.GetProperty("totalSessions").GetInt32());
        Assert.Equal(1, history.GetProperty("days")[6].GetProperty("cardsReviewed").GetInt32());
    }

    [Fact]
    public async Task History_WithUnsupportedRange_Returns400()
    {
        using var client = _factory.CreateClient(); await AuthenticateAsync(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/progress/history?days=366")).StatusCode);
    }

    private static async Task CompleteSessionAsync(HttpClient client, Guid studySetId, Guid flashcardId)
    {
        var session = await (await client.PostAsJsonAsync("/api/study-sessions", new { studySetId, mode = "Review" })).Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = session.GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/api/flashcards/{flashcardId}/review", new { rating = "Good", studySessionId = sessionId });
        await client.PostAsync($"/api/study-sessions/{sessionId}/complete", null);
    }

    private static async Task<(StudySetDto Set, FlashcardDto Card)> CreateStudyDataAsync(HttpClient client)
    {
        await AuthenticateAsync(client);
        var subject = await (await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("History", null))).Content.ReadFromJsonAsync<SubjectDto>();
        var set = await (await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets", new CreateStudySetRequest("Daily learning", null))).Content.ReadFromJsonAsync<StudySetDto>();
        var card = await (await client.PostAsJsonAsync($"/api/study-sets/{set!.Id}/flashcards", new CreateFlashcardRequest("Prompt", "Answer", null))).Content.ReadFromJsonAsync<FlashcardDto>();
        return (set, card!);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"history-{Guid.NewGuid():N}@studyflow.test", "Password1", "Learner"));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }
}
