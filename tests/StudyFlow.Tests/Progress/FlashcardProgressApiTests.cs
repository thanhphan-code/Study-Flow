using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;
using Microsoft.Extensions.DependencyInjection;
using StudyFlow.Infrastructure.Persistence;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Tests.Progress;

public sealed class FlashcardProgressApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public FlashcardProgressApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ReviewNewCard_WithGood_CreatesProgress()
    {
        using var client = _factory.CreateClient(); var card = await CreateOwnedCardAsync(client);
        var response = await client.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", new { rating = "Good" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); var progress = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Reviewing", progress.GetProperty("status").GetString()); Assert.Equal(3, progress.GetProperty("intervalDays").GetInt32()); Assert.Equal(1, progress.GetProperty("correctCount").GetInt32());
        using var scope = _factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
        var attempt = db.LearningAttempts.Single(x => x.FlashcardId == card.Id);
        var state = db.FlashcardProgress.Single(x => x.FlashcardId == card.Id);
        Assert.Equal(StudyMode.Review, attempt.Mode); Assert.True(attempt.SrsCommitted); Assert.Equal(ReviewRating.Good, attempt.CommittedRating);
        Assert.True(state.MasteryScore > 0); Assert.Equal(1, state.StateRevision); Assert.NotNull(state.LastAttemptAt); Assert.Equal("simple-srs/2", state.SchedulerVersion);
        var decision = db.LearningDecisions.Single(x => x.LearningAttemptId == attempt.Id);
        Assert.Equal("memory-state/2", decision.StatePolicyVersion); Assert.Equal("simple-srs/2", decision.SchedulerVersion);
    }

    [Fact]
    public async Task UserCannotReviewAnotherUsersFlashcard()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient(); var card = await CreateOwnedCardAsync(owner); await AuthenticateAsync(attacker);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", new { rating = "Good" })).StatusCode);
    }

    [Fact]
    public async Task TypedWrongAnswer_OverridesOptimisticSelfRating()
    {
        using var client = _factory.CreateClient(); var card = await CreateOwnedCardAsync(client);
        var response = await client.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", new
        {
            rating = "Good",
            submittedAnswer = "Definitely incorrect",
            attemptType = "Recall",
            direction = "Forward",
            clientAttemptId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("wrongCount").GetInt32()); Assert.Equal(0, body.GetProperty("correctCount").GetInt32());
        using var scope = _factory.Services.CreateScope(); var attempt = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>().LearningAttempts.Single(x => x.FlashcardId == card.Id);
        Assert.Equal(LearningAttemptResult.Wrong, attempt.Result); Assert.Equal(ReviewRating.Again, attempt.CommittedRating);
    }

    [Fact]
    public async Task SameClientAttempt_IsIdempotent_ButDifferentPayloadConflicts()
    {
        using var client = _factory.CreateClient(); var card = await CreateOwnedCardAsync(client); var clientAttemptId = Guid.NewGuid();
        var request = new { rating = "Good", submittedAnswer = "Position change over time", attemptType = "Recall", direction = "Forward", clientAttemptId };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", request)).StatusCode);
        var conflict = await client.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", new { rating = "Good", submittedAnswer = "different", attemptType = "Recall", direction = "Forward", clientAttemptId });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode); Assert.Contains("IDEMPOTENCY_KEY_REUSED", await conflict.Content.ReadAsStringAsync());
        using var scope = _factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
        Assert.Equal(1, db.LearningAttempts.Count(x => x.FlashcardId == card.Id));
        Assert.Equal(1, db.FlashcardProgress.Single(x => x.FlashcardId == card.Id).StateRevision);
    }

    [Fact]
    public async Task EachUserHasOnlyOneProgressPerFlashcard()
    {
        using var client = _factory.CreateClient(); var card = await CreateOwnedCardAsync(client);
        var first = await (await client.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", new { rating = "Good" })).Content.ReadFromJsonAsync<JsonElement>();
        var second = await (await client.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", new { rating = "Hard" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(first.GetProperty("id").GetGuid(), second.GetProperty("id").GetGuid()); Assert.Equal(2, second.GetProperty("correctCount").GetInt32());
    }

    [Fact]
    public async Task GetDueCards_DoesNotReturnFutureCards()
    {
        using var client = _factory.CreateClient(); var card = await CreateOwnedCardAsync(client);
        await client.PostAsJsonAsync($"/api/flashcards/{card.Id}/review", new { rating = "Again" });
        var due = await client.GetFromJsonAsync<JsonElement>("/api/reviews/due");
        Assert.Equal(0, due.GetArrayLength());
    }

    [Fact]
    public async Task GetDueCards_ReturnsOnlyCurrentUsersCards()
    {
        using var firstUser = _factory.CreateClient(); using var secondUser = _factory.CreateClient();
        var firstCard = await CreateOwnedCardAsync(firstUser); var secondCard = await CreateOwnedCardAsync(secondUser);
        await firstUser.PostAsJsonAsync($"/api/flashcards/{firstCard.Id}/review", new { rating = "Again" });
        await secondUser.PostAsJsonAsync($"/api/flashcards/{secondCard.Id}/review", new { rating = "Again" });
        _factory.Clock.Advance(TimeSpan.FromMinutes(11));
        var due = await firstUser.GetFromJsonAsync<JsonElement>("/api/reviews/due");
        Assert.Equal(1, due.GetArrayLength()); Assert.Equal(firstCard.Id, due[0].GetProperty("flashcardId").GetGuid());
    }

    private static async Task<FlashcardDto> CreateOwnedCardAsync(HttpClient client)
    {
        await AuthenticateAsync(client);
        var subject = await (await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Physics", null))).Content.ReadFromJsonAsync<SubjectDto>();
        var studySet = await (await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets", new CreateStudySetRequest("Motion", null))).Content.ReadFromJsonAsync<StudySetDto>();
        return (await (await client.PostAsJsonAsync($"/api/study-sets/{studySet!.Id}/flashcards", new CreateFlashcardRequest("Velocity?", "Position change over time", null))).Content.ReadFromJsonAsync<FlashcardDto>())!;
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"progress-{Guid.NewGuid():N}@studyflow.test", "Password1", "Learner")); var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }
}
