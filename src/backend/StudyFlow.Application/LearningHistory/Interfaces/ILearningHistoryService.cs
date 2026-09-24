using StudyFlow.Application.LearningHistory.DTOs;

namespace StudyFlow.Application.LearningHistory.Interfaces;

public interface ILearningHistoryService
{
    Task<StreakDto> GetStreakAsync(Guid userId, CancellationToken cancellationToken);
    Task<LearningHistoryDto> GetHistoryAsync(Guid userId, int days, CancellationToken cancellationToken);
}
