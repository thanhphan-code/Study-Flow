using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Flashcards.DTOs;

namespace StudyFlow.Application.Flashcards.Interfaces;

public interface IFlashcardService
{
    Task<Result<IReadOnlyList<FlashcardDto>>> ListAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken);
    Task<Result<FlashcardDto>> CreateAsync(Guid userId, Guid studySetId, CreateFlashcardRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<FlashcardDto>>> BulkCreateAsync(Guid userId, Guid studySetId, BulkCreateFlashcardsRequest request, CancellationToken cancellationToken);
    Task<Result<FlashcardDto>> UpdateAsync(Guid userId, Guid flashcardId, UpdateFlashcardRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid flashcardId, CancellationToken cancellationToken);
    Task<Result<FlashcardDto>> UploadImageAsync(Guid userId, Guid flashcardId, FlashcardImageUpload upload, CancellationToken cancellationToken);
    Task<Result<FlashcardImageContent>> GetImageAsync(Guid userId, Guid flashcardId, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteImageAsync(Guid userId, Guid flashcardId, CancellationToken cancellationToken);
}
