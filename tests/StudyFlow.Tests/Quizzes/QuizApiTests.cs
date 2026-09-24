using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.Quizzes.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;
using Microsoft.Extensions.DependencyInjection;
using StudyFlow.Infrastructure.Persistence;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Tests.Quizzes;

public sealed class QuizApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public QuizApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateAndCompleteQuiz_WithCorrectAnswers_ReturnsPerfectScore()
    {
        using var client = _factory.CreateClient(); var quiz = await CreateQuizAsync(client);
        var start = await client.PostAsync($"/api/quizzes/{quiz.Id}/attempts", null); var rawSession = await start.Content.ReadAsStringAsync();
        Assert.DoesNotContain("isCorrect", rawSession, StringComparison.OrdinalIgnoreCase);
        var session = JsonDocument.Parse(rawSession).RootElement; var attemptId = session.GetProperty("attemptId").GetGuid();
        var answersByQuestion = new Dictionary<string, string> { ["Term 1"] = "Definition 1", ["Term 2"] = "False", ["Term 3"] = "Definition 3" };
        foreach (var question in session.GetProperty("questions").EnumerateArray())
        {
            var expected = answersByQuestion.Single(pair => question.GetProperty("questionText").GetString()!.Contains(pair.Key)).Value;
            var option = question.GetProperty("options").EnumerateArray().Single(value => value.GetProperty("text").GetString() == expected);
            var response = await client.PostAsJsonAsync($"/api/quiz-attempts/{attemptId}/answers", new SubmitQuizAnswerRequest(question.GetProperty("id").GetGuid(), option.GetProperty("id").GetGuid()));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        var complete = await client.PostAsync($"/api/quiz-attempts/{attemptId}/complete", null); var result = await complete.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, result.GetProperty("score").GetDouble()); Assert.Equal(3, result.GetProperty("correctCount").GetInt32());
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>();
            var cardIds = db.Questions.Where(x => x.QuizId == quiz.Id && x.FlashcardId != null).Select(x => x.FlashcardId!.Value).ToList();
            Assert.Equal(3, db.LearningAttempts.Count(x => cardIds.Contains(x.FlashcardId) && x.Mode == StudyMode.Quiz));
            Assert.Equal(3, db.FlashcardProgress.Count(x => cardIds.Contains(x.FlashcardId) && x.CorrectCount == 1));
        }
        var firstQuestion = session.GetProperty("questions")[0]; var firstOption = firstQuestion.GetProperty("options")[0];
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/quiz-attempts/{attemptId}/answers", new SubmitQuizAnswerRequest(firstQuestion.GetProperty("id").GetGuid(), firstOption.GetProperty("id").GetGuid()))).StatusCode);
    }

    [Fact]
    public async Task CompleteQuiz_WithWrongAnswer_ReturnsWrongAnswerReview()
    {
        using var client = _factory.CreateClient(); var quiz = await CreateQuizAsync(client); var session = await (await client.PostAsync($"/api/quizzes/{quiz.Id}/attempts", null)).Content.ReadFromJsonAsync<JsonElement>();
        var question = session.GetProperty("questions")[0]; var wrong = question.GetProperty("options").EnumerateArray().First(x => x.GetProperty("text").GetString() != "Definition 1");
        await client.PostAsJsonAsync($"/api/quiz-attempts/{session.GetProperty("attemptId").GetGuid()}/answers", new SubmitQuizAnswerRequest(question.GetProperty("id").GetGuid(), wrong.GetProperty("id").GetGuid()));
        var result = await (await client.PostAsync($"/api/quiz-attempts/{session.GetProperty("attemptId").GetGuid()}/complete", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("wrongCount").GetInt32() >= 1); Assert.Contains(result.GetProperty("questions").EnumerateArray(), x => !x.GetProperty("isCorrect").GetBoolean() && x.GetProperty("correctAnswer").GetString() == "Definition 1");
    }

    [Fact]
    public async Task QuizEndpoints_WhenUserIsNotOwner_ReturnNotFound()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient(); var quiz = await CreateQuizAsync(owner); await AuthenticateAsync(attacker);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.GetAsync($"/api/study-sets/{quiz.StudySetId}/quizzes")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.PostAsync($"/api/quizzes/{quiz.Id}/attempts", null)).StatusCode);
    }

    [Fact]
    public async Task CreateQuiz_WhenNotEnoughFlashcards_ReturnsValidationError()
    {
        using var client = _factory.CreateClient(); var set = await CreateStudySetAsync(client); await AddCardAsync(client, set.Id, 1);
        var response = await client.PostAsJsonAsync($"/api/study-sets/{set.Id}/quizzes", new CreateQuizRequest("Too large", 2));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("NOT_ENOUGH_FLASHCARDS", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateManualQuiz_WithOptions_CreatesPlayableQuiz()
    {
        using var client = _factory.CreateClient(); var set = await CreateStudySetAsync(client);
        var request = new CreateManualQuizRequest("Vietnam geography", [new("What is the capital of Vietnam?", "The national capital.", [new("Hanoi", true), new("Da Nang", false), new("Ho Chi Minh City", false)])]);
        var response = await client.PostAsJsonAsync($"/api/study-sets/{set.Id}/quizzes/manual", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var quiz = await response.Content.ReadFromJsonAsync<QuizDto>();
        var session = await (await client.PostAsync($"/api/quizzes/{quiz!.Id}/attempts", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(session.GetProperty("questions").EnumerateArray()); Assert.Equal(3, session.GetProperty("questions")[0].GetProperty("options").GetArrayLength());
    }

    [Fact]
    public async Task CreateManualQuiz_WithoutExactlyOneCorrectOption_ReturnsValidationError()
    {
        using var client = _factory.CreateClient(); var set = await CreateStudySetAsync(client);
        var request = new CreateManualQuizRequest("Invalid", [new("Question", null, [new("A", false), new("B", false)])]);
        var response = await client.PostAsJsonAsync($"/api/study-sets/{set.Id}/quizzes/manual", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<QuizDto> CreateQuizAsync(HttpClient client)
    {
        var set = await CreateStudySetAsync(client); for (var index = 1; index <= 3; index++) await AddCardAsync(client, set.Id, index);
        var response = await client.PostAsJsonAsync($"/api/study-sets/{set.Id}/quizzes", new CreateQuizRequest("Core concepts", 3)); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<QuizDto>())!;
    }
    private static async Task<StudySetDto> CreateStudySetAsync(HttpClient client)
    {
        await AuthenticateAsync(client); var subject = await (await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Physics", null))).Content.ReadFromJsonAsync<SubjectDto>();
        return (await (await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets", new CreateStudySetRequest("Motion", null))).Content.ReadFromJsonAsync<StudySetDto>())!;
    }
    private static Task<HttpResponseMessage> AddCardAsync(HttpClient client, Guid setId, int index) => client.PostAsJsonAsync($"/api/study-sets/{setId}/flashcards", new CreateFlashcardRequest($"Term {index}", $"Definition {index}", $"Explanation {index}"));
    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"quiz-{Guid.NewGuid():N}@studyflow.test", "Password1", "Quiz Owner")); var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }
}
