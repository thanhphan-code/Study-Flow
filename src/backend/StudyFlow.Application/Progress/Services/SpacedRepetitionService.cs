using StudyFlow.Application.Progress.DTOs;
using StudyFlow.Application.Progress.Interfaces;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.Progress.Services;

public sealed class SpacedRepetitionService : ISpacedRepetitionService, ISrsScheduler
{
    public const int MaximumIntervalDays = 3650;
    public string Version => "simple-srs/2";

    public ReviewSchedule Calculate(SrsSchedulingContext context)
    {
        var schedule = Calculate(context.Status, context.IntervalDays, context.EaseFactor, context.Rating, context.Now);
        if (context.Rating == ReviewRating.Again) return schedule;

        var activityFactor = context.AttemptType switch
        {
            LearningAttemptType.TrueFalse => .65,
            LearningAttemptType.MultipleChoice => .75,
            LearningAttemptType.Application => 1.1,
            _ => 1.0
        };
        // Zero represents an activity that does not ask for confidence (for example live battles).
        var confidenceFactor = context.Confidence switch { 0 => 1.0, <= 1 => .8, 2 => .9, 4 => 1.1, >= 5 => 1.2, _ => 1.0 };
        var responseFactor = context.ResponseTimeMs > 30_000 ? .8 : context.ResponseTimeMs > 15_000 ? .9 : 1.0;
        var hintFactor = context.HintUsed ? .75 : 1.0;
        var failureFactor = context.ConsecutiveWrong > 0 ? Math.Max(.6, 1 - context.ConsecutiveWrong * .1) : 1.0;
        var factor = activityFactor * confidenceFactor * responseFactor * hintFactor * failureFactor;
        var days = Math.Clamp((int)Math.Round(schedule.IntervalDays * factor, MidpointRounding.AwayFromZero), 1, MaximumIntervalDays);
        return schedule with { IntervalDays = days, NextReviewAt = context.Now.AddDays(days) };
    }

    public ReviewSchedule Calculate(FlashcardStatus currentStatus, int currentIntervalDays, double currentEaseFactor, ReviewRating rating, DateTimeOffset now)
    {
        var ease = Math.Clamp(currentEaseFactor, 1.3, 3.0);
        if (rating == ReviewRating.Again) return new(FlashcardStatus.Learning, 0, Math.Max(1.3, ease - 0.2), now.AddMinutes(10));

        var interval = rating switch
        {
            ReviewRating.Hard => currentStatus == FlashcardStatus.New || currentIntervalDays == 0 ? 1 : Math.Max(1, (int)Math.Round(currentIntervalDays * 1.2, MidpointRounding.AwayFromZero)),
            ReviewRating.Good => currentStatus == FlashcardStatus.New || currentIntervalDays == 0 ? 3 : Math.Max(3, (int)Math.Round(currentIntervalDays * 2.0, MidpointRounding.AwayFromZero)),
            ReviewRating.Easy => currentStatus == FlashcardStatus.New || currentIntervalDays == 0 ? 7 : Math.Max(7, (int)Math.Round(currentIntervalDays * 2.5, MidpointRounding.AwayFromZero)),
            _ => throw new ArgumentOutOfRangeException(nameof(rating))
        };
        ease = rating switch { ReviewRating.Hard => Math.Max(1.3, ease - 0.15), ReviewRating.Easy => Math.Min(3.0, ease + 0.15), _ => ease };
        interval = Math.Min(interval, MaximumIntervalDays);
        var status = interval >= 30 ? FlashcardStatus.Mastered : rating == ReviewRating.Hard ? FlashcardStatus.Learning : FlashcardStatus.Reviewing;
        return new(status, interval, ease, now.AddDays(interval));
    }
}
