using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.SmartLearning.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Domain.Enums;
using StudyFlow.Infrastructure.Persistence;
using StudyFlow.Tests.Auth;

namespace StudyFlow.Tests.SmartLearning;

public sealed class SmartLearningApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public SmartLearningApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task WrongThenImmediateCorrect_DoesNotOverwriteFailureOrCommitSrs()
    {
        using var client = _factory.CreateClient(); var set = await CreateSetAsync(client, 5); var started = await StartAsync(client, set.Id); var item = started.GetProperty("items")[0]; var sessionId = started.GetProperty("studySessionId").GetGuid(); var cardId = item.GetProperty("flashcardId").GetGuid(); var answer = item.GetProperty("backText").GetString()!;
        await RecordAsync(client, sessionId, cardId, LearningAttemptType.MultipleChoice, RecallDirection.Forward, answer);
        _factory.Clock.Advance(TimeSpan.FromSeconds(1)); await RecordAsync(client, sessionId, cardId, LearningAttemptType.Recall, RecallDirection.Forward, "definitely wrong");
        _factory.Clock.Advance(TimeSpan.FromSeconds(1)); var immediateCorrect = await RecordAsync(client, sessionId, cardId, LearningAttemptType.Recall, RecallDirection.Forward, answer);
        Assert.Equal("InProgress", immediateCorrect.GetProperty("cardOutcome").GetString()); Assert.False(immediateCorrect.GetProperty("srsCommitted").GetBoolean());
        using var scope = _factory.Services.CreateScope(); Assert.False(scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>().FlashcardProgress.Any(x => x.FlashcardId == cardId));
    }

    [Fact]
    public async Task TwoCorrectRecallsSeparatedByThreeOtherCards_CommitsSrsOnce()
    {
        using var client = _factory.CreateClient(); var set = await CreateSetAsync(client, 5); var started = await StartAsync(client, set.Id); var sessionId = started.GetProperty("studySessionId").GetGuid(); var items = started.GetProperty("items").EnumerateArray().ToArray(); var target = items[0]; var targetId = target.GetProperty("flashcardId").GetGuid(); var targetAnswer = target.GetProperty("backText").GetString()!;
        await RecordAsync(client, sessionId, targetId, LearningAttemptType.MultipleChoice, RecallDirection.Forward, targetAnswer); _factory.Clock.Advance(TimeSpan.FromSeconds(1));
        var firstRecall = await RecordAsync(client, sessionId, targetId, LearningAttemptType.Recall, RecallDirection.Forward, targetAnswer); Assert.Equal("InProgress", firstRecall.GetProperty("cardOutcome").GetString());
        foreach (var other in items.Skip(1).Take(3)) { _factory.Clock.Advance(TimeSpan.FromSeconds(1)); await RecordAsync(client, sessionId, other.GetProperty("flashcardId").GetGuid(), LearningAttemptType.MultipleChoice, RecallDirection.Forward, other.GetProperty("backText").GetString()!); }
        _factory.Clock.Advance(TimeSpan.FromSeconds(1)); var completed = await RecordAsync(client, sessionId, targetId, LearningAttemptType.Recall, RecallDirection.Reverse, target.GetProperty("frontText").GetString()!, confidence: 5);
        Assert.Equal("Completed", completed.GetProperty("cardOutcome").GetString()); Assert.True(completed.GetProperty("srsCommitted").GetBoolean());
        using var scope = _factory.Services.CreateScope(); var progress = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>().FlashcardProgress.Single(x => x.FlashcardId == targetId); Assert.Equal(1, progress.CorrectCount); Assert.InRange(progress.IntervalDays, 1, 4);
    }

    [Fact]
    public async Task MissingVietnameseDiacritics_IsCloseNotCorrect()
    {
        using var client = _factory.CreateClient(); var set = await CreateSetAsync(client, 4, "quá trình quang hợp"); var started = await StartAsync(client, set.Id); var item = started.GetProperty("items")[0]; var sessionId = started.GetProperty("studySessionId").GetGuid(); var cardId = item.GetProperty("flashcardId").GetGuid();
        await RecordAsync(client, sessionId, cardId, LearningAttemptType.MultipleChoice, RecallDirection.Forward, item.GetProperty("backText").GetString()!); _factory.Clock.Advance(TimeSpan.FromSeconds(1));
        var close = await RecordAsync(client, sessionId, cardId, LearningAttemptType.Recall, RecallDirection.Forward, "qua trinh quang hop"); Assert.Equal("Close", close.GetProperty("result").GetString()); Assert.False(close.GetProperty("srsCommitted").GetBoolean());
    }

    [Fact]
    public async Task DuplicateClientAttempt_IsIdempotent()
    {
        using var client = _factory.CreateClient(); var set = await CreateSetAsync(client, 4); var started = await StartAsync(client, set.Id); var item = started.GetProperty("items")[0]; var sessionId = started.GetProperty("studySessionId").GetGuid(); var cardId = item.GetProperty("flashcardId").GetGuid(); var attemptId = Guid.NewGuid();
        var first = await RecordAsync(client, sessionId, cardId, LearningAttemptType.MultipleChoice, RecallDirection.Forward, item.GetProperty("backText").GetString()!, attemptId: attemptId); var duplicate = await RecordAsync(client, sessionId, cardId, LearningAttemptType.MultipleChoice, RecallDirection.Forward, item.GetProperty("backText").GetString()!, attemptId: attemptId);
        Assert.Equal(first.GetProperty("id").GetGuid(), duplicate.GetProperty("id").GetGuid()); using var scope = _factory.Services.CreateScope(); Assert.Equal(1, scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>().LearningAttempts.Count(x => x.StudySessionId == sessionId));
    }

    [Fact]
    public async Task ActiveSession_CanBeResumedWithPersistedNextStep()
    {
        using var client = _factory.CreateClient(); var set = await CreateSetAsync(client, 5); var started = await StartAsync(client, set.Id); var item = started.GetProperty("items")[0]; var sessionId = started.GetProperty("studySessionId").GetGuid(); var cardId = item.GetProperty("flashcardId").GetGuid();
        await RecordAsync(client, sessionId, cardId, LearningAttemptType.MultipleChoice, RecallDirection.Forward, item.GetProperty("backText").GetString()!);
        var resumed = await (await client.GetAsync($"/api/study-sets/{set.Id}/smart-learn/active")).Content.ReadFromJsonAsync<JsonElement>(); var resumedCard = resumed.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("flashcardId").GetGuid() == cardId);
        Assert.Equal("Recall", resumedCard.GetProperty("attemptType").GetString()); Assert.Equal("Forward", resumedCard.GetProperty("direction").GetString());
    }

    [Fact]
    public async Task SmartLearn_InAnotherUsersSet_Returns404()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient(); var set = await CreateSetAsync(owner, 5); await AuthenticateAsync(attacker); Assert.Equal(HttpStatusCode.NotFound, (await attacker.PostAsJsonAsync($"/api/study-sets/{set.Id}/smart-learn", new StartSmartLearnRequest(5))).StatusCode);
    }

    private static async Task<JsonElement> StartAsync(HttpClient client, Guid setId) => (await (await client.PostAsJsonAsync($"/api/study-sets/{setId}/smart-learn", new StartSmartLearnRequest(5))).Content.ReadFromJsonAsync<JsonElement>());
    private static async Task<JsonElement> RecordAsync(HttpClient client, Guid sessionId, Guid cardId, LearningAttemptType type, RecallDirection direction, string answer, int confidence = 3, Guid? attemptId = null)
    {
        var response = await client.PostAsJsonAsync($"/api/smart-learn/sessions/{sessionId}/attempts", new RecordLearningAttemptRequest(attemptId ?? Guid.NewGuid(), cardId, type, direction, answer, confidence, 1800, false)); Assert.Equal(HttpStatusCode.Created, response.StatusCode); return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static async Task<StudySetDto> CreateSetAsync(HttpClient client, int cards, string? firstAnswer = null)
    {
        await AuthenticateAsync(client); var subject = await (await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Smart learning", null))).Content.ReadFromJsonAsync<SubjectDto>(); var set = await (await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets", new CreateStudySetRequest("Adaptive set", null))).Content.ReadFromJsonAsync<StudySetDto>();
        for (var index = 0; index < cards; index++) await client.PostAsJsonAsync($"/api/study-sets/{set!.Id}/flashcards", new CreateFlashcardRequest($"Term {index}", index == 0 && firstAnswer is not null ? firstAnswer : $"Answer {index}", "Explanation", EnableReverseRecall: index == 0)); return set!;
    }
    private static async Task AuthenticateAsync(HttpClient client) { var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"smart-{Guid.NewGuid():N}@studyflow.test", "Password1", "Smart Learner")); var json = await response.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString()); }
}
