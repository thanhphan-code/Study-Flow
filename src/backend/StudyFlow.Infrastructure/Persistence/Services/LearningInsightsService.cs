using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.LearningInsights.DTOs;
using StudyFlow.Application.LearningInsights.Interfaces;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class LearningInsightsService(StudyFlowDbContext dbContext, ILearningStatePolicy statePolicy, TimeProvider timeProvider) : ILearningInsightsService
{
    public async Task<ProgressOverviewDto> GetOverviewAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cardRows = await OwnedCards(userId).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var cards = cardRows.Select(x => new { State = ToState(x), Assessment = statePolicy.Assess(x.Progress, now) }).ToList();
        var attempts = await OwnedAttempts(userId).ToListAsync(cancellationToken);
        var weak = cards.Count(x => x.Assessment.IsWeak);
        return new ProgressOverviewDto(
            await dbContext.StudySets.CountAsync(set => dbContext.Subjects.Any(subject => subject.Id == set.SubjectId && subject.UserId == userId), cancellationToken),
            cards.Count, cards.Count(x => x.State.Status == FlashcardStatus.New), cards.Count(x => x.State.Status == FlashcardStatus.Learning),
            cards.Count(x => x.State.Status == FlashcardStatus.Reviewing), cards.Count(x => x.State.Status == FlashcardStatus.Mastered),
            cards.Count(x => x.State.CorrectCount + x.State.WrongCount > 0), attempts.Count, Accuracy(attempts), weak,
            cards.Count(x => x.Assessment.IsDue || x.Assessment.IsWeak));
    }

    public async Task<Result<StudySetProgressDto>> GetStudySetProgressAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken)
    {
        var set = await dbContext.StudySets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studySetId && dbContext.Subjects.Any(subject => subject.Id == x.SubjectId && subject.UserId == userId), cancellationToken);
        if (set is null) return Result<StudySetProgressDto>.Failure("STUDY_SET_NOT_FOUND", "Study set was not found.", ErrorType.NotFound);
        var sourceIds = set.Type == StudySetType.Standard ? new List<Guid> { studySetId } : await dbContext.StudySetSources.AsNoTracking().Where(x => x.CombinedStudySetId == studySetId).Select(x => x.SourceStudySetId).ToListAsync(cancellationToken);
        var cardRows = await OwnedCards(userId, sourceIds).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var cards = cardRows.Select(x => new { State = ToState(x), Assessment = statePolicy.Assess(x.Progress, now) }).ToList();
        var attempts = await OwnedAttempts(userId, studySetId).ToListAsync(cancellationToken);
        return Result<StudySetProgressDto>.Success(new(set.Id, set.Title, cards.Count, cards.Count(x => x.State.Status == FlashcardStatus.New), cards.Count(x => x.State.Status == FlashcardStatus.Learning), cards.Count(x => x.State.Status == FlashcardStatus.Reviewing), cards.Count(x => x.State.Status == FlashcardStatus.Mastered), cards.Count(x => x.State.CorrectCount + x.State.WrongCount > 0), attempts.Count, Accuracy(attempts), cards.Count(x => x.Assessment.IsWeak)));
    }

    public async Task<IReadOnlyList<WeakCardDto>> GetWeakCardsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var rows = await OwnedCards(userId).ToListAsync(cancellationToken);
        return rows.Select(x => new { Row = x, State = ToState(x), Assessment = statePolicy.Assess(x.Progress, now) })
            .Where(x => x.Assessment.IsWeak).Select(x => new WeakCardDto(x.Row.Id, x.Row.StudySetId, x.Row.StudySetTitle, x.Row.FrontText, x.State.Status, x.State.CorrectCount, x.State.WrongCount, x.State.EaseFactor, x.State.IntervalDays, x.State.NextReviewAt, (int)Math.Round(x.Assessment.WeaknessScore), x.Assessment.IsDue, x.Assessment.Reason))
            .OrderByDescending(x => x.IsDue).ThenByDescending(x => x.WeaknessScore).ThenBy(x => x.NextReviewAt).Take(50).ToList();
    }

    private IQueryable<OwnedCardRow> OwnedCards(Guid userId, IReadOnlyCollection<Guid>? studySetIds = null) =>
        from card in dbContext.Flashcards.AsNoTracking()
        join set in dbContext.StudySets.AsNoTracking() on card.StudySetId equals set.Id
        join subject in dbContext.Subjects.AsNoTracking() on set.SubjectId equals subject.Id
        join progress in dbContext.FlashcardProgress.AsNoTracking().Where(x => x.UserId == userId) on card.Id equals progress.FlashcardId into progressRows
        from progress in progressRows.DefaultIfEmpty()
        where subject.UserId == userId && (studySetIds == null || studySetIds.Contains(set.Id))
        select new OwnedCardRow(card.Id, set.Id, set.Title, card.FrontText, progress);

    private IQueryable<AttemptRow> OwnedAttempts(Guid userId, Guid? studySetId = null) =>
        from attempt in dbContext.QuizAttempts.AsNoTracking()
        join quiz in dbContext.Quizzes.AsNoTracking() on attempt.QuizId equals quiz.Id
        where attempt.UserId == userId && attempt.CompletedAt != null && (!studySetId.HasValue || quiz.StudySetId == studySetId.Value)
        select new AttemptRow(quiz.StudySetId, attempt.CorrectCount, attempt.WrongCount);

    private static CardState ToState(OwnedCardRow x) => x.Progress is null ? new(FlashcardStatus.New, 0, 0, 2.5, 0, null) : new(x.Progress.Status, x.Progress.CorrectCount, x.Progress.WrongCount, x.Progress.EaseFactor, x.Progress.IntervalDays, x.Progress.NextReviewAt);
    private static decimal Accuracy(IReadOnlyCollection<AttemptRow> attempts)
    {
        var questions = attempts.Sum(x => x.CorrectCount + x.WrongCount);
        return questions == 0 ? 0 : Math.Round((decimal)attempts.Sum(x => x.CorrectCount) * 100 / questions, 2);
    }

    private sealed record OwnedCardRow(Guid Id, Guid StudySetId, string StudySetTitle, string FrontText, Domain.Entities.FlashcardProgress? Progress);
    private sealed record AttemptRow(Guid StudySetId, int CorrectCount, int WrongCount);
    private sealed record CardState(FlashcardStatus Status, int CorrectCount, int WrongCount, double EaseFactor, int IntervalDays, DateTimeOffset? NextReviewAt);
}
