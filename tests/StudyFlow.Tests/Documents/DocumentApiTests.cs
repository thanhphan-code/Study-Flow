using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Tests.Auth;
using StudyFlow.Infrastructure.Persistence;

namespace StudyFlow.Tests.Documents;

public sealed class DocumentApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public DocumentApiTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task UploadTxt_ExtractsTextAndCanBeDeleted()
    {
        using var client = _factory.CreateClient(); await AuthenticateAsync(client);
        var response = await UploadAsync(client, "/api/documents", "notes.txt", "text/plain", Encoding.UTF8.GetBytes("First line\nSecond line"));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode); var accepted = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Queued", accepted.GetProperty("processingStatus").GetString()); var id = accepted.GetProperty("id").GetGuid();
        var document = await WaitForStatusAsync(client, id, "Ready"); Assert.Contains("Second line", document.GetProperty("extractedText").GetString()); Assert.True(document.GetProperty("chunkCount").GetInt32() > 0);
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>("/api/documents"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/documents/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/documents/{id}")).StatusCode);
    }

    [Theory]
    [InlineData("fake.pdf", "application/pdf")]
    [InlineData("fake.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public async Task UploadWithSpoofedExtension_Returns400(string name, string contentType)
    {
        using var client = _factory.CreateClient(); await AuthenticateAsync(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(client, "/api/documents", name, contentType, Encoding.UTF8.GetBytes("not that format"))).StatusCode);
    }

    [Fact]
    public async Task UploadDocx_ExtractsParagraphText()
    {
        using var client = _factory.CreateClient(); await AuthenticateAsync(client); var bytes = OfficeFile("word/document.xml", "<w:document xmlns:w=\"urn:w\"><w:body><w:p><w:r><w:t>Imported lesson</w:t></w:r></w:p></w:body></w:document>");
        var response = await UploadAsync(client, "/api/documents", "lesson.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", bytes);
        var accepted = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(HttpStatusCode.Accepted, response.StatusCode); var id = accepted.GetProperty("id").GetGuid();
        var document = await WaitForStatusAsync(client, id, "Ready"); Assert.Contains("Imported lesson", document.GetProperty("extractedText").GetString());
        await client.DeleteAsync($"/api/documents/{id}");
    }

    [Fact]
    public async Task UploadPptx_CreatesChunksWithSlideMetadata()
    {
        using var client = _factory.CreateClient(); await AuthenticateAsync(client); var bytes = PptxFile("<p:sld xmlns:p=\"urn:p\" xmlns:a=\"urn:a\"><p:cSld><a:p><a:r><a:t>Slide lesson</a:t></a:r></a:p></p:cSld></p:sld>");
        var response = await UploadAsync(client, "/api/documents", "lesson.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", bytes);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode); var accepted = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = accepted.GetProperty("id").GetGuid();
        var document = await WaitForStatusAsync(client, id, "Ready"); Assert.Contains("Slide lesson", document.GetProperty("extractedText").GetString()); Assert.Equal("application/vnd.openxmlformats-officedocument.presentationml.presentation", document.GetProperty("mimeType").GetString());
        using var scope = _factory.Services.CreateScope(); var chunk = await scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>().DocumentChunks.AsNoTracking().SingleAsync(x => x.DocumentId == id); Assert.Equal(1, chunk.SlideNumber);
    }

    [Fact]
    public async Task AnotherUserCannotReadOrDeleteDocument()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient(); await AuthenticateAsync(owner); await AuthenticateAsync(attacker);
        var uploaded = await (await UploadAsync(owner, "/api/documents", "private.txt", "text/plain", "secret"u8.ToArray())).Content.ReadFromJsonAsync<JsonElement>(); var id = uploaded.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.GetAsync($"/api/documents/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await attacker.DeleteAsync($"/api/documents/{id}")).StatusCode);
        Assert.Empty((await attacker.GetFromJsonAsync<JsonElement[]>("/api/documents"))!); await owner.DeleteAsync($"/api/documents/{id}");
    }

    [Fact]
    public async Task UploadToAnotherUsersStudySet_Returns404()
    {
        using var owner = _factory.CreateClient(); using var attacker = _factory.CreateClient(); var set = await CreateSetAsync(owner); await AuthenticateAsync(attacker);
        Assert.Equal(HttpStatusCode.NotFound, (await UploadAsync(attacker, $"/api/study-sets/{set.Id}/documents", "notes.txt", "text/plain", "private"u8.ToArray())).StatusCode);
    }

    [Fact]
    public async Task CorruptedPdf_IsAcceptedThenFailsWithoutRetry()
    {
        using var client = _factory.CreateClient(); await AuthenticateAsync(client);
        var response = await UploadAsync(client, "/api/documents", "broken.pdf", "application/pdf", "%PDF-not-a-real-pdf"u8.ToArray());
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode); var accepted = await response.Content.ReadFromJsonAsync<JsonElement>();
        var document = await WaitForStatusAsync(client, accepted.GetProperty("id").GetGuid(), "Failed");
        Assert.Equal("DOCUMENT_CORRUPTED", document.GetProperty("failureCode").GetString()); Assert.False(document.GetProperty("canRetry").GetBoolean());
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string path, string name, string contentType, byte[] bytes)
    {
        using var form = new MultipartFormDataContent(); var file = new ByteArrayContent(bytes); file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType); form.Add(file, "file", name); return await client.PostAsync(path, form);
    }
    private static byte[] OfficeFile(string entryName, string xml)
    {
        using var result = new MemoryStream(); using (var archive = new ZipArchive(result, ZipArchiveMode.Create, true)) { Write(archive, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"/>"); Write(archive, entryName, xml); } return result.ToArray();
    }
    private static byte[] PptxFile(string slideXml)
    {
        using var result = new MemoryStream(); using (var archive = new ZipArchive(result, ZipArchiveMode.Create, true)) { Write(archive, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"/>"); Write(archive, "ppt/presentation.xml", "<p:presentation xmlns:p=\"urn:p\"/>"); Write(archive, "ppt/slides/slide1.xml", slideXml); } return result.ToArray();
    }
    private static void Write(ZipArchive archive, string name, string value) { using var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.UTF8); writer.Write(value); }
    private static async Task<JsonElement> WaitForStatusAsync(HttpClient client, Guid id, string expected)
    {
        for (var attempt = 0; attempt < 50; attempt++) { var value = await client.GetFromJsonAsync<JsonElement>($"/api/documents/{id}"); var status = value.GetProperty("processingStatus").GetString(); if (status == expected) return value; if (status == "Failed" && expected != "Failed") throw new Xunit.Sdk.XunitException($"Document processing failed: {value}"); await Task.Delay(100); }
        throw new TimeoutException($"Document {id} did not reach {expected}.");
    }
    private static async Task<StudySetDto> CreateSetAsync(HttpClient client) { await AuthenticateAsync(client); var subject = await (await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Imports", null))).Content.ReadFromJsonAsync<SubjectDto>(); return (await (await client.PostAsJsonAsync($"/api/subjects/{subject!.Id}/study-sets", new CreateStudySetRequest("Documents", null))).Content.ReadFromJsonAsync<StudySetDto>())!; }
    private static async Task AuthenticateAsync(HttpClient client) { var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest($"docs-{Guid.NewGuid():N}@studyflow.test", "Password1", "Learner")); var json = await response.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString()); }
}
