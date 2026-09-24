using StudyFlow.Application.LearningEngine.DTOs;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.LearningEngine.Services;

public sealed class LearningStatePolicy : ILearningStatePolicy
{
    public string Version => "memory-state/2";

    public LearningStateAssessment Assess(FlashcardProgress? progress, DateTimeOffset now)
    {
        if (progress is null)
            return new(true, false, false, 0m, 0m, 35, "New", RecommendedNextAction.LearnNew,
                new(LearningAttemptType.MultipleChoice, RecallDirection.Forward, null, "IntroduceNewCard", 35));

        var total = progress.CorrectCount + progress.WrongCount;
        var wrongRatio = total == 0 ? 0m : (decimal)progress.WrongCount / total;
        var isDue = progress.NextReviewAt <= now;
        var overdueDays = isDue && progress.NextReviewAt.HasValue
            ? Math.Max(0, (int)(now - progress.NextReviewAt.Value).TotalDays)
            : 0;

        var recallEvidence = Math.Min(35m, progress.SuccessfulRecallCount * 5m);
        var recognitionEvidence = Math.Min(12m, progress.SuccessfulRecognitionCount * 2m);
        var intervalStrength = Math.Min(25m, progress.IntervalDays / 1.2m);
        var streakStrength = Math.Min(15m, progress.ConsecutiveCorrect * 3m);
        var hintPenalty = Math.Min(15m, progress.HintedCorrectCount * 2m);
        var mastery = Math.Clamp(recallEvidence + recognitionEvidence + intervalStrength + streakStrength - hintPenalty - progress.RecentWrongCount * 6m, 0m, 100m);
        var averageResponse = progress.TimedAttemptCount == 0 ? 0 : progress.TotalResponseTimeMs / progress.TimedAttemptCount;
        var weakness = Math.Clamp(
            progress.RecentWrongCount * 12m
            + progress.ConsecutiveWrong * 10m
            + wrongRatio * 20m
            + Math.Min(12m, progress.HintedCorrectCount * 2m)
            + (averageResponse > 30_000 ? 10m : averageResponse > 15_000 ? 5m : 0m)
            + (decimal)Math.Max(0d, 2.5d - progress.EaseFactor) * 12m, 0m, 100m);
        var isWeak = progress.WrongCount > 0 && weakness >= 35m;
        var priority = (isDue ? 100 : 0) + Math.Min(40, overdueDays * 5) + (isWeak ? 60 : 0) + (int)Math.Round(weakness / 4m);
        var reason = isDue ? "Due" : isWeak ? "Weak" : "Strengthen";
        var nextAction = isDue ? RecommendedNextAction.ReviewNow : isWeak ? RecommendedNextAction.Strengthen : RecommendedNextAction.KeepFresh;
        var nextStep = isWeak
            ? new RecommendedLearningStep(LearningAttemptType.Recall, RecallDirection.Forward, null, "RecentFailuresNeedReinforcement", priority)
            : isDue
                ? new RecommendedLearningStep(LearningAttemptType.Recall, RecallDirection.Forward, progress.NextReviewAt, "ScheduledReviewDue", priority)
                : mastery >= 80m
                    ? new RecommendedLearningStep(LearningAttemptType.Application, RecallDirection.Forward, progress.NextReviewAt, "ApplyMasteredKnowledge", priority)
                    : new RecommendedLearningStep(LearningAttemptType.Recall, RecallDirection.Forward, progress.NextReviewAt, "StrengthenRecall", priority);

        return new(false, isDue, isWeak, Math.Round(mastery, 2), Math.Round(weakness, 2), priority, reason, nextAction, nextStep);
    }
}
