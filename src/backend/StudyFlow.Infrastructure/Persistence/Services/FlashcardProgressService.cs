using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Progress.DTOs;
using StudyFlow.Application.Progress.Interfaces;
using StudyFlow.Application.LearningEngine.DTOs;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class FlashcardProgressService(StudyFlowDbContext dbContext, ILearningEngine learningEngine, ILearningStatePolicy statePolicy, TimeProvider timeProvider) : IFlashcardProgressService
{
    public async Task<Result<FlashcardProgressDto>> ReviewAsync(Guid userId, Guid flashcardId, ReviewFlashcardRequest request, CancellationToken cancellationToken)
    {
        var result = request.SubmittedAnswer is not null ? (LearningAttemptResult?)null : request.Rating switch
        {
            ReviewRating.Again => LearningAttemptResult.Wrong,
            ReviewRating.Hard => LearningAttemptResult.Close,
            _ => LearningAttemptResult.Correct
        };
        var outcome = request.Rating == ReviewRating.Again ? LearningCardOutcome.NeedsReview : LearningCardOutcome.Completed;
        var engineResult = await learningEngine.RecordAsync(new(
            userId, flashcardId, request.StudySessionId, request.Mode ?? StudyMode.Review, request.AttemptType ?? LearningAttemptType.Recall, request.Direction ?? RecallDirection.Forward,
            request.SubmittedAnswer ?? string.Empty, result, request.Confidence ?? request.Rating switch { ReviewRating.Again => 1, ReviewRating.Hard => 2, ReviewRating.Good => 3, _ => 5 },
            request.ResponseTimeMs ?? 0, request.HintUsed, request.ClientAttemptId ?? Guid.NewGuid(), request.Rating, outcome, request.StudySessionId.HasValue), cancellationToken);
        if (!engineResult.IsSuccess)
            return Result<FlashcardProgressDto>.Failure(engineResult.Error!.Code, engineResult.Error.Message, engineResult.Error.Type);
        return Result<FlashcardProgressDto>.Success(ToDto(engineResult.Value!.Progress!, timeProvider.GetUtcNow()));
    }

    public async Task<IReadOnlyList<DueFlashcardDto>> GetDueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return await dbContext.FlashcardProgress.AsNoTracking()
            .Where(progress => progress.UserId == userId && progress.NextReviewAt != null && progress.NextReviewAt <= now)
            .Join(dbContext.Flashcards, progress => progress.FlashcardId, card => card.Id, (progress, card) => new { progress, card })
            .OrderBy(x => x.progress.NextReviewAt)
            .Select(x => new DueFlashcardDto(x.card.Id, x.card.StudySetId, x.card.FrontText, x.card.BackText, x.card.Explanation, x.card.LanguageCode, x.card.ReadingText, x.card.Romanization, x.card.ExampleText, x.card.ExampleTranslation, x.card.MemoryTip, x.progress.NextReviewAt!.Value, x.progress.Status))
            .ToListAsync(cancellationToken);
    }

    private FlashcardProgressDto ToDto(FlashcardProgress value, DateTimeOffset now)
    {
        var assessment = statePolicy.Assess(value, now);
        return new(value.Id, value.FlashcardId, value.Status, value.CorrectCount, value.WrongCount, value.LastReviewedAt!.Value, value.NextReviewAt!.Value, value.IntervalDays, value.EaseFactor, value.MasteryScore, value.WeaknessScore, assessment.RecommendedNextAction, assessment.NextStep, value.StateRevision, value.SchedulerVersion, value.LastScheduledAt);
    }
}
