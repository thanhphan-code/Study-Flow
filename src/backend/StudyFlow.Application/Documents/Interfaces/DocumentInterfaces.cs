using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Documents.DTOs;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.Documents.Interfaces;

public interface IFileStorageService
{
    Task SaveAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);
    Task SaveAsync(string key, Stream stream, CancellationToken cancellationToken);
    Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
}
public sealed record DocumentExtractionSection(string Content, int? PageNumber = null, int? SlideNumber = null, string? SectionTitle = null);
public sealed record DocumentExtractionResult(IReadOnlyList<DocumentExtractionSection> Sections);
public sealed record DocumentChunkDraft(int ChunkIndex, string Content, int? PageNumber, int? SlideNumber, string? SectionTitle, int StartOffset, int EndOffset, string ContentHash);
public interface IDocumentTextExtractor
{
    bool CanHandle(DocumentFileType type);
    Task<DocumentExtractionResult> ExtractAsync(Stream stream, CancellationToken cancellationToken);
}
public interface IDocumentChunker { IReadOnlyList<DocumentChunkDraft> Chunk(DocumentExtractionResult extraction); }
public interface IDocumentProcessingQueue { Task EnqueueAsync(Guid documentId, CancellationToken cancellationToken); }
public interface IDocumentProcessor { Task ProcessAsync(Guid documentId, CancellationToken cancellationToken); Task RecoverStaleAsync(CancellationToken cancellationToken); }
public interface IDocumentContextService { Task<Result<string>> GetContextAsync(Guid userId, Guid documentId, int maximumCharacters, CancellationToken cancellationToken); }
public interface IDocumentService
{
    Task<Result<DocumentDto>> UploadAsync(Guid userId, DocumentUpload upload, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentListItemDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<DocumentDto>> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<Result<DocumentDto>> RetryAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
