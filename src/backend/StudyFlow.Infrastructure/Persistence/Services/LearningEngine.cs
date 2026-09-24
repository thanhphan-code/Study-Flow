using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.LearningEngine.DTOs;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Application.Progress.DTOs;
using StudyFlow.Application.Progress.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class LearningEngine(
    StudyFlowDbContext dbContext,
    ISrsScheduler scheduler,
    ILearningStatePolicy statePolicy,
    IAnswerEvaluator answerEvaluator,
    TimeProvider timeProvider) : ILearningEngine
{
    public async Task<Result<LearningEngineResult>> RecordAsync(RecordLearningEvidence evidence, CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true && dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
            await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [$"{evidence.UserId}:{evidence.FlashcardId}"], cancellationToken);
        var card = await dbContext.Flashcards.SingleOrDefaultAsync(card => card.Id == evidence.FlashcardId
            && dbContext.StudySets.Any(set => set.Id == card.StudySetId
                && dbContext.Subjects.Any(subject => subject.Id == set.SubjectId && subject.UserId == evidence.UserId)), cancellationToken);
        if (card is null)
            return Result<LearningEngineResult>.Failure("FLASHCARD_NOT_FOUND", "Flashcard was not found.", ErrorType.NotFound);

        StudySession? session = null;
        var mode = evidence.Mode;
        if (evidence.StudySessionId.HasValue)
        {
            session = await dbContext.StudySessions.SingleOrDefaultAsync(x => x.Id == evidence.StudySessionId && x.UserId == evidence.UserId, cancellationToken);
            if (session is null)
                return Result<LearningEngineResult>.Failure("STUDY_SESSION_NOT_FOUND", "Study session was not found.", ErrorType.NotFound);
            if (session.IsCompleted && session.Mode != StudyMode.Quiz)
                return Result<LearningEngineResult>.Failure("STUDY_SESSION_COMPLETED", "Study session is already completed.", ErrorType.Conflict);
            var cardStudySetId = await dbContext.Flashcards.Where(x => x.Id == evidence.FlashcardId).Select(x => x.StudySetId).SingleAsync(cancellationToken);
            var belongsToSession = cardStudySetId == session.StudySetId
                || await dbContext.StudySetSources.AnyAsync(x => x.CombinedStudySetId == session.StudySetId && x.SourceStudySetId == cardStudySetId, cancellationToken);
            if (!belongsToSession)
                return Result<LearningEngineResult>.Failure("FLASHCARD_NOT_IN_SESSION", "Flashcard does not belong to this study session.", ErrorType.Validation);
            mode = session.Mode;
        }

        var duplicate = await dbContext.LearningAttempts.SingleOrDefaultAsync(
            x => x.UserId == evidence.UserId && x.ClientAttemptId == evidence.ClientAttemptId,
            cancellationToken);
        if (duplicate is not null)
        {
            if (duplicate.FlashcardId != evidence.FlashcardId || duplicate.StudySessionId != evidence.StudySessionId
                || duplicate.AttemptType != evidence.AttemptType || duplicate.Direction != evidence.Direction
                || !string.Equals(duplicate.SubmittedAnswer, evidence.SubmittedAnswer.Trim(), StringComparison.Ordinal))
                return Result<LearningEngineResult>.Failure("IDEMPOTENCY_KEY_REUSED", "ClientAttemptId was already used with different learning evidence.", ErrorType.Conflict);
            var existingProgress = await dbContext.FlashcardProgress.SingleOrDefaultAsync(
                x => x.UserId == evidence.UserId && x.FlashcardId == duplicate.FlashcardId,
                cancellationToken);
            return Result<LearningEngineResult>.Success(new(duplicate, existingProgress));
        }

        var now = timeProvider.GetUtcNow();
        var result = evidence.Result ?? answerEvaluator.Evaluate(card, evidence.Direction, evidence.SubmittedAnswer, evidence.AttemptType);
        var commitRating = evidence.CommitRating;
        var commitOutcome = evidence.CommitOutcome;
        if (!evidence.Result.HasValue && commitRating.HasValue)
        {
            if (result == LearningAttemptResult.Wrong) { commitRating = ReviewRating.Again; commitOutcome = LearningCardOutcome.NeedsReview; }
            else if (result == LearningAttemptResult.Close && commitRating is ReviewRating.Good or ReviewRating.Easy) commitRating = ReviewRating.Hard;
        }
        var attempt = LearningAttempt.Create(
            evidence.ClientAttemptId,
            evidence.UserId,
            evidence.FlashcardId,
            evidence.StudySessionId,
            mode,
            evidence.AttemptType,
            evidence.Direction,
            evidence.SubmittedAnswer,
            result,
            evidence.Confidence,
            evidence.ResponseTimeMs,
            evidence.HintUsed,
            now);
        dbContext.LearningAttempts.Add(attempt);

        FlashcardProgress? progress = null;
        if (commitRating.HasValue && commitOutcome.HasValue)
        {
            progress = await ApplyProgressAsync(evidence.UserId, attempt, commitRating.Value, commitOutcome.Value, now, cancellationToken);
            if (evidence.CountTowardsSession && session is not null)
                session.RecordCardReview(commitOutcome == LearningCardOutcome.Completed, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return Result<LearningEngineResult>.Success(new(attempt, progress));
    }

    public async Task<Result<LearningEngineResult>> CommitAsync(Guid userId, LearningAttempt attempt, ReviewRating rating, LearningCardOutcome outcome, bool countTowardsSession, CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true && dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
            await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [$"{userId}:{attempt.FlashcardId}"], cancellationToken);
        if (attempt.UserId != userId)
            return Result<LearningEngineResult>.Failure("LEARNING_ATTEMPT_NOT_FOUND", "Learning attempt was not found.", ErrorType.NotFound);
        if (attempt.SrsCommitted)
        {
            var existing = await dbContext.FlashcardProgress.SingleOrDefaultAsync(x => x.UserId == userId && x.FlashcardId == attempt.FlashcardId, cancellationToken);
            return Result<LearningEngineResult>.Success(new(attempt, existing));
        }

        var now = timeProvider.GetUtcNow();
        var progress = await ApplyProgressAsync(userId, attempt, rating, outcome, now, cancellationToken);
        if (countTowardsSession && attempt.StudySessionId.HasValue)
        {
            var session = await dbContext.StudySessions.SingleOrDefaultAsync(x => x.Id == attempt.StudySessionId && x.UserId == userId, cancellationToken);
            if (session is null)
                return Result<LearningEngineResult>.Failure("STUDY_SESSION_NOT_FOUND", "Study session was not found.", ErrorType.NotFound);
            session.RecordCardReview(outcome == LearningCardOutcome.Completed, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return Result<LearningEngineResult>.Success(new(attempt, progress));
    }

    private async Task<FlashcardProgress> ApplyProgressAsync(Guid userId, LearningAttempt attempt, ReviewRating rating, LearningCardOutcome outcome, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var progress = await dbContext.FlashcardProgress.SingleOrDefaultAsync(x => x.UserId == userId && x.FlashcardId == attempt.FlashcardId, cancellationToken);
        var wasNew = progress is null;
        if (progress is null)
        {
            progress = FlashcardProgress.Create(userId, attempt.FlashcardId);
            dbContext.FlashcardProgress.Add(progress);
        }

        var before = statePolicy.Assess(wasNew ? null : progress, now); var intervalBefore = progress.IntervalDays; var nextReviewBefore = progress.NextReviewAt;
        progress.ApplyEvidence(attempt, rating, now);
        var schedule = scheduler.Calculate(new(progress.Status, progress.IntervalDays, progress.EaseFactor, rating,
            attempt.AttemptType, attempt.Result, attempt.Confidence, attempt.ResponseTimeMs, attempt.HintUsed, progress.ConsecutiveWrong, now));
        progress.ApplySchedule(rating, schedule.Status, schedule.IntervalDays, schedule.EaseFactor, now, schedule.NextReviewAt, scheduler.Version);
        var assessment = statePolicy.Assess(progress, now);
        progress.UpdateLearningState(assessment.MasteryScore, assessment.WeaknessScore, now);
        attempt.MarkSrsCommitted(rating, outcome, now);
        var trace = JsonSerializer.Serialize(new
        {
            attempt.Result, attempt.AttemptType, attempt.Direction, attempt.HintUsed, attempt.Confidence, attempt.ResponseTimeMs,
            EffectiveRating = rating, ConsecutiveWrong = progress.ConsecutiveWrong, RecentWrongCount = progress.RecentWrongCount,
            BeforeReason = before.NextStep.Reason, AfterReason = assessment.NextStep.Reason
        });
        dbContext.LearningDecisions.Add(LearningDecision.Create(attempt.Id, before.MasteryScore, assessment.MasteryScore,
            before.WeaknessScore, assessment.WeaknessScore, intervalBefore, progress.IntervalDays, nextReviewBefore,
            progress.NextReviewAt, before.NextStep.Activity.ToString(), assessment.NextStep.Activity.ToString(),
            statePolicy.Version, scheduler.Version, trace, now));
        return progress;
    }

}
