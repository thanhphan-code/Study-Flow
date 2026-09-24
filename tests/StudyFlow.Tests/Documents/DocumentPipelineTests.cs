using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;
using StudyFlow.Infrastructure.Documents;
using StudyFlow.Infrastructure.Persistence;
using StudyFlow.Tests.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StudyFlow.Tests.Documents;

public sealed class DocumentPipelineTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    public DocumentPipelineTests(AuthApiFactory factory) => _factory = factory;
    [Fact]
    public void Chunker_PreservesSourceMetadataAndCreatesOverlap()
    {
        var text = string.Join(" ", Enumerable.Range(0, 900).Select(index => $"Sentence {index}."));
        var chunks = new DocumentChunker().Chunk(new DocumentExtractionResult([new(text, PageNumber: 7, SectionTitle: "Motion")]));

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => { Assert.Equal(7, chunk.PageNumber); Assert.Equal("Motion", chunk.SectionTitle); Assert.InRange(chunk.Content.Length, 1, 2500); Assert.Equal(64, chunk.ContentHash.Length); });
        Assert.Contains(chunks[0].Content[^100..], chunks[1].Content);
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(x => x.ChunkIndex));
    }

    [Fact]
    public void Chunker_RejectsEmptyExtraction()
    {
        var exception = Assert.Throws<DocumentProcessingException>(() => new DocumentChunker().Chunk(new DocumentExtractionResult([new("  ")])));
        Assert.Equal("DOCUMENT_EMPTY", exception.Code); Assert.False(exception.Retryable);
    }

    [Fact]
    public void DocumentAndJob_EnforceStateTransitions()
    {
        var now = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero); var document = Document.Create(Guid.NewGuid(), null, null, "stored.txt", "notes.txt", "text/plain", DocumentFileType.Txt, 10, "key"); var job = DocumentProcessingJob.Create(document.Id, now);
        document.Queue(now); Assert.True(job.TryStart(now)); Assert.True(document.TryBeginProcessing(job.Id, now)); Assert.False(document.TryBeginProcessing(Guid.NewGuid(), now));
        document.Complete("preview", now.AddSeconds(1)); job.Complete(now.AddSeconds(1));
        Assert.Equal(DocumentProcessingStatus.Ready, document.ProcessingStatus); Assert.Equal(DocumentProcessingJobStatus.Completed, job.Status); Assert.Null(document.ActiveProcessingJobId);
    }

    [Fact]
    public async Task Recovery_RequeuesStaleProcessingJob()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["DocumentProcessing:PollIntervalMilliseconds"] = "60000" })));
        using var client = factory.CreateClient(); await Task.Delay(150); var old = _factory.Clock.GetUtcNow().AddHours(-1); Guid documentId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>(); var document = Document.Create(Guid.NewGuid(), null, null, "stale.txt", "stale.txt", "text/plain", DocumentFileType.Txt, 10, "missing"); var job = DocumentProcessingJob.Create(document.Id, old); document.Queue(old); Assert.True(job.TryStart(old)); Assert.True(document.TryBeginProcessing(job.Id, old)); db.Documents.Add(document); db.DocumentProcessingJobs.Add(job); await db.SaveChangesAsync(); documentId = document.Id;
        }
        await using (var scope = factory.Services.CreateAsyncScope()) await scope.ServiceProvider.GetRequiredService<IDocumentProcessor>().RecoverStaleAsync(CancellationToken.None);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StudyFlowDbContext>(); var document = await db.Documents.AsNoTracking().SingleAsync(x => x.Id == documentId); var job = await db.DocumentProcessingJobs.AsNoTracking().SingleAsync(x => x.DocumentId == documentId);
            Assert.Equal(DocumentProcessingStatus.Queued, document.ProcessingStatus); Assert.Equal(1, document.RetryCount); Assert.Equal(DocumentProcessingJobStatus.Queued, job.Status); Assert.Equal(1, job.AttemptCount);
        }
    }
}
