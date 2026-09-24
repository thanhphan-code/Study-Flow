using StudyFlow.Domain.Enums;
using StudyFlow.Application.LearningEngine.DTOs;

namespace StudyFlow.Application.Progress.DTOs;

public sealed record ReviewFlashcardRequest(
    ReviewRating Rating,
    Guid? StudySessionId = null,
    StudyMode? Mode = null,
    string? SubmittedAnswer = null,
    LearningAttemptType? AttemptType = null,
    RecallDirection? Direction = null,
    int? Confidence = null,
    int? ResponseTimeMs = null,
    bool HintUsed = false,
    Guid? ClientAttemptId = null);
public sealed record FlashcardProgressDto(Guid Id, Guid FlashcardId, FlashcardStatus Status, int CorrectCount, int WrongCount, DateTimeOffset LastReviewedAt, DateTimeOffset NextReviewAt, int IntervalDays, double EaseFactor, decimal MasteryScore, decimal WeaknessScore, RecommendedNextAction RecommendedNextAction, RecommendedLearningStep RecommendedLearningStep, int StateRevision, string SchedulerVersion, DateTimeOffset? LastScheduledAt);
public sealed record DueFlashcardDto(Guid FlashcardId, Guid StudySetId, string FrontText, string BackText, string? Explanation, string? LanguageCode, string? ReadingText, string? Romanization, string? ExampleText, string? ExampleTranslation, string? MemoryTip, DateTimeOffset NextReviewAt, FlashcardStatus Status);
public sealed record ReviewSchedule(FlashcardStatus Status, int IntervalDays, double EaseFactor, DateTimeOffset NextReviewAt);
public sealed record SrsSchedulingContext(FlashcardStatus Status, int IntervalDays, double EaseFactor, ReviewRating Rating, LearningAttemptType AttemptType, LearningAttemptResult Result, int Confidence, int ResponseTimeMs, bool HintUsed, int ConsecutiveWrong, DateTimeOffset Now);
