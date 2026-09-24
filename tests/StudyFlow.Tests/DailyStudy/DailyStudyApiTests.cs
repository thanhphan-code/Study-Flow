using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;

namespace StudyFlow.Tests.DailyStudy;

public sealed class DailyStudyApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public DailyStudyApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task DailyStudy_ReturnsOnlyCurrentUsersCards()
    {
        using var owner = _factory.CreateClient(); using var other = _factory.CreateClient();
        var ownerSet = await CreateSetAsync(owner, "Owner"); var otherSet = await CreateSetAsync(other, "Other");
        await owner.PostAsJsonAsync($"/api/study-sets/{ownerSet.Id}/flashcards", new CreateFlashcardRequest("Owner card", "Answer", null));
        await other.PostAsJsonAsync($"/api/study-sets/{otherSet.Id}/flashcards", new CreateFlashcardRequest("Other card", "Secret", null));

        var plan = await owner.GetFromJsonAsync<JsonElement>("/api/daily-study?minutes=5&focus=Balanced");
        var items = plan.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, item => item.GetProperty("frontText").GetString() == "Owner card");
        Assert.DoesNotContain(items, item => item.GetProperty("frontText").GetString() == "Other card");
    }

    [Fact]
    public async Task LanguageFocus_ReturnsOnlyLanguageCardsWithContext()
    {
        using var client = _factory.CreateClient(); var set = await CreateSetAsync(client, "Languages");
        await client.PostAsJsonAsync($"/api/study-sets/{set.Id}/flashcards", new CreateFlashcardRequest("食べる", "Ăn", null, "ja-JP", "たべる", "taberu", "毎朝パンを食べる。", "Tôi ăn bánh mì mỗi sáng.", "Think of a table", "to eat"));
        await client.PostAsJsonAsync($"/api/study-sets/{set.Id}/flashcards", new CreateFlashcardRequest("Velocity", "Speed with direction", null));

        var plan = await client.GetFromJsonAsync<JsonElement>("/api/daily-study?minutes=5&focus=Language");
        var item = Assert.Single(plan.GetProperty("items").EnumerateArray());
        Assert.Equal("ja-JP", item.GetProperty("languageCode").GetString());
        Assert.Equal("毎朝パンを食べる。", item.GetProperty("exampleText").GetString());
    }

    private static async Task<StudySetDto> CreateSetAsync(HttpClient client, string name)
    {
        var auth = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"daily-{Guid.NewGuid():N}@studyflow.test", "Password1", name));
        var json = await auth.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        var subject = await (await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest(name, null))).Content.ReadFromJsonAsync<SubjectDto>();
        return (await (await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets", new CreateStudySetRequest(name, null))).Content.ReadFromJsonAsync<StudySetDto>())!;
    }
}
