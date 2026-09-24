using StudyFlow.Application.Common.Models;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.Grounding;

public sealed record DocumentContextChunk(string Alias, Guid DocumentId, string DocumentName, Guid DocumentChunkId, int ChunkIndex, string Content, int? PageNumber, int? SlideNumber, string? SectionTitle, int StartOffset, int EndOffset, string ContentHash);
public sealed record DocumentContextResult(IReadOnlyList<DocumentContextChunk> Chunks, string RetrievalRevision);
public sealed record ValidatedSource(Guid DocumentId, string DocumentName, Guid DocumentChunkId, int? PageNumber, int? SlideNumber, string? SectionTitle, int StartOffset, int EndOffset, string ContentHash, string Snippet);
public sealed record GroundingValidationResult(IReadOnlyList<ValidatedSource> Valid, IReadOnlyList<string> Rejected);
public sealed record SourceReferenceDto(Guid Id, Guid? DocumentId, string DocumentName, Guid? DocumentChunkId, int? PageNumber, int? SlideNumber, string? SectionTitle, string Snippet, int StartOffset, int EndOffset, bool DocumentAvailable);
public interface IDocumentContextRetriever { Task<Result<DocumentContextResult>> RetrieveAsync(Guid userId, IReadOnlyCollection<Guid> documentIds, string query, int? maxChunks, CancellationToken cancellationToken); }
public interface IGroundingValidator { Task<GroundingValidationResult> ValidateAsync(Guid userId, IReadOnlyCollection<string> sourceAliases, DocumentContextResult suppliedContext, CancellationToken cancellationToken); }
public interface ISourceReferenceService
{
    Task<Result<IReadOnlyList<SourceReferenceDto>>> GetForFlashcardAsync(Guid userId, Guid flashcardId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<SourceReferenceDto>>> GetForQuestionAsync(Guid userId, Guid questionId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<SourceReferenceDto>>> GetForJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken);
}
