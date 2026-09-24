using System.Data;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.LearningEngine.DTOs;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Application.SmartLearning.DTOs;
using StudyFlow.Application.SmartLearning.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class SmartLearningService(StudyFlowDbContext dbContext, ILearningEngine learningEngine, ILearningStatePolicy statePolicy, IAnswerEvaluator answerEvaluator, TimeProvider timeProvider) : ISmartLearningService
{
    private const int MaximumRecallAttempts = 3;

    public async Task<Result<SmartLearnSessionDto>> StartAsync(Guid userId, Guid studySetId, StartSmartLearnRequest request, CancellationToken cancellationToken)
    {
        var active = await dbContext.StudySessions.AsNoTracking().Where(x => x.UserId == userId && x.StudySetId == studySetId && x.Mode == StudyMode.Learn && x.EndedAt == null).OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(cancellationToken);
        if (active is not null && await dbContext.SmartLearningCards.AnyAsync(x => x.StudySessionId == active.Id && x.Outcome == LearningCardOutcome.InProgress, cancellationToken)) return await BuildSessionAsync(active, cancellationToken);
        var set = await dbContext.StudySets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studySetId && dbContext.Subjects.Any(subject => subject.Id == x.SubjectId && subject.UserId == userId), cancellationToken);
        if (set is null) return Result<SmartLearnSessionDto>.Failure("STUDY_SET_NOT_FOUND", "Study set was not found.", ErrorType.NotFound);
        var studySetIds = set.Type == StudySetType.Standard ? new List<Guid> { studySetId } : await dbContext.StudySetSources.AsNoTracking().Where(x => x.CombinedStudySetId == studySetId).Select(x => x.SourceStudySetId).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var candidates = await (from card in dbContext.Flashcards.AsNoTracking()
            join progress in dbContext.FlashcardProgress.AsNoTracking().Where(x => x.UserId == userId) on card.Id equals progress.FlashcardId into progressRows
            from progress in progressRows.DefaultIfEmpty()
            where studySetIds.Contains(card.StudySetId)
            select new Candidate(card, progress)).ToListAsync(cancellationToken);
        if (candidates.Count == 0) return Result<SmartLearnSessionDto>.Failure("NO_FLASHCARDS", "Add flashcards before starting Smart Learn.", ErrorType.Validation);
        var ranked = candidates.Select(x => Rank(x, now)).OrderByDescending(x => x.Priority).ThenBy(x => x.Card.OrderIndex).ToList();
        var selected = SelectBalanced(ranked, Math.Min(request.ItemCount, ranked.Count));
        var session = StudySession.Start(userId, studySetId, StudyMode.Learn, now); dbContext.StudySessions.Add(session); await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.SmartLearningCards.AddRange(selected.Select((item, index) => SmartLearningCard.Create(session.Id, item.Card.Id, index, item.IsNew && ranked.Count >= 3 ? LearningAttemptType.MultipleChoice : LearningAttemptType.Recall))); await dbContext.SaveChangesAsync(cancellationToken);
        return Result<SmartLearnSessionDto>.Success(new(session.Id, set.Id, set.Title, now, selected.Select(item => ToItem(item, ranked)).ToList()));
    }

    public async Task<Result<SmartLearnSessionDto>> ResumeAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken)
    {
        var session = await dbContext.StudySessions.AsNoTracking().Where(x => x.UserId == userId && x.StudySetId == studySetId && x.Mode == StudyMode.Learn && x.EndedAt == null).OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(cancellationToken);
        if (session is null) return Result<SmartLearnSessionDto>.Failure("ACTIVE_SESSION_NOT_FOUND", "No active Smart Learn session was found.", ErrorType.NotFound);
        return await BuildSessionAsync(session, cancellationToken);
    }

    public async Task<Result<LearningAttemptDto>> RecordAttemptAsync(Guid userId, Guid sessionId, RecordLearningAttemptRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken) : null;
        if (transaction is not null) await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [sessionId.ToString()], cancellationToken);
        var session = await dbContext.StudySessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId && x.Mode == StudyMode.Learn && x.EndedAt == null, cancellationToken);
        if (session is null) return Result<LearningAttemptDto>.Failure("SMART_SESSION_NOT_FOUND", "Active Smart Learn session was not found.", ErrorType.NotFound);
        var duplicate = await dbContext.LearningAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.StudySessionId == sessionId && x.ClientAttemptId == request.ClientAttemptId, cancellationToken);
        if (duplicate is not null) return await BuildDuplicateResultAsync(userId, session, duplicate, cancellationToken);
        var card = await dbContext.Flashcards.SingleOrDefaultAsync(x => x.Id == request.FlashcardId && (x.StudySetId == session.StudySetId || dbContext.StudySetSources.Any(link => link.CombinedStudySetId == session.StudySetId && link.SourceStudySetId == x.StudySetId)), cancellationToken);
        if (card is null) return Result<LearningAttemptDto>.Failure("FLASHCARD_NOT_FOUND", "Flashcard was not found in this session.", ErrorType.NotFound);
        var sessionCard = await dbContext.SmartLearningCards.SingleOrDefaultAsync(x => x.StudySessionId == sessionId && x.FlashcardId == card.Id, cancellationToken);
        if (sessionCard is null || sessionCard.Outcome != LearningCardOutcome.InProgress) return Result<LearningAttemptDto>.Failure("CARD_NOT_ACTIVE", "This card is not active in the learning session.", ErrorType.Conflict);
        var cardAttempts = await dbContext.LearningAttempts.Where(x => x.StudySessionId == sessionId && x.FlashcardId == card.Id).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        if (cardAttempts.Any(x => x.SrsCommitted)) return Result<LearningAttemptDto>.Failure("CARD_ALREADY_FINISHED", "This card has already finished its learning round.", ErrorType.Conflict);
        var supportsReverse = SupportsReverse(card);
        var availableCards = await dbContext.SmartLearningCards.CountAsync(x => x.StudySessionId == session.Id, cancellationToken);
        var expectedType = sessionCard.AttemptType;
        var expectedDirection = sessionCard.Direction;
        if (request.AttemptType != expectedType || request.AttemptType == LearningAttemptType.Recall && request.Direction != expectedDirection)
            return Result<LearningAttemptDto>.Failure("INVALID_LEARNING_STEP", "The submitted attempt does not match the next required learning step.", ErrorType.Conflict);
        var result = answerEvaluator.Evaluate(card, request.Direction, request.SubmittedAnswer, request.AttemptType); var now = timeProvider.GetUtcNow();
        var outcome = LearningCardOutcome.InProgress; DateTimeOffset? nextReviewAt = null;
        if (request.AttemptType == LearningAttemptType.Recall)
        {
            var previousSuccess = SuccessfulRecallSinceLastFailure(cardAttempts); var requiredGap = Math.Min(3, availableCards - 1);
            var intervening = previousSuccess is null ? 0 : await dbContext.LearningAttempts.Where(x => x.StudySessionId == session.Id && x.FlashcardId != card.Id && x.CreatedAt > previousSuccess.CreatedAt).Select(x => x.FlashcardId).Distinct().CountAsync(cancellationToken);
            var recallCount = cardAttempts.Count(x => x.AttemptType == LearningAttemptType.Recall) + 1;
            if (result == LearningAttemptResult.Correct && previousSuccess is not null && intervening >= requiredGap) outcome = LearningCardOutcome.Completed;
            else if (recallCount >= MaximumRecallAttempts) outcome = LearningCardOutcome.NeedsReview;
        }
        var rating = outcome == LearningCardOutcome.InProgress ? (ReviewRating?)null : outcome == LearningCardOutcome.Completed ? ReviewRating.Good : ReviewRating.Again;
        var engineResult = await learningEngine.RecordAsync(new RecordLearningEvidence(
            userId, card.Id, session.Id, StudyMode.Learn, request.AttemptType, request.Direction, request.SubmittedAnswer,
            result, request.Confidence, request.ResponseTimeMs, request.HintUsed, request.ClientAttemptId,
            rating, outcome == LearningCardOutcome.InProgress ? null : outcome, outcome != LearningCardOutcome.InProgress), cancellationToken);
        if (!engineResult.IsSuccess) return Result<LearningAttemptDto>.Failure(engineResult.Error!.Code, engineResult.Error.Message, engineResult.Error.Type);
        var attempt = engineResult.Value!.Attempt;
        if (outcome != LearningCardOutcome.InProgress)
        {
            sessionCard.Finish(outcome, now);
            nextReviewAt = engineResult.Value.Progress?.NextReviewAt;
        }
        var next = NextStep(attempt, outcome, cardAttempts, supportsReverse);
        if (outcome == LearningCardOutcome.InProgress && next.Type.HasValue && next.Direction.HasValue) { var attemptNumber = await dbContext.LearningAttempts.CountAsync(x => x.StudySessionId == session.Id, cancellationToken) + 1; sessionCard.Schedule(next.Type.Value, next.Direction.Value, attemptNumber + next.RetryAfterItems, now); }
        await dbContext.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return Result<LearningAttemptDto>.Success(ToDto(attempt, outcome, next.Type, next.Direction, next.RetryAfterItems, nextReviewAt));
    }

    public async Task<Result<SmartLearnSummaryDto>> CompleteAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.StudySessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId && x.Mode == StudyMode.Learn, cancellationToken);
        if (session is null) return Result<SmartLearnSummaryDto>.Failure("SMART_SESSION_NOT_FOUND", "Smart Learn session was not found.", ErrorType.NotFound);
        if (!session.IsCompleted)
        {
            var attempts = await dbContext.LearningAttempts.Where(x => x.StudySessionId == sessionId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
            foreach (var group in attempts.GroupBy(x => x.FlashcardId).Where(x => !x.Any(a => a.SrsCommitted) && x.Any(a => a.Result != LearningAttemptResult.Correct)))
            {
                var finalAttempt = group.Last();
                var commit = await learningEngine.CommitAsync(userId, finalAttempt, ReviewRating.Again, LearningCardOutcome.NeedsReview, true, cancellationToken);
                if (!commit.IsSuccess) return Result<SmartLearnSummaryDto>.Failure(commit.Error!.Code, commit.Error.Message, commit.Error.Type);
                var now = timeProvider.GetUtcNow(); var state = await dbContext.SmartLearningCards.SingleOrDefaultAsync(x => x.StudySessionId == session.Id && x.FlashcardId == group.Key, cancellationToken); state?.Finish(LearningCardOutcome.NeedsReview, now);
            }
            session.Complete(timeProvider.GetUtcNow()); await dbContext.SaveChangesAsync(cancellationToken);
        }
        return Result<SmartLearnSummaryDto>.Success(await BuildSummaryAsync(userId, session, cancellationToken));
    }

    private async Task<Result<LearningAttemptDto>> BuildDuplicateResultAsync(Guid userId, StudySession session, LearningAttempt duplicate, CancellationToken cancellationToken)
    {
        var attempts = await dbContext.LearningAttempts.AsNoTracking().Where(x => x.StudySessionId == session.Id && x.FlashcardId == duplicate.FlashcardId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var card = await dbContext.Flashcards.AsNoTracking().SingleAsync(x => x.Id == duplicate.FlashcardId, cancellationToken);
        var outcome = duplicate.CommittedOutcome ?? LearningCardOutcome.InProgress;
        var next = NextStep(duplicate, outcome, attempts.Where(x => x.Id != duplicate.Id).ToList(), SupportsReverse(card));
        var nextReview = duplicate.SrsCommitted ? await dbContext.FlashcardProgress.AsNoTracking().Where(x => x.UserId == userId && x.FlashcardId == duplicate.FlashcardId).Select(x => x.NextReviewAt).SingleOrDefaultAsync(cancellationToken) : null;
        return Result<LearningAttemptDto>.Success(ToDto(duplicate, outcome, next.Type, next.Direction, next.RetryAfterItems, nextReview));
    }

    private async Task<SmartLearnSummaryDto> BuildSummaryAsync(Guid userId, StudySession session, CancellationToken cancellationToken)
    {
        var attempts = await dbContext.LearningAttempts.AsNoTracking().Where(x => x.StudySessionId == session.Id).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var cardIds = attempts.Select(x => x.FlashcardId).Distinct().ToList(); var progress = await dbContext.FlashcardProgress.AsNoTracking().Where(x => x.UserId == userId && cardIds.Contains(x.FlashcardId)).ToListAsync(cancellationToken); var terminal = attempts.Where(x => x.SrsCommitted).ToList();
        var improved = attempts.GroupBy(x => x.FlashcardId).Count(group => group.Any(x => x.Result != LearningAttemptResult.Correct) && group.Any(x => x.CommittedOutcome == LearningCardOutcome.Completed));
        var mastered = progress.Count(x => x.Status == FlashcardStatus.Mastered && x.LastReviewedAt >= session.StartedAt); var nextReview = progress.Where(x => x.NextReviewAt.HasValue).Select(x => x.NextReviewAt).Min();
        return new(session.Id, session.DurationSeconds, attempts.Count, terminal.Count(x => x.CommittedOutcome == LearningCardOutcome.Completed), terminal.Count(x => x.CommittedOutcome == LearningCardOutcome.NeedsReview), improved, mastered, nextReview);
    }

    private async Task<Result<SmartLearnSessionDto>> BuildSessionAsync(StudySession session, CancellationToken cancellationToken)
    {
        var set = await dbContext.StudySets.AsNoTracking().SingleAsync(x => x.Id == session.StudySetId, cancellationToken); var attemptCount = await dbContext.LearningAttempts.CountAsync(x => x.StudySessionId == session.Id, cancellationToken);
        var states = await dbContext.SmartLearningCards.AsNoTracking().Where(x => x.StudySessionId == session.Id && x.Outcome == LearningCardOutcome.InProgress).OrderBy(x => x.NextEligibleAttemptNumber > attemptCount).ThenBy(x => x.NextEligibleAttemptNumber).ThenBy(x => x.OrderIndex).ToListAsync(cancellationToken); var ids = states.Select(x => x.FlashcardId).ToList(); var cards = await dbContext.Flashcards.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var all = states.Select(x => new RankedCandidate(cards[x.FlashcardId], null, 0, "Resume", false, false, x.AttemptType == LearningAttemptType.MultipleChoice, false)).ToList(); var items = states.Select(state => ToItem(all.Single(x => x.Card.Id == state.FlashcardId), all) with { AttemptType = state.AttemptType, Direction = state.Direction }).ToList(); return Result<SmartLearnSessionDto>.Success(new(session.Id, session.StudySetId, set.Title, session.StartedAt, items));
    }

    private static LearningAttempt? SuccessfulRecallSinceLastFailure(IReadOnlyList<LearningAttempt> attempts)
    {
        var recalls = attempts.Where(x => x.AttemptType == LearningAttemptType.Recall).ToList(); var lastFailure = recalls.FindLastIndex(x => x.Result != LearningAttemptResult.Correct); return recalls.Skip(lastFailure + 1).FirstOrDefault(x => x.Result == LearningAttemptResult.Correct);
    }

    private static (LearningAttemptType? Type, RecallDirection? Direction, int RetryAfterItems) NextStep(LearningAttempt attempt, LearningCardOutcome outcome, IReadOnlyList<LearningAttempt> previous, bool supportsReverse)
    {
        if (outcome != LearningCardOutcome.InProgress) return (null, null, 0);
        var all = previous.Concat([attempt]).OrderBy(x => x.CreatedAt).ToList(); var direction = ExpectedDirection(all, supportsReverse);
        if (attempt.AttemptType == LearningAttemptType.MultipleChoice) return (LearningAttemptType.Recall, RecallDirection.Forward, 2);
        var failures = all.Count(x => x.AttemptType == LearningAttemptType.Recall && x.Result != LearningAttemptResult.Correct);
        return (LearningAttemptType.Recall, direction, attempt.Result == LearningAttemptResult.Correct ? 3 : failures <= 1 ? 3 : 5);
    }

    private static RecallDirection ExpectedDirection(IReadOnlyList<LearningAttempt> attempts, bool supportsReverse) => supportsReverse && attempts.Any(x => x.AttemptType == LearningAttemptType.Recall && x.Direction == RecallDirection.Forward && x.Result == LearningAttemptResult.Correct) ? RecallDirection.Reverse : RecallDirection.Forward;
    private static LearningAttemptDto ToDto(LearningAttempt attempt, LearningCardOutcome outcome, LearningAttemptType? nextType, RecallDirection? nextDirection, int retryAfterItems, DateTimeOffset? nextReviewAt) => new(attempt.Id, attempt.ClientAttemptId, attempt.FlashcardId, attempt.AttemptType, attempt.Direction, attempt.Result, attempt.Confidence, attempt.ResponseTimeMs, attempt.HintUsed, attempt.CreatedAt, outcome, nextType, nextDirection, retryAfterItems, attempt.SrsCommitted, nextReviewAt);

    private static bool SupportsReverse(Flashcard card) => card.EnableReverseRecall;

    private RankedCandidate Rank(Candidate value, DateTimeOffset now)
    {
        var assessment = statePolicy.Assess(value.Progress, now);
        return new(value.Card, value.Progress, assessment.Priority, assessment.Reason, assessment.IsDue, assessment.IsWeak, assessment.IsNew, (value.Progress?.WrongCount ?? 0) > 0);
    }
    private static IReadOnlyList<RankedCandidate> SelectBalanced(IReadOnlyList<RankedCandidate> ranked, int count)
    {
        var selected = new List<RankedCandidate>(); var seen = new HashSet<Guid>(); var groups = new[] { ranked.Where(x => x.IsDue).Take((int)Math.Ceiling(count * .4)).ToList(), ranked.Where(x => x.IsWeak).Take((int)Math.Ceiling(count * .25)).ToList(), ranked.Where(x => x.IsNew).Take((int)Math.Ceiling(count * .2)).ToList(), ranked.Where(x => x.WasWrong).Take((int)Math.Ceiling(count * .15)).ToList() };
        for (var position = 0; selected.Count < count && groups.Any(group => position < group.Count); position++) foreach (var group in groups) if (position < group.Count && seen.Add(group[position].Card.Id)) selected.Add(group[position]); foreach (var item in ranked) if (selected.Count < count && seen.Add(item.Card.Id)) selected.Add(item); return selected.Take(count).ToList();
    }
    private static SmartLearnItemDto ToItem(RankedCandidate item, IReadOnlyList<RankedCandidate> all)
    {
        var useChoices = item.IsNew && all.Count >= 3; var options = new List<SmartLearnOptionDto>(); if (useChoices) { options.Add(new(item.Card.BackText)); options.AddRange(all.Where(x => x.Card.Id != item.Card.Id).Select(x => x.Card.BackText).Distinct().Take(3).Select(x => new SmartLearnOptionDto(x))); options = options.OrderBy(x => StableOrder(item.Card.Id, x.Text)).ToList(); }
        return new(item.Card.Id, item.Card.FrontText, item.Card.BackText, item.Card.Explanation, item.Card.LanguageCode, item.Card.ReadingText, item.Card.Romanization, item.Card.ExampleText, item.Card.ExampleTranslation, item.Card.MemoryTip, useChoices ? LearningAttemptType.MultipleChoice : LearningAttemptType.Recall, RecallDirection.Forward, SupportsReverse(item.Card), options, item.Priority, item.Reason);
    }
    private static int StableOrder(Guid id, string value) => HashCode.Combine(id, value);
    private sealed record Candidate(Flashcard Card, FlashcardProgress? Progress);
    private sealed record RankedCandidate(Flashcard Card, FlashcardProgress? Progress, int Priority, string Reason, bool IsDue, bool IsWeak, bool IsNew, bool WasWrong);
}
