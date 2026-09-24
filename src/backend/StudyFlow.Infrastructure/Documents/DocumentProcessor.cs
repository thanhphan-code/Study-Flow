using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;
using StudyFlow.Infrastructure.Persistence;

namespace StudyFlow.Infrastructure.Documents;

internal sealed class DocumentProcessor(StudyFlowDbContext dbContext, IFileStorageService storage, IEnumerable<IDocumentTextExtractor> extractors, IDocumentChunker chunker, IOptions<DocumentProcessingOptions> options, TimeProvider timeProvider, ILogger<DocumentProcessor> logger) : IDocumentProcessor
{
    private readonly DocumentProcessingOptions _options = options.Value;
    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var claim = await ClaimAsync(documentId, cancellationToken); if (claim is null) return;
        var started = timeProvider.GetTimestamp();
        try
        {
            await using var stream = await storage.OpenReadAsync(claim.StorageKey, cancellationToken);
            var extractor = extractors.SingleOrDefault(x => x.CanHandle(claim.FileType)) ?? throw new DocumentProcessingException("DOCUMENT_UNSUPPORTED_TYPE", "No extractor supports this document type.", false);
            var extraction = await extractor.ExtractAsync(stream, cancellationToken); var drafts = chunker.Chunk(extraction);
            await CompleteAsync(claim.DocumentId, claim.JobId, extraction, drafts, cancellationToken);
            logger.LogInformation("DocumentProcessingCompleted DocumentId={DocumentId} UserId={UserId} JobId={JobId} Attempt={Attempt} Extractor={Extractor} DurationMs={DurationMs} ChunkCount={ChunkCount} ExtractedCharacterCount={Characters}", claim.DocumentId, claim.UserId, claim.JobId, claim.Attempt, extractor.GetType().Name, timeProvider.GetElapsedTime(started).TotalMilliseconds, drafts.Count, extraction.Sections.Sum(x => x.Content.Length));
        }
        catch (OperationCanceledException) { await FailAsync(claim, new DocumentProcessingException("DOCUMENT_PROCESSING_TIMEOUT", "Document processing timed out.", true), CancellationToken.None); }
        catch (Exception exception) when (exception is not OperationCanceledException) { await FailAsync(claim, Classify(exception), CancellationToken.None); }
    }

    public async Task RecoverStaleAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow(); var staleBefore = now.AddMinutes(-_options.ProcessingTimeoutMinutes);
        var jobs = await dbContext.DocumentProcessingJobs.Where(x => x.Status == DocumentProcessingJobStatus.Processing && x.StartedAt < staleBefore).ToListAsync(cancellationToken);
        foreach (var job in jobs)
        {
            var document = await dbContext.Documents.SingleOrDefaultAsync(x => x.Id == job.DocumentId, cancellationToken); if (document is null || !document.IsProcessing(job.Id)) continue;
            const string code = "DOCUMENT_PROCESSING_TIMEOUT"; const string message = "Document processing did not complete before the timeout.";
            if (job.AttemptCount >= _options.MaxAttempts) { job.Fail(code, message, now); document.Fail(code, message, now); }
            else { var available = now; job.Requeue(code, message, available, now); document.Requeue(code, message, now); }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Claim?> ClaimAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var isPostgres = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true;
        await using var transaction = isPostgres ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        if (isPostgres)
            await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [$"document:{documentId}"], cancellationToken);
        var now = timeProvider.GetUtcNow(); var document = await dbContext.Documents.SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken);
        var job = await dbContext.DocumentProcessingJobs.Where(x => x.DocumentId == documentId && x.Status == DocumentProcessingJobStatus.Queued && x.AvailableAt <= now).OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (document is null || job is null || !job.TryStart(now) || !document.TryBeginProcessing(job.Id, now)) { if (transaction is not null) await transaction.CommitAsync(cancellationToken); return null; }
        await dbContext.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new(document.Id, document.UserId, document.StorageKey, document.FileType, job.Id, job.AttemptCount);
    }

    private async Task CompleteAsync(Guid documentId, Guid jobId, DocumentExtractionResult extraction, IReadOnlyList<DocumentChunkDraft> drafts, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear(); var isPostgres = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true; await using var transaction = isPostgres ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        var document = await dbContext.Documents.SingleAsync(x => x.Id == documentId, cancellationToken); var job = await dbContext.DocumentProcessingJobs.SingleAsync(x => x.Id == jobId, cancellationToken);
        if (!document.IsProcessing(jobId) || job.Status != DocumentProcessingJobStatus.Processing) { if (transaction is not null) await transaction.CommitAsync(cancellationToken); return; }
        var previous = await dbContext.DocumentChunks.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken); dbContext.DocumentChunks.RemoveRange(previous); await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.DocumentChunks.AddRange(drafts.Select(x => DocumentChunk.Create(documentId, x.ChunkIndex, x.Content, x.PageNumber, x.SlideNumber, x.SectionTitle, x.StartOffset, x.EndOffset, x.ContentHash)));
        var allText = string.Join("\n\n", extraction.Sections.Select(x => x.Content)); var preview = allText[..Math.Min(allText.Length, 60_000)]; var now = timeProvider.GetUtcNow(); document.Complete(preview, now); job.Complete(now);
        await dbContext.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    private async Task FailAsync(Claim claim, DocumentProcessingException error, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear(); var document = await dbContext.Documents.SingleOrDefaultAsync(x => x.Id == claim.DocumentId, cancellationToken); var job = await dbContext.DocumentProcessingJobs.SingleOrDefaultAsync(x => x.Id == claim.JobId, cancellationToken);
        if (document is null || job is null || !document.IsProcessing(job.Id) || job.Status != DocumentProcessingJobStatus.Processing) return;
        var now = timeProvider.GetUtcNow();
        if (error.Retryable && job.AttemptCount < _options.MaxAttempts) { var delay = job.AttemptCount == 1 ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(2); job.Requeue(error.Code, error.Message, now.Add(delay), now); document.Requeue(error.Code, error.Message, now); }
        else { job.Fail(error.Code, error.Message, now); document.Fail(error.Code, error.Message, now); }
        await dbContext.SaveChangesAsync(cancellationToken); logger.LogWarning(error, "DocumentProcessingFailed DocumentId={DocumentId} UserId={UserId} JobId={JobId} Attempt={Attempt} FailureCode={FailureCode} Retryable={Retryable}", claim.DocumentId, claim.UserId, claim.JobId, claim.Attempt, error.Code, error.Retryable);
    }

    private static DocumentProcessingException Classify(Exception exception) => exception switch
    {
        DocumentProcessingException known => known,
        IOException => new("DOCUMENT_STORAGE_FAILED", "The document file could not be read from storage.", true, exception),
        TimeoutException => new("DOCUMENT_PROCESSING_TIMEOUT", "Document processing timed out.", true, exception),
        _ => new("DOCUMENT_PROCESSING_FAILED", "Document processing failed.", false, exception)
    };
    private sealed record Claim(Guid DocumentId, Guid UserId, string StorageKey, DocumentFileType FileType, Guid JobId, int Attempt);
}
