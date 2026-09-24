using System.Security.Cryptography;
using System.Text;
using StudyFlow.Application.Documents.Interfaces;

namespace StudyFlow.Infrastructure.Documents;

public sealed class DocumentChunker : IDocumentChunker
{
    private const int TargetCharacters = 2500;
    private const int OverlapCharacters = 300;
    private const int MaximumChunks = 2000;

    public IReadOnlyList<DocumentChunkDraft> Chunk(DocumentExtractionResult extraction)
    {
        var result = new List<DocumentChunkDraft>(); var globalOffset = 0;
        foreach (var section in extraction.Sections)
        {
            var content = section.Content.Trim(); var local = 0;
            while (local < content.Length)
            {
                var desiredEnd = Math.Min(content.Length, local + TargetCharacters); var end = FindBoundary(content, local, desiredEnd);
                var value = content[local..end].Trim();
                if (value.Length > 0)
                {
                    var startOffset = globalOffset + local; var endOffset = globalOffset + end;
                    result.Add(new(result.Count, value, section.PageNumber, section.SlideNumber, section.SectionTitle, startOffset, endOffset, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()));
                    if (result.Count > MaximumChunks) throw new DocumentProcessingException("DOCUMENT_CONTENT_TOO_LARGE", "The document creates too many chunks.", false);
                }
                if (end >= content.Length) break; local = Math.Max(local + 1, end - OverlapCharacters);
            }
            globalOffset += content.Length + 2;
        }
        if (result.Count == 0) throw new DocumentProcessingException("DOCUMENT_EMPTY", "The document does not contain readable text.", false);
        return result;
    }

    private static int FindBoundary(string text, int start, int desiredEnd)
    {
        if (desiredEnd >= text.Length) return text.Length;
        var minimum = start + TargetCharacters / 2;
        for (var index = desiredEnd; index >= minimum; index--) if (text[index - 1] == '\n' && index < text.Length && text[index] == '\n') return index;
        for (var index = desiredEnd; index >= minimum; index--) if (text[index - 1] is '.' or '!' or '?' or '。' or '！' or '？') return index;
        for (var index = desiredEnd; index >= minimum; index--) if (char.IsWhiteSpace(text[index - 1])) return index;
        return desiredEnd;
    }
}
