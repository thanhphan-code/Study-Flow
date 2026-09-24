using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;
using StudyFlow.Infrastructure.Persistence;

namespace StudyFlow.Infrastructure.Documents;

internal sealed class DocumentProcessingQueue(StudyFlowDbContext dbContext, TimeProvider timeProvider) : IDocumentProcessingQueue
{
    public async Task EnqueueAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var isPostgres = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true;
        await using var transaction = isPostgres && dbContext.Database.CurrentTransaction is null ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        if (isPostgres)
            await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [$"document:{documentId}"], cancellationToken);
        var document = await dbContext.Documents.SingleAsync(x => x.Id == documentId, cancellationToken);
        var active = await dbContext.DocumentProcessingJobs.AnyAsync(x => x.DocumentId == documentId && (x.Status == DocumentProcessingJobStatus.Queued || x.Status == DocumentProcessingJobStatus.Processing), cancellationToken);
        if (active) { if (transaction is not null) await transaction.CommitAsync(cancellationToken); return; }
        var now = timeProvider.GetUtcNow(); document.Queue(now); dbContext.DocumentProcessingJobs.Add(DocumentProcessingJob.Create(documentId, now)); await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }
}
