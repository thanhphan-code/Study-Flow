using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.StudySets.Interfaces;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class StudySetService(StudyFlowDbContext dbContext, IFileStorageService storage, ILearningStatePolicy statePolicy, TimeProvider timeProvider) : IStudySetService
{
    public async Task<Result<IReadOnlyList<StudySetDto>>> ListAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        if (!await OwnsSubjectAsync(userId, subjectId, cancellationToken)) return Result<IReadOnlyList<StudySetDto>>.Failure("SUBJECT_NOT_FOUND", "Subject was not found.", ErrorType.NotFound);
        var sets = await dbContext.StudySets.AsNoTracking().Where(x => x.SubjectId == subjectId).OrderByDescending(x => x.UpdatedAt).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<StudySetDto>>.Success(await BuildDtosAsync(sets, cancellationToken));
    }
    public async Task<Result<StudySetDto>> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken) { var value = await FindOwnedAsync(userId, id, true, cancellationToken); return value is null ? NotFound() : Result<StudySetDto>.Success((await BuildDtosAsync([value], cancellationToken))[0]); }
    public async Task<Result<StudySetDto>> CreateAsync(Guid userId, Guid subjectId, CreateStudySetRequest request, CancellationToken cancellationToken) { if (!await OwnsSubjectAsync(userId, subjectId, cancellationToken)) return NotFound("SUBJECT_NOT_FOUND", "Subject was not found."); var value = StudySet.Create(subjectId, request.Title, request.Description); dbContext.StudySets.Add(value); await dbContext.SaveChangesAsync(cancellationToken); return Result<StudySetDto>.Success(ToDto(value, [], 0)); }
    public async Task<Result<StudySetDto>> CreateCombinedAsync(Guid userId, Guid subjectId, CreateCombinedStudySetRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsSubjectAsync(userId, subjectId, cancellationToken)) return NotFound("SUBJECT_NOT_FOUND", "Subject was not found."); var sources = await LoadValidSourcesAsync(userId, request.SourceStudySetIds, subjectId, cancellationToken); if (sources is null) return Result<StudySetDto>.Failure("INVALID_STUDY_SET_SOURCES", "Sources must be standard study sets in this subject.", ErrorType.Validation);
        var combined = StudySet.CreateCombined(subjectId, request.Title, request.Description); dbContext.StudySets.Add(combined); dbContext.StudySetSources.AddRange(sources.Select((source, index) => StudySetSource.Create(combined.Id, source.Id, index))); await dbContext.SaveChangesAsync(cancellationToken); return Result<StudySetDto>.Success(ToDto(combined, sources.Select((x, i) => new StudySetSourceDto(x.Id, x.Title, i)).ToList(), await CountCardsAsync(sources.Select(x => x.Id), cancellationToken)));
    }
    public async Task<Result<StudySetDto>> UpdateAsync(Guid userId, Guid id, UpdateStudySetRequest request, CancellationToken cancellationToken) { var value = await FindOwnedAsync(userId, id, false, cancellationToken); if (value is null) return NotFound(); value.Update(request.Title, request.Description); await dbContext.SaveChangesAsync(cancellationToken); return Result<StudySetDto>.Success((await BuildDtosAsync([value], cancellationToken))[0]); }
    public async Task<Result<StudySetDto>> UpdateSourcesAsync(Guid userId, Guid studySetId, UpdateStudySetSourcesRequest request, CancellationToken cancellationToken)
    {
        var combined = await FindOwnedAsync(userId, studySetId, false, cancellationToken); if (combined is null) return NotFound(); if (combined.Type != StudySetType.Combined) return Result<StudySetDto>.Failure("NOT_COMBINED_STUDY_SET", "Only combined study sets have sources.", ErrorType.Validation); var sources = await LoadValidSourcesAsync(userId, request.SourceStudySetIds, combined.SubjectId, cancellationToken); if (sources is null) return Result<StudySetDto>.Failure("INVALID_STUDY_SET_SOURCES", "Sources must be standard study sets in this subject.", ErrorType.Validation);
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var old = await dbContext.StudySetSources.Where(x => x.CombinedStudySetId == studySetId).ToListAsync(cancellationToken);
        dbContext.StudySetSources.RemoveRange(old);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.StudySetSources.AddRange(sources.Select((source, index) => StudySetSource.Create(studySetId, source.Id, index)));
        combined.Update(combined.Title, combined.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return Result<StudySetDto>.Success((await BuildDtosAsync([combined], cancellationToken))[0]);
    }
    public async Task<Result<StudyTogetherDto>> StudyTogetherAsync(Guid userId, StudyTogetherRequest request, CancellationToken cancellationToken)
    {
        var sources = await LoadValidSourcesAsync(userId, request.SourceStudySetIds, null, cancellationToken);
        if (sources is null || sources.Select(x => x.SubjectId).Distinct().Count() != 1) return Result<StudyTogetherDto>.Failure("INVALID_STUDY_SET_SOURCES", "Choose standard study sets from the same subject.", ErrorType.Validation);
        var ids = sources.Select(x => x.Id).ToList();
        var rows = await (from card in dbContext.Flashcards.AsNoTracking()
            join progress in dbContext.FlashcardProgress.AsNoTracking().Where(x => x.UserId == userId) on card.Id equals progress.FlashcardId into progressRows
            from progress in progressRows.DefaultIfEmpty()
            where ids.Contains(card.StudySetId)
            select new StudyTogetherCandidate(card, progress)).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var ranked = rows.OrderByDescending(x => statePolicy.Assess(x.Progress, now).Priority).ThenBy(x => x.Card.OrderIndex).Select(x => x.Card).ToList();
        return Result<StudyTogetherDto>.Success(new(sources.Select((x, i) => new StudySetSourceDto(x.Id, x.Title, i)).ToList(), ranked.Select(ToFlashcardDto).ToList()));
    }
    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var value = await FindOwnedAsync(userId, id, false, cancellationToken); if (value is null) return Result<bool>.Failure("STUDY_SET_NOT_FOUND", "Study set was not found.", ErrorType.NotFound); if (value.Type == StudySetType.Standard) { var usedBy = await dbContext.StudySetSources.Where(x => x.SourceStudySetId == id).Join(dbContext.StudySets, x => x.CombinedStudySetId, x => x.Id, (_, set) => set.Title).ToListAsync(cancellationToken); if (usedBy.Count > 0) return Result<bool>.Failure("STUDY_SET_IN_COMBINED_SET", $"This study set is included in: {string.Join(", ", usedBy)}. Remove it from those review sets first.", ErrorType.Conflict); }
        var imageKeys = await dbContext.Flashcards.Where(x => x.StudySetId == id && x.ImageStorageKey != null).Select(x => x.ImageStorageKey!).ToListAsync(cancellationToken); dbContext.StudySets.Remove(value); await dbContext.SaveChangesAsync(cancellationToken); foreach (var key in imageKeys) await storage.DeleteAsync(key, cancellationToken); return Result<bool>.Success(true);
    }
    private async Task<List<StudySetDto>> BuildDtosAsync(IReadOnlyList<StudySet> sets, CancellationToken cancellationToken)
    {
        var ids = sets.Select(x => x.Id).ToList(); var links = await dbContext.StudySetSources.AsNoTracking().Where(x => ids.Contains(x.CombinedStudySetId)).OrderBy(x => x.OrderIndex).ToListAsync(cancellationToken); var sourceIds = links.Select(x => x.SourceStudySetId).Distinct().ToList(); var sourceSets = await dbContext.StudySets.AsNoTracking().Where(x => sourceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken); var directCounts = await dbContext.Flashcards.AsNoTracking().Where(x => ids.Contains(x.StudySetId) || sourceIds.Contains(x.StudySetId)).GroupBy(x => x.StudySetId).Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
        return sets.Select(set => { var sources = links.Where(x => x.CombinedStudySetId == set.Id).Select(x => new StudySetSourceDto(x.SourceStudySetId, sourceSets[x.SourceStudySetId].Title, x.OrderIndex)).ToList(); var count = set.Type == StudySetType.Standard ? directCounts.GetValueOrDefault(set.Id) : sources.Sum(x => directCounts.GetValueOrDefault(x.Id)); return ToDto(set, sources, count); }).ToList();
    }
    private async Task<List<StudySet>?> LoadValidSourcesAsync(Guid userId, IReadOnlyList<Guid> ids, Guid? subjectId, CancellationToken cancellationToken) { var values = await dbContext.StudySets.AsNoTracking().Where(x => ids.Contains(x.Id) && x.Type == StudySetType.Standard && (!subjectId.HasValue || x.SubjectId == subjectId) && dbContext.Subjects.Any(s => s.Id == x.SubjectId && s.UserId == userId)).ToListAsync(cancellationToken); if (values.Count != ids.Count) return null; var map = values.ToDictionary(x => x.Id); return ids.Select(x => map[x]).ToList(); }
    private Task<int> CountCardsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) { var values = ids.ToList(); return dbContext.Flashcards.CountAsync(x => values.Contains(x.StudySetId), cancellationToken); }
    private Task<bool> OwnsSubjectAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken) => dbContext.Subjects.AnyAsync(x => x.Id == subjectId && x.UserId == userId, cancellationToken);
    private Task<StudySet?> FindOwnedAsync(Guid userId, Guid id, bool readOnly, CancellationToken cancellationToken) { IQueryable<StudySet> query = dbContext.StudySets; if (readOnly) query = query.AsNoTracking(); return query.SingleOrDefaultAsync(x => x.Id == id && dbContext.Subjects.Any(subject => subject.Id == x.SubjectId && subject.UserId == userId), cancellationToken); }
    private static StudySetDto ToDto(StudySet value, IReadOnlyList<StudySetSourceDto> sources, int totalCards) => new(value.Id, value.SubjectId, value.Title, value.Description, value.Type.ToString(), sources, totalCards, value.CreatedAt, value.UpdatedAt);
    private static FlashcardDto ToFlashcardDto(Flashcard card) => new(card.Id, card.StudySetId, card.FrontText, card.BackText, card.Explanation, card.LanguageCode, card.ReadingText, card.Romanization, card.ExampleText, card.ExampleTranslation, card.MemoryTip, card.AcceptedAnswers, card.EnableReverseRecall, card.ImageStorageKey is null ? null : $"/api/flashcards/{card.Id}/image", card.OrderIndex, card.CreatedAt, card.UpdatedAt);
    private sealed record StudyTogetherCandidate(Flashcard Card, FlashcardProgress? Progress);
    private static Result<StudySetDto> NotFound(string code = "STUDY_SET_NOT_FOUND", string message = "Study set was not found.") => Result<StudySetDto>.Failure(code, message, ErrorType.NotFound);
}
