using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class DocumentChunk : BaseEntity
{
    private DocumentChunk() { }
    private DocumentChunk(Guid documentId, int chunkIndex, string content, int? pageNumber, int? slideNumber, string? sectionTitle, int startOffset, int endOffset, string contentHash)
    {
        DocumentId = documentId; ChunkIndex = chunkIndex; Content = content; PageNumber = pageNumber; SlideNumber = slideNumber;
        SectionTitle = sectionTitle; StartOffset = startOffset; EndOffset = endOffset; CharacterCount = content.Length;
        EstimatedTokenCount = Math.Max(1, (int)Math.Ceiling(content.Length / 4d)); ContentHash = contentHash;
    }
    public Guid DocumentId { get; private init; }
    public int ChunkIndex { get; private init; }
    public string Content { get; private init; } = string.Empty;
    public int? PageNumber { get; private init; }
    public int? SlideNumber { get; private init; }
    public string? SectionTitle { get; private init; }
    public int StartOffset { get; private init; }
    public int EndOffset { get; private init; }
    public int CharacterCount { get; private init; }
    public int EstimatedTokenCount { get; private init; }
    public string ContentHash { get; private init; } = string.Empty;
    public static DocumentChunk Create(Guid documentId, int index, string content, int? page, int? slide, string? section, int start, int end, string hash) => new(documentId, index, content, page, slide, section, start, end, hash);
}
