using StudyFlow.Application.Dashboard.DTOs;

namespace StudyFlow.Application.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(Guid userId, CancellationToken cancellationToken);
}
