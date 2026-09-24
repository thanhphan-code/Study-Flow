using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class FlashcardProgress : BaseEntity
{
    private FlashcardProgress() { }
    private FlashcardProgress(Guid userId, Guid flashcardId) { UserId = userId; FlashcardId = flashcardId; }
    public Guid UserId { get; private init; }
    public Guid FlashcardId { get; private init; }
    public FlashcardStatus Status { get; private set; } = FlashcardStatus.New;
    public int CorrectCount { get; private set; }
    public int WrongCount { get; private set; }
    public DateTimeOffset? LastReviewedAt { get; private set; }
    public DateTimeOffset? NextReviewAt { get; private set; }
    public int IntervalDays { get; private set; }
    public double EaseFactor { get; private set; } = 2.5;
    public decimal MasteryScore { get; private set; }
    public decimal WeaknessScore { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public int StateRevision { get; private set; }
    public string SchedulerVersion { get; private set; } = "legacy";
    public DateTimeOffset? LastScheduledAt { get; private set; }
    public int ConsecutiveCorrect { get; private set; }
    public int ConsecutiveWrong { get; private set; }
    public int RecentWrongCount { get; private set; }
    public int SuccessfulRecallCount { get; private set; }
    public int SuccessfulRecognitionCount { get; private set; }
    public int HintedCorrectCount { get; private set; }
    public int UnhintedCorrectCount { get; private set; }
    public long TotalResponseTimeMs { get; private set; }
    public int TimedAttemptCount { get; private set; }
    public DateTimeOffset? LastCorrectAt { get; private set; }
    public DateTimeOffset? LastWrongAt { get; private set; }
    public static FlashcardProgress Create(Guid userId, Guid flashcardId) => new(userId, flashcardId);
    public void ApplyEvidence(LearningAttempt attempt, ReviewRating rating, DateTimeOffset now)
    {
        if (rating == ReviewRating.Again) WrongCount++; else CorrectCount++;
        if (attempt.Result == LearningAttemptResult.Wrong)
        {
            ConsecutiveWrong++; ConsecutiveCorrect = 0; RecentWrongCount = Math.Min(10, RecentWrongCount + 1); LastWrongAt = now;
        }
        else
        {
            ConsecutiveCorrect++; ConsecutiveWrong = 0; RecentWrongCount = Math.Max(0, RecentWrongCount - 1); LastCorrectAt = now;
            if (attempt.AttemptType == LearningAttemptType.Recall) SuccessfulRecallCount++;
            if (attempt.AttemptType is LearningAttemptType.MultipleChoice or LearningAttemptType.TrueFalse) SuccessfulRecognitionCount++;
            if (attempt.HintUsed) HintedCorrectCount++; else UnhintedCorrectCount++;
        }
        if (attempt.ResponseTimeMs > 0) { TotalResponseTimeMs += attempt.ResponseTimeMs; TimedAttemptCount++; }
        LastAttemptAt = now; UpdatedAt = now;
    }
    public void ApplySchedule(ReviewRating rating, FlashcardStatus status, int intervalDays, double easeFactor, DateTimeOffset reviewedAt, DateTimeOffset nextReviewAt, string schedulerVersion)
    {
        Status = status; IntervalDays = intervalDays; EaseFactor = easeFactor; LastReviewedAt = reviewedAt; NextReviewAt = nextReviewAt; SchedulerVersion = schedulerVersion; LastScheduledAt = reviewedAt; UpdatedAt = reviewedAt;
    }
    public void UpdateLearningState(decimal masteryScore, decimal weaknessScore, DateTimeOffset attemptedAt)
    {
        MasteryScore = Math.Clamp(masteryScore, 0m, 100m);
        WeaknessScore = Math.Clamp(weaknessScore, 0m, 100m);
        LastAttemptAt = attemptedAt;
        StateRevision++;
        UpdatedAt = attemptedAt;
    }
}
