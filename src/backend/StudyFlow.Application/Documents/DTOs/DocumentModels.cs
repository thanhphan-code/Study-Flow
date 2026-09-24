using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.Documents.DTOs;

public sealed record DocumentUpload(byte[] Content, string OriginalFileName, string ContentType, Guid? SubjectId, Guid? StudySetId);
public sealed record DocumentListItemDto(Guid Id, Guid? SubjectId, Guid? StudySetId, string OriginalFileName, string MimeType, DocumentFileType FileType, long FileSize, DocumentProcessingStatus ProcessingStatus, int RetryCount, bool CanRetry, string ProcessingRevision, DateTimeOffset CreatedAt);
public sealed record DocumentDto(Guid Id, Guid? SubjectId, Guid? StudySetId, string OriginalFileName, string MimeType, DocumentFileType FileType, long FileSize, DocumentProcessingStatus ProcessingStatus, string? ExtractedText, string? FailureCode, string? ProcessingError, int RetryCount, bool CanRetry, DateTimeOffset? QueuedAt, DateTimeOffset? ProcessingStartedAt, DateTimeOffset? ProcessingCompletedAt, DateTimeOffset? FailedAt, string ProcessingRevision, int ChunkCount, DateTimeOffset CreatedAt);
