using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class DocumentProcessingJob : BaseEntity
{
    private DocumentProcessingJob() { }
    private DocumentProcessingJob(Guid documentId, DateTimeOffset now) { DocumentId = documentId; Status = DocumentProcessingJobStatus.Queued; AvailableAt = now; CreatedAt = now; UpdatedAt = now; }
    public Guid DocumentId { get; private init; }
    public DocumentProcessingJobStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public static DocumentProcessingJob Create(Guid documentId, DateTimeOffset now) => new(documentId, now);
    public bool TryStart(DateTimeOffset now) { if (Status != DocumentProcessingJobStatus.Queued || AvailableAt > now) return false; Status = DocumentProcessingJobStatus.Processing; AttemptCount++; StartedAt = now; UpdatedAt = now; return true; }
    public void Complete(DateTimeOffset now) { Status = DocumentProcessingJobStatus.Completed; CompletedAt = now; LastErrorCode = null; LastErrorMessage = null; UpdatedAt = now; }
    public void Requeue(string code, string message, DateTimeOffset availableAt, DateTimeOffset now) { Status = DocumentProcessingJobStatus.Queued; AvailableAt = availableAt; StartedAt = null; LastErrorCode = code; LastErrorMessage = message; UpdatedAt = now; }
    public void Fail(string code, string message, DateTimeOffset now) { Status = DocumentProcessingJobStatus.Failed; LastErrorCode = code; LastErrorMessage = message; CompletedAt = now; UpdatedAt = now; }
}
