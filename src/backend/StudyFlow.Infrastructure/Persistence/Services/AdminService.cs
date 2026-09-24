using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudyFlow.Application.Admin;
using StudyFlow.Application.Common.Models;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;
using StudyFlow.Infrastructure.Authentication;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class AdminService(StudyFlowDbContext db, TimeProvider clock, IOptions<AdminOptions> adminOptions) : IAdminService
{
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromMinutes(15);

    public async Task<AdminOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        return new AdminOverviewDto(
            await db.Users.CountAsync(cancellationToken),
            await db.Users.CountAsync(x => !x.IsSuspended && x.LastActiveAt >= now.Subtract(ActiveWindow), cancellationToken),
            await db.Users.CountAsync(x => x.IsSuspended, cancellationToken),
            await db.Users.CountAsync(x => !x.IsEmailVerified, cancellationToken),
            await db.Users.CountAsync(x => x.Role == UserRole.Admin, cancellationToken),
            await db.Users.CountAsync(x => x.CreatedAt >= today, cancellationToken),
            await db.Reports.CountAsync(x => x.Status == "Pending", cancellationToken),
            await db.Documents.CountAsync(x => x.ProcessingStatus == DocumentProcessingStatus.Failed, cancellationToken),
            await db.StudySessions.CountAsync(x => x.StartedAt >= today, cancellationToken));
    }

    public async Task<AdminPagedResult<AdminUserListItemDto>> GetUsersAsync(string? search, string status, string role, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);
        var now = clock.GetUtcNow();
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Email.ToLower().Contains(term) || x.DisplayName.ToLower().Contains(term));
        }
        query = status.ToLowerInvariant() switch
        {
            "active" => query.Where(x => !x.IsSuspended && x.LastActiveAt >= now.Subtract(ActiveWindow)),
            "offline" => query.Where(x => !x.IsSuspended && (x.LastActiveAt == null || x.LastActiveAt < now.Subtract(ActiveWindow))),
            "suspended" => query.Where(x => x.IsSuspended),
            "unverified" => query.Where(x => !x.IsEmailVerified),
            _ => query
        };
        if (Enum.TryParse<UserRole>(role, true, out var parsedRole)) query = query.Where(x => x.Role == parsedRole);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.LastActiveAt).ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { User = x, Username = db.UserProfiles.Where(p => p.UserId == x.Id).Select(p => p.Username).FirstOrDefault() })
            .ToListAsync(cancellationToken);
        var items = rows.Select(x => ToListItem(x.User, x.Username)).ToList();
        return new(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<Result<AdminUserDetailsDto>> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await db.Users.AsNoTracking().Where(x => x.Id == userId)
            .Select(x => new { User = x, Username = db.UserProfiles.Where(p => p.UserId == x.Id).Select(p => p.Username).FirstOrDefault() })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null) return Result<AdminUserDetailsDto>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng.", ErrorType.NotFound);
        var audit = await AuditQuery().Where(x => x.Log.TargetUserId == userId).Take(20).ToListAsync(cancellationToken);
        var details = new AdminUserDetailsDto(
            ToListItem(row.User, row.Username),
            await db.Subjects.CountAsync(x => x.UserId == userId, cancellationToken),
            await db.StudySets.CountAsync(x => db.Subjects.Any(subject => subject.Id == x.SubjectId && subject.UserId == userId), cancellationToken),
            await db.StudySessions.CountAsync(x => x.UserId == userId, cancellationToken),
            await db.Documents.CountAsync(x => x.UserId == userId, cancellationToken),
            await db.BattleParticipants.CountAsync(x => x.UserId == userId, cancellationToken),
            audit.Select(ToAuditItem).ToList());
        return Result<AdminUserDetailsDto>.Success(details);
    }

    public async Task<Result<AdminUserListItemDto>> UpdateStatusAsync(Guid actorUserId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        if (actorUserId == userId) return Protected<AdminUserListItemDto>("Bạn không thể thay đổi trạng thái tài khoản đang dùng.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Missing<AdminUserListItemDto>();
        if (IsBootstrap(user)) return Protected<AdminUserListItemDto>("Không thể tạm ngưng admin gốc được cấu hình trên máy chủ.");
        if (request.Suspended) user.Suspend(request.Reason, clock.GetUtcNow()); else user.Activate(clock.GetUtcNow());
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (profile is not null) profile.IsSuspended = request.Suspended;
        AddAudit(actorUserId, userId, request.Suspended ? "USER_SUSPENDED" : "USER_ACTIVATED", request.Reason, new { request.Suspended });
        await db.SaveChangesAsync(cancellationToken);
        return Result<AdminUserListItemDto>.Success(ToListItem(user, profile?.Username));
    }

    public async Task<Result<AdminUserListItemDto>> UpdateRoleAsync(Guid actorUserId, Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (actorUserId == userId) return Protected<AdminUserListItemDto>("Bạn không thể tự thay đổi vai trò của mình.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Missing<AdminUserListItemDto>();
        if (IsBootstrap(user) && request.Role != UserRole.Admin) return Protected<AdminUserListItemDto>("Không thể hạ quyền admin gốc được cấu hình trên máy chủ.");
        var previous = user.Role;
        user.ChangeRole(request.Role, clock.GetUtcNow());
        AddAudit(actorUserId, userId, "USER_ROLE_CHANGED", request.Reason, new { PreviousRole = previous.ToString(), NewRole = request.Role.ToString() });
        await db.SaveChangesAsync(cancellationToken);
        var username = await db.UserProfiles.Where(x => x.UserId == userId).Select(x => x.Username).SingleOrDefaultAsync(cancellationToken);
        return Result<AdminUserListItemDto>.Success(ToListItem(user, username));
    }

    public async Task<Result<bool>> RevokeSessionsAsync(Guid actorUserId, Guid userId, RevokeUserSessionsRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return Missing<bool>();
        user.RevokeAllSessions(clock.GetUtcNow());
        AddAudit(actorUserId, userId, "USER_SESSIONS_REVOKED", request.Reason, new { });
        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<AdminPagedResult<AdminAuditItemDto>> GetAuditAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);
        var total = await db.AdminAuditLogs.CountAsync(cancellationToken);
        var rows = await AuditQuery().Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new(rows.Select(ToAuditItem).ToList(), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    private IQueryable<AuditProjection> AuditQuery() =>
        from log in db.AdminAuditLogs.AsNoTracking()
        join actor in db.Users.AsNoTracking() on log.ActorUserId equals actor.Id
        join target in db.Users.AsNoTracking() on log.TargetUserId equals target.Id
        orderby log.CreatedAt descending
        select new AuditProjection(log, actor.DisplayName, target.DisplayName);

    private void AddAudit(Guid actor, Guid target, string action, string reason, object metadata) =>
        db.AdminAuditLogs.Add(AdminAuditLog.Create(actor, target, action, reason, JsonSerializer.Serialize(metadata)));

    private bool IsBootstrap(User user) => adminOptions.Value.IsBootstrapAdmin(user.Email);
    private static AdminUserListItemDto ToListItem(User user, string? username) => new(user.Id, user.Email, user.DisplayName, username, user.Role, user.IsEmailVerified, user.IsSuspended, user.SuspensionReason, user.LastActiveAt, user.CreatedAt);
    private static AdminAuditItemDto ToAuditItem(AuditProjection x) => new(x.Log.Id, x.Log.ActorUserId, x.ActorName, x.Log.TargetUserId, x.TargetName, x.Log.Action, x.Log.Reason, x.Log.CreatedAt);
    private static Result<T> Missing<T>() => Result<T>.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng.", ErrorType.NotFound);
    private static Result<T> Protected<T>(string message) => Result<T>.Failure("ADMIN_ACCOUNT_PROTECTED", message, ErrorType.Conflict);
    private sealed record AuditProjection(AdminAuditLog Log, string ActorName, string TargetName);
}
