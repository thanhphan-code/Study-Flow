using StudyFlow.Application.Progress.Services;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Tests.Progress;

public sealed class SpacedRepetitionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 8, 0, 0, TimeSpan.Zero);
    private readonly SpacedRepetitionService _service = new();

    [Fact]
    public void ReviewAgain_SchedulesCardSooner()
    {
        var again = _service.Calculate(FlashcardStatus.Reviewing, 3, 2.5, ReviewRating.Again, Now);
        var hard = _service.Calculate(FlashcardStatus.Reviewing, 3, 2.5, ReviewRating.Hard, Now);
        Assert.Equal(Now.AddMinutes(10), again.NextReviewAt);
        Assert.True(again.NextReviewAt < hard.NextReviewAt);
        Assert.Equal(FlashcardStatus.Learning, again.Status);
    }

    [Fact]
    public void ReviewEasy_SchedulesCardLaterThanGood()
    {
        var good = _service.Calculate(FlashcardStatus.New, 0, 2.5, ReviewRating.Good, Now);
        var easy = _service.Calculate(FlashcardStatus.New, 0, 2.5, ReviewRating.Easy, Now);
        Assert.Equal(3, good.IntervalDays);
        Assert.Equal(7, easy.IntervalDays);
        Assert.True(easy.NextReviewAt > good.NextReviewAt);
    }

    [Fact]
    public void RecognitionOrHint_ProducesShorterIntervalThanUnhintedRecall()
    {
        var recall = _service.Calculate(new(FlashcardStatus.Reviewing, 10, 2.5, ReviewRating.Good, LearningAttemptType.Recall, LearningAttemptResult.Correct, 3, 3_000, false, 0, Now));
        var recognition = _service.Calculate(new(FlashcardStatus.Reviewing, 10, 2.5, ReviewRating.Good, LearningAttemptType.MultipleChoice, LearningAttemptResult.Correct, 3, 3_000, false, 0, Now));
        var hinted = _service.Calculate(new(FlashcardStatus.Reviewing, 10, 2.5, ReviewRating.Good, LearningAttemptType.Recall, LearningAttemptResult.Correct, 3, 3_000, true, 0, Now));
        Assert.True(recognition.IntervalDays < recall.IntervalDays); Assert.True(hinted.IntervalDays < recall.IntervalDays);
        Assert.Equal("simple-srs/2", _service.Version);
    }

    [Fact]
    public void VeryLargeExistingInterval_IsCappedWithoutDateOverflow()
    {
        var schedule = _service.Calculate(FlashcardStatus.Mastered, SpacedRepetitionService.MaximumIntervalDays, 3.0, ReviewRating.Easy, Now);
        Assert.Equal(SpacedRepetitionService.MaximumIntervalDays, schedule.IntervalDays);
        Assert.Equal(Now.AddDays(SpacedRepetitionService.MaximumIntervalDays), schedule.NextReviewAt);
    }

    [Fact]
    public void UnaskedConfidence_IsNeutralInsteadOfLowConfidence()
    {
        var unknown = _service.Calculate(new(FlashcardStatus.Reviewing, 10, 2.5, ReviewRating.Good, LearningAttemptType.Recall, LearningAttemptResult.Correct, 0, 3_000, false, 0, Now));
        var neutral = _service.Calculate(new(FlashcardStatus.Reviewing, 10, 2.5, ReviewRating.Good, LearningAttemptType.Recall, LearningAttemptResult.Correct, 3, 3_000, false, 0, Now));
        Assert.Equal(neutral.IntervalDays, unknown.IntervalDays);
    }
}
