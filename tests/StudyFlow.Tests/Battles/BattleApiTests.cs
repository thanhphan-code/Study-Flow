using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Battles;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;
using StudyFlow.Infrastructure.Persistence;
using StudyFlow.Tests.Auth;

namespace StudyFlow.Tests.Battles;

public sealed class BattleApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory factory;
    public BattleApiTests(AuthApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task RoomCapacity_ConcurrentJoinsAndReconnect_KeepUniqueParticipants()
    {
        using var host = factory.CreateClient(); var hostId = await SignIn(host);
        var room = await Create(host, hostId);
        var clients = new List<HttpClient>();
        for (var i = 0; i < 12; i++) { var client = factory.CreateClient(); await SignIn(client); clients.Add(client); }
        try
        {
            var responses = await Task.WhenAll(clients.Select(c => c.PostAsync($"/api/battles/{room.Id}/join", null)));
            Assert.Equal(9, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
            Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            var accepted = clients[Array.FindIndex(responses, r => r.IsSuccessStatusCode)];
            for (var i = 0; i < 3; i++) Assert.Equal(HttpStatusCode.OK, (await accepted.PostAsync($"/api/battles/{room.Id}/join", null)).StatusCode);
            var state = await State(host, room.Id); Assert.Equal(10, state.PlayerCount);
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
            Assert.Equal(10, await db.BattleParticipants.CountAsync(x => x.BattleRoomId == room.Id));
        }
        finally { foreach (var client in clients) client.Dispose(); }
    }

    [Fact]
    public async Task Authorization_LockKickAndAccountOwnership_AreEnforced()
    {
        using var host = factory.CreateClient(); var hostId = await SignIn(host); var room = await Create(host, hostId);
        using var guest = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync($"/api/battles/{room.Id}/join", null)).StatusCode);
        var guestId = await SignIn(guest);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.PostAsync($"/api/battles/{room.Id}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.PostAsJsonAsync("/api/battles", new CreateBattleRequest(room.StudySetId, "Stolen"))).StatusCode);
        await host.PostAsync($"/api/battles/{room.Id}/lock", null);
        Assert.Equal(HttpStatusCode.Conflict, (await guest.PostAsync($"/api/battles/{room.Id}/join", null)).StatusCode);
        await host.PostAsync($"/api/battles/{room.Id}/unlock", null);
        Assert.Equal(HttpStatusCode.OK, (await guest.PostAsync($"/api/battles/{room.Id}/join", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.PostAsync($"/api/battles/{room.Id}/participants/{guestId}/kick", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/api/battles/{room.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await guest.PostAsync($"/api/battles/{room.Id}/join", null)).StatusCode);
    }

    [Fact]
    public async Task Answer_Idempotency_Secrecy_AndPrivateMemoryProgress()
    {
        using var host = factory.CreateClient(); var hostId = await SignIn(host); var room = await Create(host, hostId, ["ShortAnswer"]);
        using var player = factory.CreateClient(); var playerId = await SignIn(player); await player.PostAsync($"/api/battles/{room.Id}/join", null);
        await host.PostAsync($"/api/battles/{room.Id}/start", null); factory.Clock.Advance(TimeSpan.FromSeconds(4));
        var state = await State(player, room.Id); var q = state.Question!;
        Assert.Null(state.MyAnswer); Assert.Empty(state.Reviews);
        Assert.All(state.Players, p => Assert.Null(p.Score));
        var raw = await player.GetStringAsync($"/api/battles/{room.Id}"); Assert.DoesNotContain("Definition", raw); Assert.DoesNotContain("Explanation", raw);
        var expected = "Definition " + q.Prompt.Split(' ').Last();
        var request = new BattleAnswerRequest(expected);
        var first = await player.PostAsJsonAsync($"/api/battles/{room.Id}/questions/{q.Id}/answer", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var feedback = (await first.Content.ReadFromJsonAsync<BattleRoomDto>())!;
        Assert.Equal("Correct", feedback.MyAnswer!.Evaluation); Assert.InRange(feedback.MyScore, 100, 150);
        var replay = await player.PostAsJsonAsync($"/api/battles/{room.Id}/questions/{q.Id}/answer", request);
        Assert.Equal(feedback.MyScore, (await replay.Content.ReadFromJsonAsync<BattleRoomDto>())!.MyScore);
        Assert.Equal(HttpStatusCode.Conflict, (await player.PostAsJsonAsync($"/api/battles/{room.Id}/questions/{q.Id}/answer", new BattleAnswerRequest("Changed"))).StatusCode);
        Assert.Null((await State(host, room.Id)).MyAnswer);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
        var answer = await db.BattleAnswers.SingleAsync(a => a.BattleQuestionId == q.Id && a.UserId == playerId);
        var attempt = await db.LearningAttempts.SingleAsync(a => a.ClientAttemptId == answer.Id);
        Assert.True(attempt.SrsCommitted); Assert.Equal(StudyMode.Battle, attempt.Mode);
        Assert.Equal(playerId, attempt.UserId);
        Assert.Single(await db.FlashcardProgress.Where(p => p.UserId == playerId && p.FlashcardId == attempt.FlashcardId).ToListAsync());
        var reviewCard = await db.Flashcards.SingleAsync(c => c.Id == attempt.FlashcardId);
        Assert.NotEqual(room.StudySetId, reviewCard.StudySetId);
        Assert.Equal(HttpStatusCode.OK, (await player.GetAsync($"/api/study-sets/{reviewCard.StudySetId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.GetAsync($"/api/study-sets/{reviewCard.StudySetId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await player.GetAsync($"/api/study-sets/{room.StudySetId}")).StatusCode);
    }

    [Theory]
    [InlineData("MultipleChoice")]
    [InlineData("FillBlank")]
    [InlineData("Matching")]
    [InlineData("Ordering")]
    [InlineData("ShortAnswer")]
    public async Task SupportedQuestionTypes_AcceptCorrectAnswer(string type)
    {
        using var host = factory.CreateClient(); var userId = await SignIn(host); var room = await Create(host, userId, [type], ordering: type == "Ordering");
        await host.PostAsync($"/api/battles/{room.Id}/start", null); factory.Clock.Advance(TimeSpan.FromSeconds(4));
        var state = await State(host, room.Id); var question = state.Question!; Assert.Equal(type, question.QuestionType);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
        var expected = (await db.BattleQuestions.SingleAsync(x => x.Id == question.Id)).CorrectAnswer;
        var response = await host.PostAsJsonAsync($"/api/battles/{room.Id}/questions/{question.Id}/answer", new BattleAnswerRequest(expected));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Correct", (await response.Content.ReadFromJsonAsync<BattleRoomDto>())!.MyAnswer!.Evaluation);
    }

    [Fact]
    public async Task FullGame_NoTimer_CompletionAndHostResults()
    {
        using var host = factory.CreateClient(); var userId = await SignIn(host); var room = await Create(host, userId, ["TrueFalse", "FillBlank"], timer: null);
        await host.PostAsync($"/api/battles/{room.Id}/start", null);
        var trueFalse = 0;
        for (var i = 0; i < 10; i++)
        {
            factory.Clock.Advance(TimeSpan.FromSeconds(4));
            var state = await State(host, room.Id); var question = state.Question!;
            Assert.Null(question.TimeLimitSeconds);
            if (question.QuestionType == "TrueFalse") trueFalse++;
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
            var expected = (await db.BattleQuestions.SingleAsync(x => x.Id == question.Id)).CorrectAnswer;
            var response = await host.PostAsJsonAsync($"/api/battles/{room.Id}/questions/{question.Id}/answer", new BattleAnswerRequest(expected));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull((await response.Content.ReadFromJsonAsync<BattleRoomDto>())!.Question!.EndedAt);
            Assert.Equal(HttpStatusCode.OK, (await host.PostAsync($"/api/battles/{room.Id}/next", null)).StatusCode);
        }
        Assert.InRange(trueFalse, 1, 2);
        var completed = await State(host, room.Id);
        Assert.Equal("Completed", completed.Status); Assert.Equal(10, completed.Reviews.Length);
        Assert.Equal(100, completed.Players.Single().Accuracy); Assert.Equal(1, completed.Players.Single().Rank);
        Assert.NotNull(completed.ReviewStudySetId);
        Assert.All(completed.Reviews, q => Assert.Equal(100, q.RoomAccuracy));
    }

    [Fact]
    public async Task ServerDeadline_RejectsLateScoreAndFutureQuestions_RecordsTimeoutOnce()
    {
        using var host = factory.CreateClient(); var userId = await SignIn(host); var room = await Create(host, userId, ["FillBlank"], timer: 10);
        await host.PostAsync($"/api/battles/{room.Id}/start", null);
        var state = await State(host, room.Id); var question = state.Question!;
        var url = $"/api/battles/{room.Id}/questions/{question.Id}/answer";
        Assert.Equal(HttpStatusCode.Conflict, (await host.PostAsJsonAsync(url, new BattleAnswerRequest("Too early"))).StatusCode);
        Guid future;
        using (var scope = factory.Services.CreateScope()) future = await scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>().BattleQuestions.Where(q => q.BattleRoomId == room.Id && q.OrderIndex == 1).Select(q => q.Id).SingleAsync();
        factory.Clock.Advance(TimeSpan.FromSeconds(4));
        Assert.Equal(HttpStatusCode.Conflict, (await host.PostAsJsonAsync($"/api/battles/{room.Id}/questions/{future}/answer", new BattleAnswerRequest("Early"))).StatusCode);
        factory.Clock.Advance(TimeSpan.FromSeconds(11));
        var late = await host.PostAsJsonAsync(url, new BattleAnswerRequest("Definition " + question.Prompt.Split(' ').Last()));
        Assert.Equal(HttpStatusCode.OK, late.StatusCode);
        var ended = (await late.Content.ReadFromJsonAsync<BattleRoomDto>())!;
        Assert.Equal(0, ended.MyScore); Assert.Equal("Wrong", ended.MyAnswer!.Evaluation);
        await host.GetAsync($"/api/battles/{room.Id}");
        using var scope2 = factory.Services.CreateScope(); var db = scope2.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
        var answer = await db.BattleAnswers.SingleAsync(a => a.BattleQuestionId == question.Id && a.UserId == userId);
        Assert.True(answer.TimedOut); Assert.Equal(10000, answer.ResponseTimeMs);
        Assert.Single(await db.LearningAttempts.Where(a => a.ClientAttemptId == answer.Id).ToListAsync());
    }

    [Fact]
    public async Task SourceGrounding_IsOnlyDisclosedAfterAnswer()
    {
        using var host = factory.CreateClient(); var userId = await SignIn(host); var room = await Create(host, userId, ["FillBlank"]);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
            var q = await db.BattleQuestions.SingleAsync(q => q.BattleRoomId == room.Id && q.OrderIndex == 0);
            q.SourceJson = JsonSerializer.Serialize(new { name = "Physics", snippet = "Secret grounded explanation" }); await db.SaveChangesAsync();
        }
        await host.PostAsync($"/api/battles/{room.Id}/start", null); factory.Clock.Advance(TimeSpan.FromSeconds(4));
        var state = await State(host, room.Id);
        Assert.DoesNotContain("Secret grounded", JsonSerializer.Serialize(state));
        var answer = await host.PostAsJsonAsync($"/api/battles/{room.Id}/questions/{state.Question!.Id}/answer", new BattleAnswerRequest("Wrong"));
        Assert.Contains("Secret grounded explanation", await answer.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExistingQuizWithoutFlashcards_CanCreateAndPlayBattle()
    {
        using var host = factory.CreateClient(); var userId = await SignIn(host); Guid setId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
            var subject = Subject.Create(userId, "Manual quiz", null); var set = StudySet.Create(subject.Id, "Quiz source", null); setId = set.Id;
            var quiz = Quiz.Create(set.Id, "Saved questions", 10); db.Subjects.Add(subject); db.StudySets.Add(set); db.Quizzes.Add(quiz);
            for (var i = 0; i < 10; i++)
            {
                var q = Question.CreateGenerated(quiz.Id, QuestionType.MultipleChoice, $"Question {i}", "Reason", i);
                db.Questions.Add(q); db.AnswerOptions.AddRange(AnswerOption.Create(q.Id, "Correct", true, 0), AnswerOption.Create(q.Id, "Wrong", false, 1));
            }
            await db.SaveChangesAsync();
        }
        var response = await host.PostAsJsonAsync("/api/battles", new CreateBattleRequest(setId, "Quiz battle", QuestionTypes: ["MultipleChoice"]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var room = (await response.Content.ReadFromJsonAsync<BattleRoomDto>())!;
        await host.PostAsync($"/api/battles/{room.Id}/start", null); factory.Clock.Advance(TimeSpan.FromSeconds(4));
        var state = await State(host, room.Id); var question = state.Question!;
        Assert.Equal(HttpStatusCode.BadRequest, (await host.PostAsJsonAsync($"/api/battles/{room.Id}/questions/{question.Id}/answer", new BattleAnswerRequest("Correct"))).StatusCode);
        var correct = question.Options.Single(x => x.Text == "Correct");
        Assert.Equal(HttpStatusCode.OK, (await host.PostAsJsonAsync($"/api/battles/{room.Id}/questions/{question.Id}/answer", new BattleAnswerRequest(correct.Id))).StatusCode);
        using var scope2 = factory.Services.CreateScope(); var db2 = scope2.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
        Assert.NotNull((await db2.BattleQuestions.SingleAsync(q => q.Id == question.Id)).SourceQuestionId);
        var answer = await db2.BattleAnswers.SingleAsync(a => a.BattleQuestionId == question.Id);
        Assert.True(await db2.LearningAttempts.AnyAsync(a => a.ClientAttemptId == answer.Id && a.SrsCommitted));
    }

    [Fact]
    public async Task DuplicateContent_AndInvalidSettings_CannotCreateRoom()
    {
        using var host = factory.CreateClient(); var userId = await SignIn(host); var room = await Create(host, userId);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.PostAsJsonAsync("/api/battles", new CreateBattleRequest(room.StudySetId, "Invalid", MaxPlayers: 500))).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
            foreach (var card in await db.Flashcards.Where(c => c.StudySetId == room.StudySetId).ToListAsync()) card.Update("Same   concept", "Same answer", null);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await host.PostAsJsonAsync("/api/battles", new CreateBattleRequest(room.StudySetId, "Duplicates", QuestionTypes: ["FillBlank"]))).StatusCode);
    }

    [Fact]
    public async Task Expiration_AndCancellation_AreTerminal()
    {
        using var host = factory.CreateClient(); var userId = await SignIn(host); var room = await Create(host, userId);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
            (await db.BattleRooms.SingleAsync(r => r.Id == room.Id)).ExpiresAt = factory.Clock.GetUtcNow().AddSeconds(-1); await db.SaveChangesAsync();
        }
        Assert.Equal("Expired", (await State(host, room.Id)).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await host.PostAsync($"/api/battles/{room.Id}/start", null)).StatusCode);
        var next = await Create(host, userId);
        Assert.Equal(HttpStatusCode.OK, (await host.PostAsync($"/api/battles/{next.Id}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.PostAsync($"/api/battles/{next.Id}/start", null)).StatusCode);
    }

    private static async Task<Guid> SignIn(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"battle-{Guid.NewGuid():N}@studyflow.test", "Password1", "Battle Student"));
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth.User.Id;
    }
    private async Task<BattleRoomDto> Create(HttpClient host, Guid userId, string[]? types = null, bool ordering = false, int? timer = 20)
    {
        Guid setId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
            var subject = Subject.Create(userId, "Physics", null); var set = StudySet.Create(subject.Id, "Motion", null); setId = set.Id;
            db.Subjects.Add(subject); db.StudySets.Add(set);
            for (var i = 0; i < 12; i++) db.Flashcards.Add(Flashcard.Create(set.Id, $"Term {i}", ordering ? $"1. First {i}\n2. Second {i}\n3. Last {i}" : $"Definition {i}", $"Explanation {i}", i));
            await db.SaveChangesAsync();
        }
        var response = await host.PostAsJsonAsync("/api/battles", new CreateBattleRequest(setId, "Physics live", TimeLimitSeconds: timer, QuestionTypes: types ?? ["MultipleChoice"], Mode: "Mixed"));
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<BattleRoomDto>())!;
    }
    private static async Task<BattleRoomDto> State(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"/api/battles/{id}");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<BattleRoomDto>())!;
    }
}
