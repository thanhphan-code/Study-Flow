using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class SourceReference : BaseEntity
{
    private SourceReference() { }
    private SourceReference(Guid? documentId, Guid? chunkId, GroundedContentType contentType, Guid contentId, int? page, int? slide, string? section, int start, int end, string hash, string retrievalRevision, string documentName, string snippet)
    { DocumentId=documentId; DocumentChunkId=chunkId; ContentType=contentType; ContentId=contentId; PageNumber=page; SlideNumber=slide; SectionTitle=section; StartOffset=start; EndOffset=end; ChunkContentHash=hash; RetrievalRevision=retrievalRevision; DocumentNameSnapshot=documentName; SourceSnippetSnapshot=snippet; }
    public Guid? DocumentId { get; private set; }
    public Guid? DocumentChunkId { get; private set; }
    public GroundedContentType ContentType { get; private init; }
    public Guid ContentId { get; private init; }
    public int? PageNumber { get; private init; }
    public int? SlideNumber { get; private init; }
    public string? SectionTitle { get; private init; }
    public int StartOffset { get; private init; }
    public int EndOffset { get; private init; }
    public string ChunkContentHash { get; private init; } = string.Empty;
    public string RetrievalRevision { get; private init; } = string.Empty;
    public string GroundingRevision { get; private init; } = "grounding-v1";
    public string DocumentNameSnapshot { get; private init; } = string.Empty;
    public string SourceSnippetSnapshot { get; private init; } = string.Empty;
    public static SourceReference Create(Guid documentId, Guid chunkId, GroundedContentType type, Guid contentId, int? page, int? slide, string? section, int start, int end, string hash, string retrievalRevision, string documentName, string snippet) => new(documentId, chunkId, type, contentId, page, slide, section, start, end, hash, retrievalRevision, documentName, snippet);
}
