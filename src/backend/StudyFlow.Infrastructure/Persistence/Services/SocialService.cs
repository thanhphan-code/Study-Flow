using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Social;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed partial class SocialService(StudyFlowDbContext db, ISocialNotifier notifier, TimeProvider clock,
    StudyFlow.Application.Documents.Interfaces.IFileStorageService storage,
    StudyFlow.Application.LearningEngine.Interfaces.IAnswerEvaluator evaluator) : ISocialService
{
    private static readonly SemaphoreSlim WriteGate = new(1, 1);
    private readonly HashSet<Guid> changedUsers = [];
    private readonly List<string> createdFiles = [];
    private DateTimeOffset Now => clock.GetUtcNow();
    private static int Skip(int page) => (Math.Clamp(page, 1, 10000) - 1) * 20;
    private static object Page<T>(List<T> rows, int page) => new { Items = rows.Take(20), Page = Math.Clamp(page, 1, 10000), HasMore = rows.Count > 20 };
    private static SocialException Missing() => new(404, "Nội dung không tồn tại hoặc bạn không có quyền truy cập.");
    private static void Require(bool ok, string message) { if (!ok) throw new SocialException(400, message); }
    private static string Clean(string? value, int max, bool required = false)
    {
        var text = value?.Trim() ?? "";
        Require(text.Length <= max && (!required || text.Length > 0), $"Nội dung cần có {(required ? "1" : "0")}–{max} ký tự.");
        return text;
    }
    private IQueryable<Guid> BlockedIds(Guid user) => db.Relationships.Where(x => x.Kind == "Block" && (x.UserId == user || x.TargetUserId == user)).Select(x => x.UserId == user ? x.TargetUserId : x.UserId);
    private Task<bool> Blocked(Guid a, Guid b, CancellationToken ct) => db.Relationships.AnyAsync(x => x.Kind == "Block" && ((x.UserId == a && x.TargetUserId == b) || (x.UserId == b && x.TargetUserId == a)), ct);
    private Task<bool> Friends(Guid a, Guid b, CancellationToken ct) => db.Relationships.AnyAsync(x => x.Kind == "Friend" && x.UserId == a && x.TargetUserId == b, ct);
    private async Task EnsurePerson(Guid user, Guid target, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.Id == target, ct) || await Blocked(user, target, ct) || await db.UserProfiles.AnyAsync(x => x.UserId == target && x.IsSuspended, ct)) throw Missing();
    }
    public async Task<object> WriteAsync(Guid userId, Func<Task<object>> action, CancellationToken ct)
    {
        await WriteGate.WaitAsync(ct);
        try
        {
            await using var tx = db.Database.ProviderName?.Contains("Npgsql") == true ? await db.Database.BeginTransactionAsync(ct) : null;
            // Serializes social mutations across instances: publication changes, blocks, shares and reactions cannot race.
            if (tx is not null) await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(73104291)", ct);
            if (await db.UserProfiles.AnyAsync(x => x.UserId == userId && x.IsSuspended, ct)) throw new SocialException(403, "Tài khoản đã bị tạm ngưng.");
            var result = await action();
            await db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);
            foreach (var user in changedUsers.Append(userId).Distinct()) await notifier.ChangedAsync(user, ct);
            return result;
        }
        catch
        {
            foreach (var key in createdFiles) await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }
        finally { WriteGate.Release(); }
    }
    private async Task Notify(Guid recipient, Guid actor, string kind, Guid entity, CancellationToken ct)
    {
        if (recipient == actor || await Blocked(recipient, actor, ct)) return;
        if (!await db.Notifications.AnyAsync(x => x.UserId == recipient && x.ActorId == actor && x.Kind == kind && x.EntityId == entity, ct))
            db.Notifications.Add(new SocialNotification { UserId = recipient, ActorId = actor, Kind = kind, EntityId = entity });
        changedUsers.Add(recipient);
    }
    private async Task<UserProfile> MyProfile(Guid user, CancellationToken ct)
    {
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == user, ct);
        if (profile is not null) return profile;
        var account = await db.Users.SingleAsync(x => x.Id == user, ct);
        profile = new UserProfile { UserId = user, Username = "u_" + user.ToString("N")[..28], DisplayName = account.DisplayName };
        db.UserProfiles.Add(profile);
        return profile;
    }
    public async Task<object> ProfileAsync(Guid userId, string username, CancellationToken ct)
    {
        var profile = username == "me" ? await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct)
            : await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Username == username.ToLowerInvariant(), ct);
        if (profile is null && username == "me")
        {
            var account = await db.Users.SingleAsync(x => x.Id == userId, ct);
            profile = new UserProfile { UserId = userId, Username = "u_" + userId.ToString("N")[..28], DisplayName = account.DisplayName };
        }
        if (profile is null) throw Missing();
        await EnsurePerson(userId, profile.UserId, ct);
        var own = profile.UserId == userId;
        var friend = await Friends(userId, profile.UserId, ct);
        var visible = own || profile.Visibility == "Public" || (friend && profile.Visibility == "FriendsOnly");
        var publicSetIds = from set in db.StudySets join subject in db.Subjects on set.SubjectId equals subject.Id
            where subject.UserId == profile.UserId && db.Publications.Any(p => p.StudySetId == set.Id && p.Visibility == "Public" && !p.IsRemoved) select set.Id;
        return new {
            profile.UserId, profile.Username, profile.DisplayName, profile.AvatarUrl, profile.Visibility,
            Bio = visible ? profile.Bio : "", IsOwn = own, IsRestricted = !visible, IsFriend = friend,
            IsFollowing = await db.Relationships.AnyAsync(x => x.UserId == userId && x.TargetUserId == profile.UserId && x.Kind == "Follow", ct),
            OutgoingRequest = await db.Relationships.AnyAsync(x => x.UserId == userId && x.TargetUserId == profile.UserId && x.Kind == "Request" && x.Status == "Pending", ct),
            IncomingRequest = await db.Relationships.AnyAsync(x => x.TargetUserId == userId && x.UserId == profile.UserId && x.Kind == "Request" && x.Status == "Pending", ct),
            Followers = visible ? await db.Relationships.CountAsync(x => x.TargetUserId == profile.UserId && x.Kind == "Follow", ct) : 0,
            Following = visible ? await db.Relationships.CountAsync(x => x.UserId == profile.UserId && x.Kind == "Follow", ct) : 0,
            Friends = visible ? await db.Relationships.CountAsync(x => x.UserId == profile.UserId && x.Kind == "Friend", ct) : 0,
            PublicSets = visible ? await publicSetIds.CountAsync(ct) : 0,
            PublicCards = visible ? await db.Flashcards.CountAsync(c => publicSetIds.Contains(c.StudySetId), ct) : 0,
            profile.ShowStreak, profile.ShowBattleHistory, profile.ShowActivityStatus
        };
    }
    public async Task<object> UpdateProfileAsync(Guid userId, ProfileRequest request, CancellationToken ct)
    {
        var name = Clean(request.Username, 30, true).ToLowerInvariant();
        Require(Regex.IsMatch(name, "^[a-z0-9_]{3,30}$", RegexOptions.CultureInvariant) && !new[] { "admin", "administrator", "support", "studyflow", "official", "moderator", "me" }.Contains(name) && !name.StartsWith("u_"), "Tên người dùng cần 3–30 chữ cái, số hoặc dấu gạch dưới; không dùng tên dành riêng.");
        if (await db.UserProfiles.AnyAsync(x => x.Username == name && x.UserId != userId, ct)) throw new SocialException(409, "Tên người dùng đã được sử dụng.");
        Require(new[] { "Public", "FriendsOnly", "Private" }.Contains(request.Visibility), "Quyền riêng tư không hợp lệ.");
        var avatar = Clean(request.AvatarUrl, 1000);
        Require(avatar.Length == 0 || (Uri.TryCreate(avatar, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo)), "Ảnh đại diện cần là liên kết HTTPS.");
        var p = await MyProfile(userId, ct);
        p.Username = name; p.DisplayName = Clean(request.DisplayName, 100, true); p.Bio = Clean(request.Bio, 500); p.Visibility = request.Visibility;
        p.AvatarUrl = avatar.Length == 0 ? null : avatar; p.ShowStreak = request.ShowStreak; p.ShowBattleHistory = request.ShowBattleHistory; p.ShowActivityStatus = request.ShowActivityStatus; p.Touch(Now);
        (await db.Users.SingleAsync(x => x.Id == userId, ct)).UpdatePublicIdentity(p.DisplayName, p.AvatarUrl);
        return new { p.Username };
    }
    public async Task<object> RelationshipAsync(Guid userId, Guid targetId, string action, CancellationToken ct)
    {
        Require(userId != targetId, "Không thể thực hiện với chính mình.");
        if (action == "unblock") { db.Relationships.RemoveRange(await db.Relationships.Where(x => x.UserId == userId && x.TargetUserId == targetId && x.Kind == "Block").ToListAsync(ct)); return new { Success = true }; }
        if (action == "block")
        {
            if (!await db.Users.AnyAsync(x => x.Id == targetId, ct)) throw Missing();
            db.Relationships.RemoveRange(await db.Relationships.Where(x => x.Kind != "Block" && ((x.UserId == userId && x.TargetUserId == targetId) || (x.UserId == targetId && x.TargetUserId == userId))).ToListAsync(ct));
            if (!await db.Relationships.AnyAsync(x => x.UserId == userId && x.TargetUserId == targetId && x.Kind == "Block", ct)) db.Relationships.Add(new SocialRelationship { UserId = userId, TargetUserId = targetId, Kind = "Block" });
            changedUsers.Add(targetId); return new { Success = true };
        }
        await EnsurePerson(userId, targetId, ct);
        var edges = await db.Relationships.Where(x => (x.UserId == userId && x.TargetUserId == targetId) || (x.UserId == targetId && x.TargetUserId == userId)).ToListAsync(ct);
        var follow = edges.FirstOrDefault(x => x.UserId == userId && x.Kind == "Follow");
        var request = edges.FirstOrDefault(x => x.UserId == userId && x.Kind == "Request");
        var incoming = edges.FirstOrDefault(x => x.UserId == targetId && x.Kind == "Request" && x.Status == "Pending");
        switch (action)
        {
            case "follow": if (follow is null) { db.Relationships.Add(new SocialRelationship { UserId = userId, TargetUserId = targetId, Kind = "Follow" }); await Notify(targetId, userId, "Follow", userId, ct); } break;
            case "unfollow": if (follow is not null) db.Relationships.Remove(follow); break;
            case "request":
                if (edges.Any(x => x.Kind == "Friend") || request?.Status == "Pending") break;
                if (incoming is not null) throw new SocialException(409, "Người này đã gửi lời mời. Hãy chấp nhận trong mục Bạn bè.");
                if (request is not null && (Now - request.UpdatedAt).TotalHours < 24) throw new SocialException(409, "Hãy chờ 24 giờ trước khi gửi lại lời mời.");
                if (request is null) db.Relationships.Add(new SocialRelationship { UserId = userId, TargetUserId = targetId, Kind = "Request", Status = "Pending" });
                else { request.Status = "Pending"; request.Touch(Now); }
                await Notify(targetId, userId, "FriendRequest", userId, ct); break;
            case "accept":
                if (incoming is null) throw Missing();
                incoming.Status = "Accepted"; incoming.Touch(Now);
                foreach (var pair in new[] { (userId, targetId), (targetId, userId) }) if (!edges.Any(x => x.UserId == pair.Item1 && x.Kind == "Friend")) db.Relationships.Add(new SocialRelationship { UserId = pair.Item1, TargetUserId = pair.Item2, Kind = "Friend" });
                await Notify(targetId, userId, "FriendAccepted", userId, ct); break;
            case "decline": if (incoming is null) throw Missing(); incoming.Status = "Declined"; incoming.Touch(Now); break;
            case "cancel": if (request is null) throw Missing(); request.Status = "Cancelled"; request.Touch(Now); break;
            case "unfriend": db.Relationships.RemoveRange(edges.Where(x => x.Kind == "Friend")); break;
            default: throw new SocialException(400, "Thao tác không hợp lệ.");
        }
        changedUsers.Add(targetId);
        return new { Success = true };
    }
    public async Task<object> PeopleAsync(Guid userId, string kind, int page, string? query, CancellationToken ct)
    {
        var blocked = BlockedIds(userId);
        var q = db.UserProfiles.AsNoTracking().Where(x => !x.IsSuspended && x.UserId != userId);
        if (kind != "blocked") q = q.Where(x => !blocked.Contains(x.UserId));
        q = kind switch {
            "friends" => q.Where(p => db.Relationships.Any(x => x.UserId == userId && x.TargetUserId == p.UserId && x.Kind == "Friend")),
            "requests" => q.Where(p => db.Relationships.Any(x => x.TargetUserId == userId && x.UserId == p.UserId && x.Kind == "Request" && x.Status == "Pending")),
            "following" => q.Where(p => db.Relationships.Any(x => x.UserId == userId && x.TargetUserId == p.UserId && x.Kind == "Follow")),
            "followers" => q.Where(p => db.Relationships.Any(x => x.TargetUserId == userId && x.UserId == p.UserId && x.Kind == "Follow")),
            "blocked" => q.Where(p => db.Relationships.Any(x => x.UserId == userId && x.TargetUserId == p.UserId && x.Kind == "Block")),
            _ => q.Where(x => x.Visibility == "Public")
        };
        var search = Clean(query, 100).ToLowerInvariant();
        if (search.Length > 0) q = q.Where(x => x.Username.Contains(search) || x.DisplayName.ToLower().Contains(search));
        return Page(await q.OrderBy(x => x.Username).Skip(Skip(page)).Take(21).Select(x => new { x.UserId, x.Username, x.DisplayName, x.AvatarUrl, Bio = x.Visibility == "Public" ? x.Bio : "" }).ToListAsync(ct), page);
    }
}
