using StudyFlow.Application.DailyStudy.DTOs;

namespace StudyFlow.Application.DailyStudy.Interfaces;

public interface IDailyStudyService
{
    Task<DailyStudyPlanDto> CreatePlanAsync(Guid userId, DailyStudyRequest request, CancellationToken cancellationToken);
}
