using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyFlow.Application.AI.DTOs;
using StudyFlow.Application.AI.Interfaces;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;
namespace StudyFlow.Tests.AI;
public sealed class AIApiTests:IClassFixture<AuthApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AIApiTests(AuthApiFactory factory)=>_factory=factory.WithWebHostBuilder(builder=>builder.ConfigureTestServices(services=>{services.RemoveAll<IAIService>();services.AddSingleton<IAIService,FakeAIService>();}));
    [Fact]
    public async Task GenerateFromDocument_ReturnsDraftWithoutSavingContent()
    {
        using var client=_factory.CreateClient();var data=await CreateData(client);var jobsResponse=await client.PostAsJsonAsync($"/api/study-sets/{data.Set.Id}/ai/generate",new{documentId=data.DocumentId,pastedText=(string?)null,generateFlashcards=true,generateQuiz=true,flashcardCount=2,quizQuestionCount=1,difficulty="Mixed",documentOnly=true});Assert.Equal(HttpStatusCode.OK,jobsResponse.StatusCode);var jobs=await jobsResponse.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(2,jobs.GetArrayLength());Assert.All(jobs.EnumerateArray(),job=>Assert.Equal("Completed",job.GetProperty("status").GetString()));Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>($"/api/study-sets/{data.Set.Id}/flashcards"))!);
    }
    [Fact]
    public async Task SaveReviewedDraft_PersistsOnce()
    {
        using var client=_factory.CreateClient();var data=await CreateData(client);var jobs=await (await client.PostAsJsonAsync($"/api/study-sets/{data.Set.Id}/ai/generate",new{documentId=data.DocumentId,generateFlashcards=true,generateQuiz=false,flashcardCount=2,quizQuestionCount=1,difficulty="Easy",documentOnly=true})).Content.ReadFromJsonAsync<JsonElement>();var job=jobs[0];var id=job.GetProperty("id").GetGuid();var cards=JsonNode.Parse(job.GetProperty("draft").GetProperty("flashcards").GetRawText())!.AsArray();foreach(var card in cards)card!["sourceIds"]=new JsonArray("C999");var save=await client.PostAsJsonAsync($"/api/ai/jobs/{id}/save",new{flashcards=cards,questions=Array.Empty<object>(),quizTitle="AI Quiz"});Assert.Equal(HttpStatusCode.OK,save.StatusCode);var savedCards=(await client.GetFromJsonAsync<JsonElement[]>($"/api/study-sets/{data.Set.Id}/flashcards"))!;Assert.Equal(2,savedCards.Length);var cardId=savedCards[0].GetProperty("id").GetGuid();var sources=await client.GetFromJsonAsync<JsonElement[]>($"/api/flashcards/{cardId}/sources");Assert.Single(sources!);Assert.Equal(data.DocumentId,sources![0].GetProperty("documentId").GetGuid());Assert.Equal("physics.txt",sources[0].GetProperty("documentName").GetString());Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/ai/jobs/{id}/save",new{flashcards=cards,questions=Array.Empty<object>(),quizTitle="AI Quiz"})).StatusCode);

        var explanation=await(await client.PostAsync($"/api/flashcards/{cardId}/ai/explain",null)).Content.ReadFromJsonAsync<JsonElement>();Assert.Equal("Explanation from material",explanation.GetProperty("text").GetString());Assert.Single(explanation.GetProperty("sources").EnumerateArray());var explanationJobId=explanation.GetProperty("jobId").GetGuid();Assert.Single((await client.GetFromJsonAsync<JsonElement[]>($"/api/ai/jobs/{explanationJobId}/sources"))!);

        using var attacker=_factory.CreateClient();await Authenticate(attacker);Assert.Equal(HttpStatusCode.NotFound,(await attacker.GetAsync($"/api/flashcards/{cardId}/sources")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await attacker.GetAsync($"/api/ai/jobs/{explanationJobId}/sources")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync($"/api/documents/{data.DocumentId}")).StatusCode);sources=await client.GetFromJsonAsync<JsonElement[]>($"/api/flashcards/{cardId}/sources");Assert.Single(sources!);Assert.False(sources![0].GetProperty("documentAvailable").GetBoolean());Assert.Equal("physics.txt",sources[0].GetProperty("documentName").GetString());
    }
    [Fact]
    public async Task Generate_WithAnotherUsersDocument_Returns404()
    {
        using var owner=_factory.CreateClient();using var attacker=_factory.CreateClient();var data=await CreateData(owner);var attackerSet=await CreateSet(attacker);var response=await attacker.PostAsJsonAsync($"/api/study-sets/{attackerSet.Id}/ai/generate",new{documentId=data.DocumentId,generateFlashcards=true,generateQuiz=false,flashcardCount=1,quizQuestionCount=1,difficulty="Mixed",documentOnly=true});Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
    }
    [Fact]
    public async Task Generate_WithFailedDocument_Returns409()
    {
        using var client=_factory.CreateClient();var set=await CreateSet(client);using var form=new MultipartFormDataContent();var file=new ByteArrayContent("%PDF-broken"u8.ToArray());file.Headers.ContentType=MediaTypeHeaderValue.Parse("application/pdf");form.Add(file,"file","broken.pdf");var upload=await client.PostAsync($"/api/study-sets/{set.Id}/documents",form);var json=await upload.Content.ReadFromJsonAsync<JsonElement>();var id=json.GetProperty("id").GetGuid();for(var attempt=0;attempt<50;attempt++){var detail=await client.GetFromJsonAsync<JsonElement>($"/api/documents/{id}");if(detail.GetProperty("processingStatus").GetString()=="Failed")break;await Task.Delay(100);}var response=await client.PostAsJsonAsync($"/api/study-sets/{set.Id}/ai/generate",new{documentId=id,generateFlashcards=true,generateQuiz=false,flashcardCount=1,quizQuestionCount=1,difficulty="Mixed",documentOnly=true});Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);var error=await response.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal("DOCUMENT_NOT_READY",error.GetProperty("code").GetString());
    }
    [Fact]
    public async Task GenerateQuiz_SavesSourceForEveryQuestion()
    {
        using var client=_factory.CreateClient();var data=await CreateData(client);var jobs=await(await client.PostAsJsonAsync($"/api/study-sets/{data.Set.Id}/ai/generate",new{documentId=data.DocumentId,generateFlashcards=false,generateQuiz=true,flashcardCount=1,quizQuestionCount=2,difficulty="Mixed",documentOnly=true})).Content.ReadFromJsonAsync<JsonElement>();var job=jobs[0];var save=await client.PostAsJsonAsync($"/api/ai/jobs/{job.GetProperty("id").GetGuid()}/save",new{flashcards=Array.Empty<object>(),questions=job.GetProperty("draft").GetProperty("questions"),quizTitle="Grounded quiz"});Assert.Equal(HttpStatusCode.OK,save.StatusCode);var saved=await save.Content.ReadFromJsonAsync<JsonElement>();var quizId=saved.GetProperty("quizId").GetGuid();var session=await(await client.PostAsync($"/api/quizzes/{quizId}/attempts",null)).Content.ReadFromJsonAsync<JsonElement>();foreach(var question in session.GetProperty("questions").EnumerateArray()){var sources=await client.GetFromJsonAsync<JsonElement[]>($"/api/quiz-questions/{question.GetProperty("id").GetGuid()}/sources");Assert.Single(sources!);Assert.Equal(data.DocumentId,sources![0].GetProperty("documentId").GetGuid());}
    }
    [Fact]
    public async Task Generate_WhenAIOnlyReturnsUnknownSources_FailsGrounding()
    {
        var invalidFactory=_factory.WithWebHostBuilder(builder=>builder.ConfigureTestServices(services=>{services.RemoveAll<IAIService>();services.AddSingleton<IAIService,InvalidSourceAIService>();}));using var client=invalidFactory.CreateClient();var data=await CreateData(client);var response=await client.PostAsJsonAsync($"/api/study-sets/{data.Set.Id}/ai/generate",new{documentId=data.DocumentId,generateFlashcards=true,generateQuiz=false,flashcardCount=1,quizQuestionCount=1,difficulty="Easy",documentOnly=true});Assert.Equal(HttpStatusCode.OK,response.StatusCode);var jobs=await response.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal("Failed",jobs[0].GetProperty("status").GetString());Assert.Equal("AI grounding failed.",jobs[0].GetProperty("errorMessage").GetString());
    }
    private static async Task<(StudySetDto Set,Guid DocumentId)> CreateData(HttpClient client){var set=await CreateSet(client);using var form=new MultipartFormDataContent();var file=new ByteArrayContent(Encoding.UTF8.GetBytes("Force equals mass times acceleration."));file.Headers.ContentType=MediaTypeHeaderValue.Parse("text/plain");form.Add(file,"file","physics.txt");var response=await client.PostAsync($"/api/study-sets/{set.Id}/documents",form);Assert.Equal(HttpStatusCode.Accepted,response.StatusCode);var document=await response.Content.ReadFromJsonAsync<JsonElement>();var id=document.GetProperty("id").GetGuid();for(var attempt=0;attempt<50;attempt++){var detail=await client.GetFromJsonAsync<JsonElement>($"/api/documents/{id}");if(detail.GetProperty("processingStatus").GetString()=="Ready")return(set,id);await Task.Delay(100);}throw new TimeoutException("Document processing did not complete.");}
    private static async Task<StudySetDto> CreateSet(HttpClient client){await Authenticate(client);var subject=await(await client.PostAsJsonAsync("/api/subjects",new CreateSubjectRequest("AI",null))).Content.ReadFromJsonAsync<SubjectDto>();return(await(await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets",new CreateStudySetRequest("Forces",null))).Content.ReadFromJsonAsync<StudySetDto>())!;}
    private static async Task Authenticate(HttpClient client){var response=await client.PostAsJsonAsync("/api/auth/register",new RegisterRequest($"ai-{Guid.NewGuid():N}@studyflow.test","Password1","Learner"));var json=await response.Content.ReadFromJsonAsync<JsonElement>();client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",json.GetProperty("accessToken").GetString());}
}
internal sealed class InvalidSourceAIService:FakeAIService
{
    public override Task<FlashcardGenerationResult> GenerateFlashcardsAsync(AIGenerationContext context,int count,CancellationToken cancellationToken)=>Task.FromResult(new FlashcardGenerationResult([new GeneratedFlashcardDto("Unsupported","Unsupported",null,SourceIds:["C999"])]));
}
internal class FakeAIService:IAIService
{
    public virtual Task<FlashcardGenerationResult> GenerateFlashcardsAsync(AIGenerationContext context,int count,CancellationToken cancellationToken)=>Task.FromResult(new FlashcardGenerationResult(Enumerable.Range(1,count).Select(x=>new GeneratedFlashcardDto($"Question {x}",$"Answer {x}","From material",SourceIds:["C1"])).ToList()));
    public Task<QuizGenerationResult> GenerateQuizAsync(AIGenerationContext context,int count,CancellationToken cancellationToken)=>Task.FromResult(new QuizGenerationResult(Enumerable.Range(1,count).Select(x=>new GeneratedQuestionDto("MultipleChoice",$"Question {x}",null,[new("Correct",true),new("Wrong",false)],SourceIds:["C1"])).ToList()));
    public Task<GroundedTextProviderResult> ExplainAsync(string material,string question,string answer,CancellationToken cancellationToken)=>Task.FromResult(new GroundedTextProviderResult("Explanation from material",["C1"]));
    public Task<GroundedTextProviderResult> GenerateSimilarQuestionAsync(string material,string question,string answer,CancellationToken cancellationToken)=>Task.FromResult(new GroundedTextProviderResult("Similar question and answer",["C1"]));
    public Task<GroundedTextProviderResult> GenerateHintAsync(string material,string question,int level,CancellationToken cancellationToken)=>Task.FromResult(new GroundedTextProviderResult("Think about the governing equation.",["C1"]));
}
