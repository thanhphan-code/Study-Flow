using System.Text;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Domain.Enums;
using StudyFlow.Infrastructure.Persistence;

namespace StudyFlow.Infrastructure.Documents;

internal sealed class DocumentContextService(StudyFlowDbContext dbContext) : IDocumentContextService
{
    public async Task<Result<string>> GetContextAsync(Guid userId, Guid documentId, int maximumCharacters, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == documentId && x.UserId == userId, cancellationToken);
        if (document is null) return Result<string>.Failure("DOCUMENT_NOT_FOUND", "Document was not found.", ErrorType.NotFound);
        if (document.ProcessingStatus != DocumentProcessingStatus.Ready) return Result<string>.Failure("DOCUMENT_NOT_READY", "The document is still being processed or has failed.", ErrorType.Conflict);
        var chunks = await dbContext.DocumentChunks.AsNoTracking().Where(x => x.DocumentId == documentId).OrderBy(x => x.ChunkIndex).ToListAsync(cancellationToken);
        if (chunks.Count == 0) return Result<string>.Failure("DOCUMENT_NOT_READY", "The document has no processed content.", ErrorType.Conflict);
        var builder = new StringBuilder(Math.Min(maximumCharacters, 64_000));
        foreach (var chunk in chunks)
        {
            var source = chunk.PageNumber.HasValue ? $"[Page {chunk.PageNumber}]" : chunk.SlideNumber.HasValue ? $"[Slide {chunk.SlideNumber}]" : $"[Chunk {chunk.ChunkIndex + 1}]";
            var value = $"{source}\n{chunk.Content}\n\n"; if (builder.Length + value.Length > maximumCharacters) break; builder.Append(value);
        }
        return builder.Length == 0 ? Result<string>.Failure("DOCUMENT_CONTENT_TOO_LARGE", "No document chunk fits the AI context limit.", ErrorType.Validation) : Result<string>.Success(builder.ToString().Trim());
    }
}
