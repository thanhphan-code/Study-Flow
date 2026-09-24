using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;

namespace StudyFlow.Tests.Flashcards;

public sealed class FlashcardApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public FlashcardApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task FlashcardCrud_WhenUserOwnsStudySet_CompletesLifecycle()
    {
        using var client = _factory.CreateClient(); var studySet = await CreateOwnedStudySetAsync(client);
        var create = await client.PostAsJsonAsync($"/api/study-sets/{studySet.Id}/flashcards", new CreateFlashcardRequest("What is velocity?", "Rate of change of position.", null));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode); var card = await create.Content.ReadFromJsonAsync<FlashcardDto>(); Assert.NotNull(card); Assert.Equal(0, card.OrderIndex);
        Assert.Contains((await client.GetFromJsonAsync<List<FlashcardDto>>($"/api/study-sets/{studySet.Id}/flashcards"))!, x => x.Id == card.Id);
        var update = await client.PutAsJsonAsync($"/api/flashcards/{card.Id}", new UpdateFlashcardRequest("Velocity?", "Change in position per unit time.", "A vector quantity."));
        Assert.Equal("Velocity?", (await update.Content.ReadFromJsonAsync<FlashcardDto>())!.FrontText);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/flashcards/{card.Id}")).StatusCode);
    }

    [Fact]
    public async Task BulkCreate_AppendsCardsInStableOrder()
    {
        using var client = _factory.CreateClient(); var studySet = await CreateOwnedStudySetAsync(client);
        await client.PostAsJsonAsync($"/api/study-sets/{studySet.Id}/flashcards", new CreateFlashcardRequest("First", "One", null));
        var request = new BulkCreateFlashcardsRequest([new("Second", "Two", null), new("Third", "Three", "Explanation")]);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/api/study-sets/{studySet.Id}/flashcards/bulk", request)).StatusCode);
        var cards = await client.GetFromJsonAsync<List<FlashcardDto>>($"/api/study-sets/{studySet.Id}/flashcards");
        var values = Assert.IsType<List<FlashcardDto>>(cards);
        Assert.Equal([0, 1, 2], values.Select(x => x.OrderIndex));
        Assert.Equal(["First", "Second", "Third"], values.Select(x => x.FrontText));
    }

    [Fact]
    public async Task CreateFlashcard_InAnotherUsersStudySet_ShouldReturn404()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient(); var studySet = await CreateOwnedStudySetAsync(owner); await AuthenticateAsync(attacker);
        var response = await attacker.PostAsJsonAsync($"/api/study-sets/{studySet.Id}/flashcards", new CreateFlashcardRequest("Attack", "Blocked", null));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); Assert.Contains("STUDY_SET_NOT_FOUND", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FlashcardEndpoints_WhenUserIsNotOwner_ReturnNotFound()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient(); var studySet = await CreateOwnedStudySetAsync(owner); await AuthenticateAsync(attacker);
        var created = await owner.PostAsJsonAsync($"/api/study-sets/{studySet.Id}/flashcards", new CreateFlashcardRequest("Private", "Answer", null)); var card = await created.Content.ReadFromJsonAsync<FlashcardDto>();
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.GetAsync($"/api/study-sets/{studySet.Id}/flashcards")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.PutAsJsonAsync($"/api/flashcards/{card!.Id}", new UpdateFlashcardRequest("Stolen", "No", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.DeleteAsync($"/api/flashcards/{card.Id}")).StatusCode);
    }

    [Fact]
    public async Task CreateFlashcard_WhenFrontOrBackIsEmpty_ReturnsValidationError()
    {
        using var client = _factory.CreateClient(); var studySet = await CreateOwnedStudySetAsync(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/study-sets/{studySet.Id}/flashcards", new CreateFlashcardRequest("", "", null))).StatusCode);
    }

    [Fact]
    public async Task LanguageFlashcard_PersistsPronunciationMetadata()
    {
        using var client = _factory.CreateClient(); var studySet = await CreateOwnedStudySetAsync(client);
        var response = await client.PostAsJsonAsync($"/api/study-sets/{studySet.Id}/flashcards", new CreateFlashcardRequest("食べる", "Ăn", null, "ja-JP", "たべる", "taberu"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var card = await response.Content.ReadFromJsonAsync<FlashcardDto>();
        Assert.NotNull(card); Assert.Equal("ja-JP", card.LanguageCode); Assert.Equal("たべる", card.ReadingText); Assert.Equal("taberu", card.Romanization);
        var listed = await client.GetFromJsonAsync<List<FlashcardDto>>($"/api/study-sets/{studySet.Id}/flashcards");
        Assert.Contains(listed!, value => value.Id == card.Id && value.ReadingText == "たべる");
    }

    [Fact]
    public async Task CreateLanguageFlashcard_WithInvalidLanguageCode_ReturnsValidationError()
    {
        using var client = _factory.CreateClient(); var studySet = await CreateOwnedStudySetAsync(client);
        var response = await client.PostAsJsonAsync($"/api/study-sets/{studySet.Id}/flashcards", new CreateFlashcardRequest("Bonjour", "Xin chào", null, "not a language", null, null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FlashcardImage_ValidatesFileAndEnforcesOwnership()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient(); var studySet = await CreateOwnedStudySetAsync(owner); await AuthenticateAsync(attacker);
        var created = await owner.PostAsJsonAsync($"/api/study-sets/{studySet.Id}/flashcards", new CreateFlashcardRequest("Cell", "Basic unit of life", null)); var card = await created.Content.ReadFromJsonAsync<FlashcardDto>();
        using var image = new MultipartFormDataContent(); var bytes = new ByteArrayContent([137, 80, 78, 71, 13, 10, 26, 10, 0]); bytes.Headers.ContentType = new MediaTypeHeaderValue("image/png"); image.Add(bytes, "file", "cell.png");
        var upload = await owner.PostAsync($"/api/flashcards/{card!.Id}/image", image);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode); Assert.NotNull((await upload.Content.ReadFromJsonAsync<FlashcardDto>())!.ImageUrl);
        var downloaded = await owner.GetAsync($"/api/flashcards/{card.Id}/image"); Assert.Equal(HttpStatusCode.OK, downloaded.StatusCode); Assert.Equal("image/png", downloaded.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.GetAsync($"/api/flashcards/{card.Id}/image")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/flashcards/{card.Id}/image")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/flashcards/{card.Id}/image")).StatusCode);
    }

    private static async Task<StudySetDto> CreateOwnedStudySetAsync(HttpClient client)
    {
        await AuthenticateAsync(client);
        var subjectResponse = await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Physics", null)); var subject = await subjectResponse.Content.ReadFromJsonAsync<SubjectDto>();
        var setResponse = await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets", new CreateStudySetRequest("Chapter 1", null)); return (await setResponse.Content.ReadFromJsonAsync<StudySetDto>())!;
    }
    private static async Task AuthenticateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"card-{Guid.NewGuid():N}@studyflow.test", "Password1", "Card Owner")); var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }
}
