using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Social;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed partial class SocialService
{
    public async Task<object> NotificationsAsync(Guid userId, int page, CancellationToken ct)
    {
        var blocked = BlockedIds(userId);
        var q = db.Notifications.AsNoTracking().Where(x => x.UserId == userId && !blocked.Contains(x.ActorId));
        var rows = await (from n in q join p in db.UserProfiles on n.ActorId equals p.UserId where !p.IsSuspended orderby n.CreatedAt descending, n.Id select new { n.Id, n.Kind, n.EntityId, n.IsRead, n.CreatedAt, Actor = new { p.UserId, p.Username, p.DisplayName, p.AvatarUrl } }).Skip(Skip(page)).Take(21).ToListAsync(ct);
        return new { Items = rows.Take(20), HasMore = rows.Count > 20, Page = page, Unread = await q.CountAsync(x => !x.IsRead, ct) };
    }
    public async Task<object> ReadNotificationsAsync(Guid userId, Guid? id, CancellationToken ct)
    {
        foreach (var item in await db.Notifications.Where(x => x.UserId == userId && !x.IsRead && (id == null || x.Id == id)).ToListAsync(ct)) item.IsRead = true;
        return new { Success = true };
    }
    private async Task<DirectConversation> Conversation(Guid user, Guid id, CancellationToken ct)
    {
        var conversation = await db.Conversations.SingleOrDefaultAsync(x => x.Id == id && (x.UserAId == user || x.UserBId == user), ct) ?? throw Missing();
        var other = conversation.UserAId == user ? conversation.UserBId : conversation.UserAId;
        await EnsurePerson(user, other, ct);
        if (!await Friends(user, other, ct)) throw new SocialException(403, "Chỉ bạn bè mới có thể nhắn tin.");
        return conversation;
    }
    public async Task<object> ConversationsAsync(Guid userId, int page, CancellationToken ct)
    {
        var blocked = BlockedIds(userId);
        var rows = await (from c in db.Conversations.AsNoTracking()
            join p in db.UserProfiles on (c.UserAId == userId ? c.UserBId : c.UserAId) equals p.UserId
            where (c.UserAId == userId || c.UserBId == userId) && !blocked.Contains(p.UserId) && !p.IsSuspended && db.Relationships.Any(r => r.UserId == userId && r.TargetUserId == p.UserId && r.Kind == "Friend")
            orderby c.UpdatedAt descending, c.CreatedAt descending
            select new { c.Id, c.UpdatedAt, Person = new { p.UserId, p.Username, p.DisplayName, p.AvatarUrl },
                Unread = db.Messages.Count(m => m.ConversationId == c.Id && m.SenderId != userId && !m.IsDeleted && (c.UserAId == userId ? c.UserAReadAt == null || m.CreatedAt > c.UserAReadAt : c.UserBReadAt == null || m.CreatedAt > c.UserBReadAt)) }).Skip(Skip(page)).Take(21).ToListAsync(ct);
        return Page(rows, page);
    }
    public async Task<object> DirectAsync(Guid userId, Guid targetId, CancellationToken ct)
    {
        await EnsurePerson(userId, targetId, ct);
        if (!await Friends(userId, targetId, ct)) throw new SocialException(403, "Hãy kết bạn trước khi nhắn tin.");
        var a = userId.CompareTo(targetId) < 0 ? userId : targetId; var b = a == userId ? targetId : userId;
        var c = await db.Conversations.SingleOrDefaultAsync(x => x.UserAId == a && x.UserBId == b, ct);
        if (c is null) { c = new DirectConversation { UserAId = a, UserBId = b }; db.Conversations.Add(c); }
        return new { c.Id };
    }
    public async Task<object> MessagesAsync(Guid userId, Guid id, int page, CancellationToken ct)
    {
        var c = await Conversation(userId, id, ct);
        var other = c.UserAId == userId ? c.UserBId : c.UserAId;
        var rows = await db.Messages.AsNoTracking().Where(x => x.ConversationId == id).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip(Skip(page)).Take(21).ToListAsync(ct);
        var result = new List<object>();
        foreach (var m in rows.Take(20))
        {
            object? shared = null;
            if (!m.IsDeleted && m.SharedId is not null)
            {
                try
                {
                    if (m.Kind == "StudySetShare" || m.Kind == "FlashcardShare")
                    {
                        var setId = m.Kind == "StudySetShare" ? m.SharedId.Value : await AccessibleCardSet(userId, m.SharedId.Value, ct);
                        var (set, _, _) = await AccessibleSet(userId, setId, ct);
                        var title = m.Kind == "FlashcardShare" ? await db.Flashcards.Where(x => x.Id == m.SharedId).Select(x => x.FrontText).SingleAsync(ct) : set.Title;
                        shared = new { Title = title, Url = $"/community/sets/{set.Id}" + (m.Kind == "FlashcardShare" ? $"#card-{m.SharedId}" : ""), Available = true };
                    }
                    else if (m.Kind == "BattleInvite")
                    {
                        var room = await db.BattleRooms.AsNoTracking().SingleOrDefaultAsync(x => x.Id == m.SharedId, ct);
                        if (room is not null && room.Status == "LobbyOpen" && room.ExpiresAt > Now && !await Blocked(userId, room.HostUserId, ct)) shared = new { Title = room.Name, Url = $"/join/{room.JoinCode}", Available = true };
                    }
                }
                catch (SocialException) { }
            }
            result.Add(new { m.Id, m.SenderId, m.Kind, Content = m.IsDeleted ? "Tin nhắn đã xóa." : m.Content, m.CreatedAt, Shared = shared, IsOwn = m.SenderId == userId, IsRead = m.SenderId == userId && (c.UserAId == other ? c.UserAReadAt : c.UserBReadAt) >= m.CreatedAt });
        }
        return new { Items = result, HasMore = rows.Count > 20, Page = page };
    }
    public async Task<object> SendAsync(Guid userId, Guid id, MessageRequest request, CancellationToken ct)
    {
        var c = await Conversation(userId, id, ct); var other = c.UserAId == userId ? c.UserBId : c.UserAId;
        Require(new[] { "Text", "StudySetShare", "FlashcardShare", "BattleInvite" }.Contains(request.Kind), "Loại tin nhắn không hợp lệ.");
        var content = Clean(request.Content, 4000, request.Kind == "Text");
        if (request.Kind != "Text")
        {
            Require(request.SharedId is not null, "Chưa chọn nội dung chia sẻ.");
            if (request.Kind == "BattleInvite")
            {
                var room = await db.BattleRooms.SingleOrDefaultAsync(x => x.Id == request.SharedId && x.HostUserId == userId && x.Status == "LobbyOpen" && x.ExpiresAt > Now, ct);
                if (room is null) throw Missing();
                if (await db.BattleParticipants.AnyAsync(x => x.BattleRoomId == room.Id && x.UserId == other && x.Status == "Kicked", ct)) throw Missing();
            }
            else
            {
                var setId = request.Kind == "StudySetShare" ? request.SharedId!.Value : await AccessibleCardSet(userId, request.SharedId!.Value, ct, other);
                await AccessibleSet(userId, setId, ct); await AccessibleSet(other, setId, ct);
            }
        }
        var message = new DirectMessage { ConversationId = id, SenderId = userId, Kind = request.Kind, Content = content, SharedId = request.Kind == "Text" ? null : request.SharedId };
        db.Messages.Add(message); c.Touch(Now);
        await Notify(other, userId, request.Kind == "BattleInvite" ? "BattleInvite" : "Message", id, ct);
        var notification = await db.Notifications.FirstOrDefaultAsync(x => x.UserId == other && x.ActorId == userId && x.Kind == "Message" && x.EntityId == id, ct);
        if (notification is not null) { notification.IsRead = false; notification.Touch(Now); }
        changedUsers.Add(other); return new { message.Id };
    }
    public async Task<object> ReadConversationAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var c = await Conversation(userId, id, ct); if (c.UserAId == userId) c.UserAReadAt = Now; else c.UserBReadAt = Now;
        changedUsers.Add(c.UserAId == userId ? c.UserBId : c.UserAId); return new { Success = true };
    }
    public async Task<object> ReportAsync(Guid userId, ReportRequest request, CancellationToken ct)
    {
        Require(new[] { "Spam", "Harassment", "InappropriateContent", "Copyright", "Impersonation", "Other" }.Contains(request.Reason), "Lý do báo cáo không hợp lệ.");
        switch (request.TargetType)
        {
            case "User": if (!await db.Users.AnyAsync(x => x.Id == request.TargetId, ct)) throw Missing(); break;
            case "StudySet": await AccessibleSet(userId, request.TargetId, ct); break;
            case "Comment": var c = await db.Comments.SingleOrDefaultAsync(x => x.Id == request.TargetId, ct) ?? throw Missing(); await AccessibleSet(userId, c.StudySetId, ct); break;
            default: throw new SocialException(400, "Đối tượng báo cáo không hợp lệ.");
        }
        if (!await db.Reports.AnyAsync(x => x.UserId == userId && x.TargetId == request.TargetId && x.TargetType == request.TargetType && x.Status == "Pending", ct)) db.Reports.Add(new ContentReport { UserId = userId, TargetId = request.TargetId, TargetType = request.TargetType, Reason = request.Reason, Details = Clean(request.Details, 2000) });
        return new { Success = true };
    }
    public async Task<object> ReportsAsync(int page, CancellationToken ct) => Page(await db.Reports.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip(Skip(page)).Take(21).Select(x => new {
        x.Id, x.TargetId, x.TargetType, x.Reason, x.Details, x.Status, x.CreatedAt,
        Summary = x.TargetType == "StudySet" ? db.StudySets.Where(s => s.Id == x.TargetId).Select(s => s.Title).FirstOrDefault()
            : x.TargetType == "Comment" ? db.Comments.Where(c => c.Id == x.TargetId).Select(c => c.Content).FirstOrDefault()
            : db.UserProfiles.Where(p => p.UserId == x.TargetId).Select(p => p.Username + " · " + p.DisplayName).FirstOrDefault()
    }).ToListAsync(ct), page);
    public async Task<object> ModerateAsync(Guid id, string action, CancellationToken ct)
    {
        var report = await db.Reports.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing();
        Require(new[] { "Dismiss", "Remove", "Suspend", "Restore" }.Contains(action), "Thao tác quản trị không hợp lệ.");
        if (action is "Suspend" or "Restore")
        {
            Require(report.TargetType == "User", "Chỉ áp dụng cho tài khoản."); var profile = await MyProfile(report.TargetId, ct); profile.IsSuspended = action == "Suspend";
        }
        if (action == "Remove")
        {
            Require(report.TargetType is "StudySet" or "Comment", "Chỉ áp dụng cho bộ học hoặc bình luận.");
            if (report.TargetType == "StudySet") { var p = await db.Publications.SingleOrDefaultAsync(x => x.StudySetId == report.TargetId, ct) ?? throw Missing(); p.IsRemoved = true; }
            else { var c = await db.Comments.SingleOrDefaultAsync(x => x.Id == report.TargetId, ct) ?? throw Missing(); c.IsDeleted = true; c.Content = ""; }
        }
        report.Status = action == "Dismiss" ? "Dismissed" : "Resolved"; report.Touch(Now);
        return new { Success = true };
    }
}
