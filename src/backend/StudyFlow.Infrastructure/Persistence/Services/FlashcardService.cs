using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.Flashcards.Interfaces;
using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class FlashcardService(StudyFlowDbContext dbContext, IFileStorageService storage) : IFlashcardService
{
    public async Task<Result<IReadOnlyList<FlashcardDto>>> ListAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken)
    {
        var type = await OwnedStudySetTypeAsync(userId, studySetId, cancellationToken); if (type is null) return Result<IReadOnlyList<FlashcardDto>>.Failure("STUDY_SET_NOT_FOUND", "Study set was not found.", ErrorType.NotFound);
        var ids = type == StudySetType.Standard ? new List<Guid> { studySetId } : await dbContext.StudySetSources.Where(x => x.CombinedStudySetId == studySetId).OrderBy(x => x.OrderIndex).Select(x => x.SourceStudySetId).ToListAsync(cancellationToken);
        var cards = (await dbContext.Flashcards.AsNoTracking().Where(x => ids.Contains(x.StudySetId)).ToListAsync(cancellationToken)).OrderBy(x => ids.IndexOf(x.StudySetId)).ThenBy(x => x.OrderIndex).ToList();
        var values = cards.Select(ToDto).ToList();
        return Result<IReadOnlyList<FlashcardDto>>.Success(values);
    }

    public async Task<Result<FlashcardDto>> CreateAsync(Guid userId, Guid studySetId, CreateFlashcardRequest request, CancellationToken cancellationToken)
    {
        var type = await OwnedStudySetTypeAsync(userId, studySetId, cancellationToken); if (type is null) return NotFound("STUDY_SET_NOT_FOUND", "Study set was not found."); if (type == StudySetType.Combined) return Result<FlashcardDto>.Failure("COMBINED_SET_READ_ONLY", "Add cards to a source study set.", ErrorType.Conflict);
        var nextOrder = await NextOrderAsync(studySetId, cancellationToken); var card = Flashcard.Create(studySetId, request.FrontText, request.BackText, request.Explanation, nextOrder, request.LanguageCode, request.ReadingText, request.Romanization, request.ExampleText, request.ExampleTranslation, request.MemoryTip, request.AcceptedAnswers, request.EnableReverseRecall);
        dbContext.Flashcards.Add(card); await dbContext.SaveChangesAsync(cancellationToken); return Result<FlashcardDto>.Success(ToDto(card));
    }

    public async Task<Result<IReadOnlyList<FlashcardDto>>> BulkCreateAsync(Guid userId, Guid studySetId, BulkCreateFlashcardsRequest request, CancellationToken cancellationToken)
    {
        var type = await OwnedStudySetTypeAsync(userId, studySetId, cancellationToken); if (type is null) return Result<IReadOnlyList<FlashcardDto>>.Failure("STUDY_SET_NOT_FOUND", "Study set was not found.", ErrorType.NotFound); if (type == StudySetType.Combined) return Result<IReadOnlyList<FlashcardDto>>.Failure("COMBINED_SET_READ_ONLY", "Add cards to a source study set.", ErrorType.Conflict);
        var nextOrder = await NextOrderAsync(studySetId, cancellationToken);
        var cards = request.Cards.Select((item, offset) => Flashcard.Create(studySetId, item.FrontText, item.BackText, item.Explanation, nextOrder + offset, item.LanguageCode, item.ReadingText, item.Romanization, item.ExampleText, item.ExampleTranslation, item.MemoryTip, item.AcceptedAnswers, item.EnableReverseRecall)).ToList();
        dbContext.Flashcards.AddRange(cards); await dbContext.SaveChangesAsync(cancellationToken); return Result<IReadOnlyList<FlashcardDto>>.Success(cards.Select(ToDto).ToList());
    }

    public async Task<Result<FlashcardDto>> UpdateAsync(Guid userId, Guid flashcardId, UpdateFlashcardRequest request, CancellationToken cancellationToken)
    {
        var card = await FindOwnedAsync(userId, flashcardId, cancellationToken); if (card is null) return NotFound();
        card.Update(request.FrontText, request.BackText, request.Explanation, request.LanguageCode, request.ReadingText, request.Romanization, request.ExampleText, request.ExampleTranslation, request.MemoryTip, request.AcceptedAnswers, request.EnableReverseRecall); await dbContext.SaveChangesAsync(cancellationToken); return Result<FlashcardDto>.Success(ToDto(card));
    }

    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid flashcardId, CancellationToken cancellationToken)
    {
        var card = await FindOwnedAsync(userId, flashcardId, cancellationToken); if (card is null) return Result<bool>.Failure("FLASHCARD_NOT_FOUND", "Flashcard was not found.", ErrorType.NotFound);
        var imageKey = card.ImageStorageKey; dbContext.Flashcards.Remove(card); await dbContext.SaveChangesAsync(cancellationToken); if (imageKey is not null) await storage.DeleteAsync(imageKey, cancellationToken); return Result<bool>.Success(true);
    }

    public async Task<Result<FlashcardDto>> UploadImageAsync(Guid userId, Guid flashcardId, FlashcardImageUpload upload, CancellationToken cancellationToken)
    {
        var card = await FindOwnedAsync(userId, flashcardId, cancellationToken); if (card is null) return NotFound();
        if (upload.Content.Length == 0) return Result<FlashcardDto>.Failure("EMPTY_IMAGE", "The uploaded image is empty.", ErrorType.Validation);
        if (upload.Content.Length > 5 * 1024 * 1024) return Result<FlashcardDto>.Failure("IMAGE_TOO_LARGE", "Image size cannot exceed 5 MB.", ErrorType.Validation);
        var extension = Path.GetExtension(upload.OriginalFileName).ToLowerInvariant();
        if (!ImageTypes.TryGetValue(extension, out var expectedType) || !string.Equals(expectedType, upload.ContentType, StringComparison.OrdinalIgnoreCase) || !HasValidImageSignature(upload.Content, expectedType)) return Result<FlashcardDto>.Failure("INVALID_IMAGE", "Only valid JPG, PNG, and WEBP images are supported.", ErrorType.Validation);
        var oldKey = card.ImageStorageKey; var key = $"flashcards/{userId:N}/{flashcardId:N}/{Guid.NewGuid():N}{extension}"; var fileName = SanitizeName(upload.OriginalFileName);
        await storage.SaveAsync(key, upload.Content, cancellationToken);
        try { card.SetImage(key, expectedType, fileName); await dbContext.SaveChangesAsync(cancellationToken); }
        catch { await storage.DeleteAsync(key, cancellationToken); throw; }
        if (oldKey is not null) await storage.DeleteAsync(oldKey, cancellationToken);
        return Result<FlashcardDto>.Success(ToDto(card));
    }

    public async Task<Result<FlashcardImageContent>> GetImageAsync(Guid userId, Guid flashcardId, CancellationToken cancellationToken)
    {
        var card = await FindOwnedAsync(userId, flashcardId, cancellationToken); if (card is null) return Result<FlashcardImageContent>.Failure("FLASHCARD_NOT_FOUND", "Flashcard was not found.", ErrorType.NotFound);
        if (card.ImageStorageKey is null || card.ImageContentType is null) return Result<FlashcardImageContent>.Failure("IMAGE_NOT_FOUND", "Flashcard image was not found.", ErrorType.NotFound);
        return Result<FlashcardImageContent>.Success(new(await storage.ReadAsync(card.ImageStorageKey, cancellationToken), card.ImageContentType, card.ImageFileName ?? "flashcard-image"));
    }

    public async Task<Result<bool>> DeleteImageAsync(Guid userId, Guid flashcardId, CancellationToken cancellationToken)
    {
        var card = await FindOwnedAsync(userId, flashcardId, cancellationToken); if (card is null) return Result<bool>.Failure("FLASHCARD_NOT_FOUND", "Flashcard was not found.", ErrorType.NotFound);
        var key = card.ImageStorageKey; if (key is null) return Result<bool>.Success(true);
        card.RemoveImage(); await dbContext.SaveChangesAsync(cancellationToken); await storage.DeleteAsync(key, cancellationToken); return Result<bool>.Success(true);
    }

    private Task<bool> OwnsStudySetAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken) => dbContext.StudySets.AnyAsync(set => set.Id == studySetId && dbContext.Subjects.Any(subject => subject.Id == set.SubjectId && subject.UserId == userId), cancellationToken);
    private Task<StudySetType?> OwnedStudySetTypeAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken) => dbContext.StudySets.Where(set => set.Id == studySetId && dbContext.Subjects.Any(subject => subject.Id == set.SubjectId && subject.UserId == userId)).Select(x => (StudySetType?)x.Type).SingleOrDefaultAsync(cancellationToken);
    private Task<Flashcard?> FindOwnedAsync(Guid userId, Guid flashcardId, CancellationToken cancellationToken) => dbContext.Flashcards.SingleOrDefaultAsync(card => card.Id == flashcardId && dbContext.StudySets.Any(set => set.Id == card.StudySetId && dbContext.Subjects.Any(subject => subject.Id == set.SubjectId && subject.UserId == userId)), cancellationToken);
    private async Task<int> NextOrderAsync(Guid studySetId, CancellationToken cancellationToken) => (await dbContext.Flashcards.Where(x => x.StudySetId == studySetId).MaxAsync(x => (int?)x.OrderIndex, cancellationToken) ?? -1) + 1;
    private static FlashcardDto ToDto(Flashcard card) => new(card.Id, card.StudySetId, card.FrontText, card.BackText, card.Explanation, card.LanguageCode, card.ReadingText, card.Romanization, card.ExampleText, card.ExampleTranslation, card.MemoryTip, card.AcceptedAnswers, card.EnableReverseRecall, card.ImageStorageKey is null ? null : $"/api/flashcards/{card.Id}/image", card.OrderIndex, card.CreatedAt, card.UpdatedAt);
    private static Result<FlashcardDto> NotFound(string code = "FLASHCARD_NOT_FOUND", string message = "Flashcard was not found.") => Result<FlashcardDto>.Failure(code, message, ErrorType.NotFound);
    private static string SanitizeName(string value) { var name = Path.GetFileName(value.Trim()); foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_'); return string.IsNullOrWhiteSpace(name) ? "flashcard-image" : name[..Math.Min(name.Length, 255)]; }
    private static bool HasValidImageSignature(byte[] content, string contentType) => contentType switch { "image/jpeg" => content.Length >= 3 && content[0] == 0xff && content[1] == 0xd8 && content[2] == 0xff, "image/png" => content.Length >= 8 && content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), "image/webp" => content.Length >= 12 && content.AsSpan(0, 4).SequenceEqual("RIFF"u8) && content.AsSpan(8, 4).SequenceEqual("WEBP"u8), _ => false };
    private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase) { [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp" };
}
