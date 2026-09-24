using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class Flashcard : BaseEntity
{
    private Flashcard() { }
    private Flashcard(Guid studySetId, string frontText, string backText, string? explanation, int orderIndex, string? languageCode, string? readingText, string? romanization, string? exampleText, string? exampleTranslation, string? memoryTip, string? acceptedAnswers, bool enableReverseRecall)
    {
        StudySetId = studySetId; FrontText = frontText.Trim(); BackText = backText.Trim(); Explanation = Normalize(explanation); OrderIndex = orderIndex; LanguageCode = Normalize(languageCode); ReadingText = Normalize(readingText); Romanization = Normalize(romanization); ExampleText = Normalize(exampleText); ExampleTranslation = Normalize(exampleTranslation); MemoryTip = Normalize(memoryTip); AcceptedAnswers = Normalize(acceptedAnswers); EnableReverseRecall = enableReverseRecall;
    }
    public Guid StudySetId { get; private init; }
    public string FrontText { get; private set; } = string.Empty;
    public string BackText { get; private set; } = string.Empty;
    public string? Explanation { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? ReadingText { get; private set; }
    public string? Romanization { get; private set; }
    public string? ExampleText { get; private set; }
    public string? ExampleTranslation { get; private set; }
    public string? MemoryTip { get; private set; }
    public string? AcceptedAnswers { get; private set; }
    public bool EnableReverseRecall { get; private set; }
    public string? ImageStorageKey { get; private set; }
    public string? ImageContentType { get; private set; }
    public string? ImageFileName { get; private set; }
    public int OrderIndex { get; private set; }
    public static Flashcard Create(Guid studySetId, string frontText, string backText, string? explanation, int orderIndex, string? languageCode = null, string? readingText = null, string? romanization = null, string? exampleText = null, string? exampleTranslation = null, string? memoryTip = null, string? acceptedAnswers = null, bool enableReverseRecall = false) => new(studySetId, frontText, backText, explanation, orderIndex, languageCode, readingText, romanization, exampleText, exampleTranslation, memoryTip, acceptedAnswers, enableReverseRecall);
    public void Update(string frontText, string backText, string? explanation, string? languageCode = null, string? readingText = null, string? romanization = null, string? exampleText = null, string? exampleTranslation = null, string? memoryTip = null, string? acceptedAnswers = null, bool enableReverseRecall = false)
    {
        FrontText = frontText.Trim(); BackText = backText.Trim(); Explanation = Normalize(explanation); LanguageCode = Normalize(languageCode); ReadingText = Normalize(readingText); Romanization = Normalize(romanization); ExampleText = Normalize(exampleText); ExampleTranslation = Normalize(exampleTranslation); MemoryTip = Normalize(memoryTip); AcceptedAnswers = Normalize(acceptedAnswers); EnableReverseRecall = enableReverseRecall; UpdatedAt = DateTimeOffset.UtcNow;
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public void SetImage(string storageKey, string contentType, string fileName) { ImageStorageKey = storageKey; ImageContentType = contentType; ImageFileName = fileName; UpdatedAt = DateTimeOffset.UtcNow; }
    public void RemoveImage() { ImageStorageKey = null; ImageContentType = null; ImageFileName = null; UpdatedAt = DateTimeOffset.UtcNow; }
}
