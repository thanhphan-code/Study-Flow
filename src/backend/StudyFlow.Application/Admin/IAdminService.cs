using StudyFlow.Application.Common.Models;

namespace StudyFlow.Application.Admin;

public interface IAdminService
{
    Task<AdminOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);
    Task<AdminPagedResult<AdminUserListItemDto>> GetUsersAsync(string? search, string status, string role, int page, int pageSize, CancellationToken cancellationToken);
    Task<Result<AdminUserDetailsDto>> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<AdminUserListItemDto>> UpdateStatusAsync(Guid actorUserId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken);
    Task<Result<AdminUserListItemDto>> UpdateRoleAsync(Guid actorUserId, Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> RevokeSessionsAsync(Guid actorUserId, Guid userId, RevokeUserSessionsRequest request, CancellationToken cancellationToken);
    Task<AdminPagedResult<AdminAuditItemDto>> GetAuditAsync(int page, int pageSize, CancellationToken cancellationToken);
}
