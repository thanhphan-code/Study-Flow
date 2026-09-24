namespace StudyFlow.Application.DailyStudy.DTOs;

public sealed record DailyStudyRequest(int Minutes = 15, string Focus = "Balanced");
public sealed record DailyStudyItemDto(
    Guid FlashcardId,
    Guid StudySetId,
    string StudySetTitle,
    string FrontText,
    string BackText,
    string? Explanation,
    string? LanguageCode,
    string? ReadingText,
    string? Romanization,
    string? ExampleText,
    string? ExampleTranslation,
    string? MemoryTip,
    string? AcceptedAnswers,
    string? ImageUrl,
    string Activity,
    string Reason);
public sealed record DailyStudyPlanDto(int Minutes, string Focus, int DueCount, int WeakCount, int NewCount, IReadOnlyList<DailyStudyItemDto> Items);
