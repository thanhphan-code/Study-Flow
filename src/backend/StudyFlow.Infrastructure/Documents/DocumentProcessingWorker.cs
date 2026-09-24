using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Domain.Enums;
using StudyFlow.Infrastructure.Persistence;

namespace StudyFlow.Infrastructure.Documents;

internal sealed class DocumentProcessingWorker(IServiceScopeFactory scopeFactory, IOptions<DocumentProcessingOptions> options, ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    private readonly DocumentProcessingOptions _options = options.Value;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextRecoveryAt = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var queryScope = scopeFactory.CreateAsyncScope(); var db = queryScope.ServiceProvider.GetRequiredService<StudyFlowDbContext>(); var now = queryScope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
                if (now >= nextRecoveryAt) { await queryScope.ServiceProvider.GetRequiredService<IDocumentProcessor>().RecoverStaleAsync(stoppingToken); nextRecoveryAt = now.AddMinutes(1); }
                var ids = await db.DocumentProcessingJobs.AsNoTracking().Where(x => x.Status == DocumentProcessingJobStatus.Queued && x.AvailableAt <= now).GroupBy(x => x.DocumentId).Select(group => new { DocumentId = group.Key, AvailableAt = group.Min(x => x.AvailableAt) }).OrderBy(x => x.AvailableAt).Take(_options.BatchSize).Select(x => x.DocumentId).ToListAsync(stoppingToken);
                foreach (var id in ids)
                {
                    await using var scope = scopeFactory.CreateAsyncScope(); using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken); timeout.CancelAfter(TimeSpan.FromMinutes(_options.ProcessingTimeoutMinutes));
                    await scope.ServiceProvider.GetRequiredService<IDocumentProcessor>().ProcessAsync(id, timeout.Token);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Document processing worker iteration failed."); }
            await Task.Delay(Math.Max(100, _options.PollIntervalMilliseconds), stoppingToken);
        }
    }
}
