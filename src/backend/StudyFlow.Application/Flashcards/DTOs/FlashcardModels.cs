namespace StudyFlow.Application.Flashcards.DTOs;

public sealed record CreateFlashcardRequest(string FrontText, string BackText, string? Explanation, string? LanguageCode = null, string? ReadingText = null, string? Romanization = null, string? ExampleText = null, string? ExampleTranslation = null, string? MemoryTip = null, string? AcceptedAnswers = null, bool EnableReverseRecall = false);
public sealed record UpdateFlashcardRequest(string FrontText, string BackText, string? Explanation, string? LanguageCode = null, string? ReadingText = null, string? Romanization = null, string? ExampleText = null, string? ExampleTranslation = null, string? MemoryTip = null, string? AcceptedAnswers = null, bool EnableReverseRecall = false);
public sealed record BulkCreateFlashcardsRequest(IReadOnlyList<CreateFlashcardRequest> Cards);
public sealed record FlashcardDto(Guid Id, Guid StudySetId, string FrontText, string BackText, string? Explanation, string? LanguageCode, string? ReadingText, string? Romanization, string? ExampleText, string? ExampleTranslation, string? MemoryTip, string? AcceptedAnswers, bool EnableReverseRecall, string? ImageUrl, int OrderIndex, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record FlashcardImageUpload(byte[] Content, string OriginalFileName, string ContentType);
public sealed record FlashcardImageContent(byte[] Content, string ContentType, string FileName);
