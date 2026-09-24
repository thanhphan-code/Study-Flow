using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class Document : BaseEntity
{
    private Document() { }
    private Document(Guid userId, Guid? subjectId, Guid? studySetId, string fileName, string originalFileName, string mimeType, DocumentFileType fileType, long fileSize, string storageKey)
    { UserId = userId; SubjectId = subjectId; StudySetId = studySetId; FileName = fileName; OriginalFileName = originalFileName; MimeType = mimeType; FileType = fileType; FileSize = fileSize; StorageKey = storageKey; ProcessingRevision = "pipeline-v2"; }
    public Guid UserId { get; private init; }
    public Guid? SubjectId { get; private init; }
    public Guid? StudySetId { get; private init; }
    public string FileName { get; private init; } = string.Empty;
    public string OriginalFileName { get; private init; } = string.Empty;
    public string MimeType { get; private init; } = "application/octet-stream";
    public DocumentFileType FileType { get; private init; }
    public long FileSize { get; private init; }
    public string StorageKey { get; private init; } = string.Empty;
    public DocumentProcessingStatus ProcessingStatus { get; private set; } = DocumentProcessingStatus.Uploaded;
    public string? ExtractedText { get; private set; }
    public string? ProcessingError { get; private set; }
    public string? FailureCode { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? QueuedAt { get; private set; }
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public DateTimeOffset? ProcessingCompletedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public Guid? ActiveProcessingJobId { get; private set; }
    public string ProcessingRevision { get; private set; } = "pipeline-v2";
    public static Document Create(Guid userId, Guid? subjectId, Guid? studySetId, string fileName, string originalFileName, string mimeType, DocumentFileType fileType, long fileSize, string storageKey) => new(userId, subjectId, studySetId, fileName, originalFileName, mimeType, fileType, fileSize, storageKey);
    public void Queue(DateTimeOffset now) { ProcessingStatus = DocumentProcessingStatus.Queued; QueuedAt = now; ProcessingError = null; FailureCode = null; FailedAt = null; UpdatedAt = now; }
    public bool TryBeginProcessing(Guid jobId, DateTimeOffset now) { if (ProcessingStatus != DocumentProcessingStatus.Queued) return false; ProcessingStatus = DocumentProcessingStatus.Processing; ActiveProcessingJobId = jobId; ProcessingStartedAt = now; ProcessingCompletedAt = null; UpdatedAt = now; return true; }
    public bool IsProcessing(Guid jobId) => ProcessingStatus == DocumentProcessingStatus.Processing && ActiveProcessingJobId == jobId;
    public void Complete(string preview, DateTimeOffset now) { ExtractedText = preview; ProcessingStatus = DocumentProcessingStatus.Ready; ActiveProcessingJobId = null; ProcessingError = null; FailureCode = null; ProcessingCompletedAt = now; FailedAt = null; UpdatedAt = now; }
    public void Requeue(string code, string message, DateTimeOffset now) { RetryCount++; ProcessingStatus = DocumentProcessingStatus.Queued; ActiveProcessingJobId = null; QueuedAt = now; ProcessingStartedAt = null; FailureCode = code; ProcessingError = message; UpdatedAt = now; }
    public void Fail(string code, string message, DateTimeOffset now) { RetryCount++; ProcessingStatus = DocumentProcessingStatus.Failed; ActiveProcessingJobId = null; FailureCode = code; ProcessingError = message; FailedAt = now; ProcessingCompletedAt = null; UpdatedAt = now; }
    public void Retry(DateTimeOffset now) { if (ProcessingStatus != DocumentProcessingStatus.Failed) throw new InvalidOperationException("Only failed documents can be retried."); Queue(now); }
}
