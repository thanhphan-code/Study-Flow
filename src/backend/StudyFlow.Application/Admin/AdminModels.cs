using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.Admin;

public sealed record AdminOverviewDto(
    int TotalUsers,
    int ActiveUsers,
    int SuspendedUsers,
    int UnverifiedUsers,
    int AdminUsers,
    int NewUsersToday,
    int PendingReports,
    int FailedDocuments,
    int StudySessionsToday);

public sealed record AdminUserListItemDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? Username,
    UserRole Role,
    bool IsEmailVerified,
    bool IsSuspended,
    string? SuspensionReason,
    DateTimeOffset? LastActiveAt,
    DateTimeOffset CreatedAt);

public sealed record AdminUserDetailsDto(
    AdminUserListItemDto User,
    int Subjects,
    int StudySets,
    int StudySessions,
    int Documents,
    int BattleParticipations,
    IReadOnlyList<AdminAuditItemDto> RecentAudit);

public sealed record AdminAuditItemDto(
    Guid Id,
    Guid ActorUserId,
    string ActorDisplayName,
    Guid TargetUserId,
    string TargetDisplayName,
    string Action,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record AdminPagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record UpdateUserStatusRequest(bool Suspended, string Reason);
public sealed record UpdateUserRoleRequest(UserRole Role, string Reason);
public sealed record RevokeUserSessionsRequest(string Reason);
