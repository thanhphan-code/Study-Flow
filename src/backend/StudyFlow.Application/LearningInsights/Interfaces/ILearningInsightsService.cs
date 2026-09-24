using StudyFlow.Application.Common.Models;
using StudyFlow.Application.LearningInsights.DTOs;

namespace StudyFlow.Application.LearningInsights.Interfaces;

public interface ILearningInsightsService
{
    Task<ProgressOverviewDto> GetOverviewAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<StudySetProgressDto>> GetStudySetProgressAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WeakCardDto>> GetWeakCardsAsync(Guid userId, CancellationToken cancellationToken);
}
