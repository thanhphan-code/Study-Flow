using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Progress.DTOs;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.Progress.Interfaces;

public interface IFlashcardProgressService
{
    Task<Result<FlashcardProgressDto>> ReviewAsync(Guid userId, Guid flashcardId, ReviewFlashcardRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DueFlashcardDto>> GetDueAsync(Guid userId, CancellationToken cancellationToken);
}
