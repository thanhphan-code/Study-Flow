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

public sealed class LearningInsightsApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public LearningInsightsApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Overview_CountsUnreviewedCardsAsNewAndDetectsWeakCards()
    {
        using var client = _factory.CreateClient(); var data = await CreateSetAsync(client, 2);
        await client.PostAsJsonAsync($"/api/flashcards/{data.Cards[0].Id}/review", new { rating = "Again" });
        var overview = await client.GetFromJsonAsync<JsonElement>("/api/progress/overview");
        Assert.Equal(2, overview.GetProperty("totalCards").GetInt32());
        Assert.Equal(1, overview.GetProperty("newCards").GetInt32());
        Assert.Equal(1, overview.GetProperty("learningCards").GetInt32());
        Assert.Equal(1, overview.GetProperty("weakCards").GetInt32());
        Assert.Equal(1, overview.GetProperty("recommendedReviewCards").GetInt32());
    }

    [Fact]
    public async Task WeakCards_ReturnOnlyCurrentUsersCards()
    {
        using var first = _factory.CreateClient(); using var second = _factory.CreateClient();
        var firstData = await CreateSetAsync(first, 1); var secondData = await CreateSetAsync(second, 1);
        await first.PostAsJsonAsync($"/api/flashcards/{firstData.Cards[0].Id}/review", new { rating = "Again" });
        await second.PostAsJsonAsync($"/api/flashcards/{secondData.Cards[0].Id}/review", new { rating = "Again" });
        var weak = await first.GetFromJsonAsync<JsonElement>("/api/progress/weak-cards");
        Assert.Equal(1, weak.GetArrayLength());
        Assert.Equal(firstData.Cards[0].Id, weak[0].GetProperty("flashcardId").GetGuid());
    }

    [Fact]
    public async Task StudySetProgress_ForAnotherUsersSet_Returns404()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient();
        var data = await CreateSetAsync(owner, 1); await AuthenticateAsync(attacker);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.GetAsync($"/api/study-sets/{data.Set.Id}/progress")).StatusCode);
    }

    [Fact]
    public async Task StudySetProgress_ForOwnedSet_ReturnsCards()
    {
        using var client = _factory.CreateClient(); var data = await CreateSetAsync(client, 2);
        var response = await client.GetAsync($"/api/study-sets/{data.Set.Id}/progress");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var progress = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, progress.GetProperty("totalCards").GetInt32());
    }

    private static async Task<(StudySetDto Set, List<FlashcardDto> Cards)> CreateSetAsync(HttpClient client, int count)
    {
        await AuthenticateAsync(client);
        var subject = await (await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Insights", null))).Content.ReadFromJsonAsync<SubjectDto>();
        var set = await (await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets", new CreateStudySetRequest("Weak areas", null))).Content.ReadFromJsonAsync<StudySetDto>();
        var cards = new List<FlashcardDto>();
        for (var index = 0; index < count; index++) cards.Add((await (await client.PostAsJsonAsync($"/api/study-sets/{set!.Id}/flashcards", new CreateFlashcardRequest($"Question {index}", $"Answer {index}", null))).Content.ReadFromJsonAsync<FlashcardDto>())!);
        return (set!, cards);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"insights-{Guid.NewGuid():N}@studyflow.test", "Password1", "Learner"));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }
}
