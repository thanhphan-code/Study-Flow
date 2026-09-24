using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.DailyStudy.DTOs;
using StudyFlow.Application.DailyStudy.Interfaces;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class DailyStudyService(StudyFlowDbContext dbContext, ILearningStatePolicy statePolicy, TimeProvider timeProvider) : IDailyStudyService
{
    public async Task<DailyStudyPlanDto> CreatePlanAsync(Guid userId, DailyStudyRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var rows = await (from card in dbContext.Flashcards.AsNoTracking()
            join set in dbContext.StudySets.AsNoTracking() on card.StudySetId equals set.Id
            join subject in dbContext.Subjects.AsNoTracking() on set.SubjectId equals subject.Id
            join progress in dbContext.FlashcardProgress.AsNoTracking().Where(x => x.UserId == userId) on card.Id equals progress.FlashcardId into progressRows
            from progress in progressRows.DefaultIfEmpty()
            where subject.UserId == userId
            select new Candidate(card, set.Title, progress)).ToListAsync(cancellationToken);

        if (request.Focus.Equals("Language", StringComparison.OrdinalIgnoreCase)) rows = rows.Where(x => x.Card.LanguageCode != null).ToList();
        var ranked = rows.Select(x => Rank(x, now)).OrderByDescending(x => x.Priority).ThenBy(x => x.Card.OrderIndex).ToList();
        var count = request.Minutes switch { 5 => 8, 10 => 15, 15 => 22, _ => 50 };
        var selected = SelectBalanced(ranked, Math.Min(count, ranked.Count));
        var items = selected.Select((item, index) => ToDto(item, request.Focus, index)).ToList();
        return new(request.Minutes, request.Focus, selected.Count(x => x.IsDue), selected.Count(x => x.IsWeak), selected.Count(x => x.IsNew), items);
    }

    private RankedCandidate Rank(Candidate value, DateTimeOffset now)
    {
        var assessment = statePolicy.Assess(value.Progress, now);
        return new(value.Card, value.StudySetTitle, assessment.Priority, assessment.Reason, assessment.IsDue, assessment.IsWeak, assessment.IsNew);
    }

    private static IReadOnlyList<RankedCandidate> SelectBalanced(IReadOnlyList<RankedCandidate> ranked, int count)
    {
        var selected = new List<RankedCandidate>(); var seen = new HashSet<Guid>();
        void Add(IEnumerable<RankedCandidate> source, int limit) { foreach (var item in source.Take(limit)) if (selected.Count < count && seen.Add(item.Card.Id)) selected.Add(item); }
        Add(ranked.Where(x => x.IsDue), (int)Math.Ceiling(count * .5));
        Add(ranked.Where(x => x.IsWeak), (int)Math.Ceiling(count * .3));
        Add(ranked.Where(x => x.IsNew), (int)Math.Ceiling(count * .2));
        Add(ranked, count);
        return selected.Take(count).ToList();
    }

    private static DailyStudyItemDto ToDto(RankedCandidate item, string focus, int index)
    {
        var language = item.Card.LanguageCode != null;
        var activity = language && (focus.Equals("Language", StringComparison.OrdinalIgnoreCase) || focus.Equals("Balanced", StringComparison.OrdinalIgnoreCase))
            ? (index % 3) switch { 0 => "Listening", 1 => "ReverseWritten", _ => "Written" }
            : "Written";
        var card = item.Card;
        return new(card.Id, card.StudySetId, item.StudySetTitle, card.FrontText, card.BackText, card.Explanation, card.LanguageCode, card.ReadingText, card.Romanization, card.ExampleText, card.ExampleTranslation, card.MemoryTip, card.AcceptedAnswers, card.ImageStorageKey is null ? null : $"/api/flashcards/{card.Id}/image", activity, item.Reason);
    }

    private sealed record Candidate(Flashcard Card, string StudySetTitle, FlashcardProgress? Progress);
    private sealed record RankedCandidate(Flashcard Card, string StudySetTitle, int Priority, string Reason, bool IsDue, bool IsWeak, bool IsNew);
}
