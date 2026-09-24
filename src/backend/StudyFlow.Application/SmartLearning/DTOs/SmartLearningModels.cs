using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.SmartLearning.DTOs;

public sealed record StartSmartLearnRequest(int ItemCount = 18);
public sealed record SmartLearnOptionDto(string Text);
public sealed record SmartLearnItemDto(Guid FlashcardId, string FrontText, string BackText, string? Explanation, string? LanguageCode, string? ReadingText, string? Romanization, string? ExampleText, string? ExampleTranslation, string? MemoryTip, LearningAttemptType AttemptType, RecallDirection Direction, bool SupportsReverse, IReadOnlyList<SmartLearnOptionDto> Options, int Priority, string Reason);
public sealed record SmartLearnSessionDto(Guid StudySessionId, Guid StudySetId, string StudySetTitle, DateTimeOffset StartedAt, IReadOnlyList<SmartLearnItemDto> Items);
public sealed record RecordLearningAttemptRequest(Guid ClientAttemptId, Guid FlashcardId, LearningAttemptType AttemptType, RecallDirection Direction, string SubmittedAnswer, int Confidence, int ResponseTimeMs, bool HintUsed);
public sealed record LearningAttemptDto(Guid Id, Guid ClientAttemptId, Guid FlashcardId, LearningAttemptType AttemptType, RecallDirection Direction, LearningAttemptResult Result, int Confidence, int ResponseTimeMs, bool HintUsed, DateTimeOffset CreatedAt, LearningCardOutcome CardOutcome, LearningAttemptType? NextAttemptType, RecallDirection? NextDirection, int RetryAfterItems, bool SrsCommitted, DateTimeOffset? NextReviewAt);
public sealed record SmartLearnSummaryDto(Guid StudySessionId, int DurationSeconds, int TotalAttempts, int Correct, int NeedsWork, int ImprovedCards, int MasteredToday, DateTimeOffset? RecommendedNextReview);
