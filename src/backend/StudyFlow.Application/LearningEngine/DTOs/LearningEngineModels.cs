using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.LearningEngine.DTOs;

public sealed record RecordLearningEvidence(
    Guid UserId,
    Guid FlashcardId,
    Guid? StudySessionId,
    StudyMode Mode,
    LearningAttemptType AttemptType,
    RecallDirection Direction,
    string SubmittedAnswer,
    LearningAttemptResult? Result,
    int Confidence,
    int ResponseTimeMs,
    bool HintUsed,
    Guid ClientAttemptId,
    ReviewRating? CommitRating = null,
    LearningCardOutcome? CommitOutcome = null,
    bool CountTowardsSession = false);

public sealed record LearningEngineResult(LearningAttempt Attempt, FlashcardProgress? Progress);

public sealed record RecommendedLearningStep(
    LearningAttemptType Activity,
    RecallDirection Direction,
    DateTimeOffset? DueAt,
    string Reason,
    int Priority);

public sealed record LearningStateAssessment(
    bool IsNew,
    bool IsDue,
    bool IsWeak,
    decimal MasteryScore,
    decimal WeaknessScore,
    int Priority,
    string Reason,
    RecommendedNextAction RecommendedNextAction,
    RecommendedLearningStep NextStep);
