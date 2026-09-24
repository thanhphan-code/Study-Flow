using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Domain.Enums;
using UglyToad.PdfPig;

namespace StudyFlow.Infrastructure.Documents;

internal abstract class DocumentTextExtractorBase(DocumentFileType type) : IDocumentTextExtractor
{
    protected const int MaximumExtractedCharacters = 2_000_000;
    public bool CanHandle(DocumentFileType candidate) => candidate == type;
    public abstract Task<DocumentExtractionResult> ExtractAsync(Stream stream, CancellationToken cancellationToken);
    protected static string Normalize(string value)
    {
        value = value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\u200B", string.Empty).Replace("\uFEFF", string.Empty);
        var lines = value.Split('\n').Select(line => Regex.Replace(line.TrimEnd(), "[\\t ]+", " "));
        return Regex.Replace(string.Join("\n", lines), "\n{3,}", "\n\n").Trim();
    }
    protected static void EnsureValid(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new DocumentProcessingException("DOCUMENT_EMPTY", "The document does not contain readable text.", false);
        if (text.Length > MaximumExtractedCharacters) throw new DocumentProcessingException("DOCUMENT_CONTENT_TOO_LARGE", "The extracted document content is too large.", false);
    }
}

internal sealed class PdfTextExtractor() : DocumentTextExtractorBase(DocumentFileType.Pdf)
{
    public override Task<DocumentExtractionResult> ExtractAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var memory = new MemoryStream(); stream.CopyTo(memory); using var pdf = PdfDocument.Open(memory.ToArray());
            if (pdf.NumberOfPages > 1000) throw new DocumentProcessingException("DOCUMENT_CONTENT_TOO_LARGE", "The PDF contains too many pages.", false);
            var sections = new List<DocumentExtractionSection>(); var total = 0;
            foreach (var page in pdf.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested(); var text = Normalize(page.Text); if (string.IsNullOrWhiteSpace(text)) continue;
                total += text.Length; if (total > MaximumExtractedCharacters) throw new DocumentProcessingException("DOCUMENT_CONTENT_TOO_LARGE", "The extracted document content is too large.", false);
                sections.Add(new(text, PageNumber: page.Number));
            }
            EnsureValid(string.Join('\n', sections.Select(x => x.Content))); return Task.FromResult(new DocumentExtractionResult(sections));
        }
        catch (DocumentProcessingException) { throw; }
        catch (Exception exception) when (exception is not OperationCanceledException) { throw new DocumentProcessingException("DOCUMENT_CORRUPTED", "The PDF is corrupted, encrypted, or unsupported.", false, exception); }
    }
}

internal abstract class OfficeTextExtractor(DocumentFileType type, string entryPrefix, int maximumParts) : DocumentTextExtractorBase(type)
{
    public override async Task<DocumentExtractionResult> ExtractAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true); ValidateArchive(archive);
            var entries = archive.Entries.Where(x => x.FullName.StartsWith(entryPrefix, StringComparison.OrdinalIgnoreCase) && x.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)).OrderBy(x => PartNumber(x.FullName)).ToList();
            if (entries.Count == 0) throw new DocumentProcessingException("DOCUMENT_CORRUPTED", "The Office document has no readable content parts.", false);
            if (entries.Count > maximumParts) throw new DocumentProcessingException("DOCUMENT_CONTENT_TOO_LARGE", "The Office document contains too many pages or slides.", false);
            var sections = new List<DocumentExtractionSection>(); var total = 0;
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested(); await using var entryStream = entry.Open();
                using var reader = XmlReader.Create(entryStream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit, MaxCharactersInDocument = 100_000_000 });
                var xml = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
                var paragraphs = xml.Descendants().Where(x => x.Name.LocalName == "p").Select(p => string.Concat(p.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value))).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var text = Normalize(string.Join("\n", paragraphs)); if (string.IsNullOrWhiteSpace(text)) continue; total += text.Length;
                if (total > MaximumExtractedCharacters) throw new DocumentProcessingException("DOCUMENT_CONTENT_TOO_LARGE", "The extracted document content is too large.", false);
                sections.Add(CreateSection(text, PartNumber(entry.FullName)));
            }
            EnsureValid(string.Join('\n', sections.Select(x => x.Content))); return new(sections);
        }
        catch (DocumentProcessingException) { throw; }
        catch (InvalidDataException exception) { throw new DocumentProcessingException("DOCUMENT_CORRUPTED", "The Office document is corrupted or malformed.", false, exception); }
        catch (XmlException exception) { throw new DocumentProcessingException("DOCUMENT_CORRUPTED", "The Office document contains malformed XML.", false, exception); }
    }
    protected abstract DocumentExtractionSection CreateSection(string text, int partNumber);
    private static int PartNumber(string value) { var digits = new string(Path.GetFileNameWithoutExtension(value).Where(char.IsDigit).ToArray()); return int.TryParse(digits, out var number) ? number : 1; }
    private static void ValidateArchive(ZipArchive archive)
    {
        const long maxExpanded = 100 * 1024 * 1024; const int maxEntries = 10_000; const double maxRatio = 200;
        if (archive.Entries.Count > maxEntries || archive.Entries.Sum(x => x.Length) > maxExpanded) throw new DocumentProcessingException("DOCUMENT_CONTENT_TOO_LARGE", "The Office package expands beyond the allowed limit.", false);
        if (archive.Entries.Any(x => x.Length > 1_000_000 && x.CompressedLength > 0 && (double)x.Length / x.CompressedLength > maxRatio)) throw new DocumentProcessingException("DOCUMENT_CONTENT_TOO_LARGE", "The Office package has an unsafe compression ratio.", false);
    }
}

internal sealed class DocxTextExtractor() : OfficeTextExtractor(DocumentFileType.Docx, "word/document.xml", 1)
{
    protected override DocumentExtractionSection CreateSection(string text, int partNumber) => new(text);
}

internal sealed class PptxTextExtractor() : OfficeTextExtractor(DocumentFileType.Pptx, "ppt/slides/slide", 1000)
{
    protected override DocumentExtractionSection CreateSection(string text, int partNumber) => new(text, SlideNumber: partNumber);
}

internal sealed class TxtTextExtractor() : DocumentTextExtractorBase(DocumentFileType.Txt)
{
    public override async Task<DocumentExtractionResult> ExtractAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true, 64 * 1024, true); var text = Normalize(await reader.ReadToEndAsync(cancellationToken)); EnsureValid(text);
            return new([new(text)]);
        }
        catch (DecoderFallbackException exception) { throw new DocumentProcessingException("DOCUMENT_CORRUPTED", "The TXT document is not valid UTF-8.", false, exception); }
    }
}
