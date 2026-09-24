using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Social;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed partial class SocialService
{
    public async Task<object> UnavailableSavesAsync(Guid userId, int page, CancellationToken ct)
    {
        var blocked = BlockedIds(userId);
        var available = from set in db.StudySets join subject in db.Subjects on set.SubjectId equals subject.Id
            where !blocked.Contains(subject.UserId) && !db.UserProfiles.Any(p => p.UserId == subject.UserId && p.IsSuspended)
                && !db.Publications.Any(p => p.StudySetId == set.Id && p.IsRemoved)
                && (subject.UserId == userId || db.Publications.Any(p => p.StudySetId == set.Id && (p.Visibility == "Public" || p.Visibility == "Unlisted" || (p.Visibility == "FriendsOnly" && db.Relationships.Any(r => r.UserId == userId && r.TargetUserId == subject.UserId && r.Kind == "Friend")))))
            select set.Id;
        return Page(await db.Reactions.AsNoTracking().Where(r => r.UserId == userId && r.Kind == "Save" && !available.Contains(r.TargetId))
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id).Skip(Skip(page)).Take(21).Select(r => new { Id = r.TargetId }).ToListAsync(ct), page);
    }
    private async Task<(StudySet Set, Guid Owner, StudySetPublication? Publication)> AccessibleSet(Guid user, Guid id, CancellationToken ct)
    {
        var item = await (from set in db.StudySets join subject in db.Subjects on set.SubjectId equals subject.Id where set.Id == id select new { Set = set, Owner = subject.UserId }).SingleOrDefaultAsync(ct);
        if (item is null) throw Missing();
        await EnsurePerson(user, item.Owner, ct);
        var publication = await db.Publications.SingleOrDefaultAsync(x => x.StudySetId == id, ct);
        if (publication?.IsRemoved == true) throw Missing();
        if (item.Owner != user && (publication is null || publication.Visibility == "Private" || (publication.Visibility == "FriendsOnly" && !await Friends(user, item.Owner, ct)))) throw Missing();
        return (item.Set, item.Owner, publication);
    }
    private IQueryable<Flashcard> Cards(Guid id) => db.Flashcards.Where(x => x.StudySetId == id || db.StudySetSources.Any(s => s.CombinedStudySetId == id && s.SourceStudySetId == x.StudySetId));
    private async Task<Guid> AccessibleCardSet(Guid userId, Guid cardId, CancellationToken ct, Guid? recipient = null)
    {
        var sourceId = await db.Flashcards.Where(x => x.Id == cardId).Select(x => x.StudySetId).SingleOrDefaultAsync(ct);
        if (sourceId == Guid.Empty) throw Missing();
        var candidates = await db.StudySetSources.Where(x => x.SourceStudySetId == sourceId).Select(x => x.CombinedStudySetId).Take(100).ToListAsync(ct);
        candidates.Insert(0, sourceId);
        foreach (var id in candidates)
        {
            try { await AccessibleSet(userId, id, ct); if (recipient is not null) await AccessibleSet(recipient.Value, id, ct); return id; }
            catch (SocialException) { }
        }
        throw Missing();
    }
    public async Task<object> ExploreAsync(Guid userId, string tab, string? query, string sort, int page, Guid? authorId, CancellationToken ct)
    {
        var blocked = BlockedIds(userId);
        var friendIds = db.Relationships.Where(x => x.UserId == userId && x.Kind == "Friend").Select(x => x.TargetUserId);
        var followedIds = db.Relationships.Where(x => x.UserId == userId && x.Kind == "Follow").Select(x => x.TargetUserId);
        var q = from pub in db.Publications.AsNoTracking()
                join set in db.StudySets on pub.StudySetId equals set.Id
                join subject in db.Subjects on set.SubjectId equals subject.Id
                join profile in db.UserProfiles on subject.UserId equals profile.UserId
                where !pub.IsRemoved && !profile.IsSuspended && !blocked.Contains(subject.UserId)
                select new { pub, set, profile };
        if (tab == "mine") q = q.Where(x => x.profile.UserId == userId);
        else if (tab == "saved") q = q.Where(x => db.Reactions.Any(r => r.UserId == userId && r.TargetId == x.set.Id && r.Kind == "Save") && (x.profile.UserId == userId || x.pub.Visibility == "Public" || x.pub.Visibility == "Unlisted" || (x.pub.Visibility == "FriendsOnly" && friendIds.Contains(x.profile.UserId))));
        else if (tab == "friends") q = q.Where(x => friendIds.Contains(x.profile.UserId) && (x.pub.Visibility == "Public" || x.pub.Visibility == "FriendsOnly"));
        else q = q.Where(x => x.pub.Visibility == "Public" || (authorId != null && x.profile.UserId == authorId && (x.profile.UserId == userId || (x.pub.Visibility == "FriendsOnly" && friendIds.Contains(x.profile.UserId)))));
        if (tab == "following") q = q.Where(x => followedIds.Contains(x.profile.UserId));
        if (authorId is not null) q = q.Where(x => x.profile.UserId == authorId);
        var term = Clean(query, 100).ToLowerInvariant();
        if (term.Length > 0) q = q.Where(x => x.set.Title.ToLower().Contains(term) || (x.set.Description != null && x.set.Description.ToLower().Contains(term)) || x.pub.Tags.ToLower().Contains(term));
        var recent = Now.AddDays(-14);
        var scored = q.Select(x => new {
            x.set.Id, x.set.Title, x.set.Description, x.pub.Visibility, x.pub.Tags, x.pub.PublishedAt, UpdatedAt = x.set.UpdatedAt,
            Author = new { x.profile.UserId, x.profile.Username, x.profile.DisplayName, x.profile.AvatarUrl },
            CardCount = db.Flashcards.Count(c => c.StudySetId == x.set.Id || db.StudySetSources.Any(s => s.CombinedStudySetId == x.set.Id && s.SourceStudySetId == c.StudySetId)),
            Likes = db.Reactions.Count(r => r.TargetId == x.set.Id && r.Kind == "Like"),
            Saves = db.Reactions.Count(r => r.TargetId == x.set.Id && r.Kind == "Save"),
            Learners = db.Reactions.Count(r => r.TargetId == x.set.Id && r.Kind == "Learned"),
            IsLiked = db.Reactions.Any(r => r.UserId == userId && r.TargetId == x.set.Id && r.Kind == "Like"),
            IsSaved = db.Reactions.Any(r => r.UserId == userId && r.TargetId == x.set.Id && r.Kind == "Save"),
            Trending = db.Reactions.Count(r => r.TargetId == x.set.Id && r.CreatedAt >= recent && r.Kind == "Like") + 2 * db.Reactions.Count(r => r.TargetId == x.set.Id && r.CreatedAt >= recent && r.Kind == "Save") + 3 * db.Reactions.Count(r => r.TargetId == x.set.Id && r.CreatedAt >= recent && r.Kind == "Learned")
        });
        var ordered = sort switch { "liked" => scored.OrderByDescending(x => x.Likes), "saved" => scored.OrderByDescending(x => x.Saves), "studied" => scored.OrderByDescending(x => x.Learners), "updated" => scored.OrderByDescending(x => x.UpdatedAt), "trending" => scored.OrderByDescending(x => x.Trending), _ => scored.OrderByDescending(x => x.PublishedAt) };
        return Page(await ordered.ThenByDescending(x => x.PublishedAt).ThenBy(x => x.Id).Skip(Skip(page)).Take(21).ToListAsync(ct), page);
    }
    public async Task<object> SetAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var (set, owner, publication) = await AccessibleSet(userId, id, ct);
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == owner, ct);
        var remix = await db.Remixes.AsNoTracking().SingleOrDefaultAsync(x => x.StudySetId == id, ct);
        var originalAvailable = false;
        if (remix is not null) { try { await AccessibleSet(userId, remix.OriginalStudySetId, ct); originalAvailable = true; } catch (SocialException) { } }
        return new {
            set.Id, set.Title, set.Description, IsOwn = userId == owner, Visibility = publication?.Visibility ?? "Private",
            AllowComments = publication?.AllowComments ?? true, AllowRemix = publication?.AllowRemix ?? true, Tags = publication?.Tags ?? "",
            Author = new { UserId = owner, Username = profile?.Username, DisplayName = profile?.DisplayName ?? "Người học", profile?.AvatarUrl },
            Likes = await db.Reactions.CountAsync(x => x.TargetId == id && x.Kind == "Like", ct), Saves = await db.Reactions.CountAsync(x => x.TargetId == id && x.Kind == "Save", ct),
            IsLiked = await db.Reactions.AnyAsync(x => x.TargetId == id && x.UserId == userId && x.Kind == "Like", ct), IsSaved = await db.Reactions.AnyAsync(x => x.TargetId == id && x.UserId == userId && x.Kind == "Save", ct),
            Attribution = remix is null ? null : new { Title = remix.OriginalTitleSnapshot, Author = remix.OriginalAuthorDisplayNameSnapshot, StudySetId = originalAvailable ? (Guid?)remix.OriginalStudySetId : null },
            Cards = await Cards(id).OrderBy(x => x.OrderIndex).ThenBy(x => x.Id).Take(500).Select(x => new {
                x.Id, x.FrontText, x.BackText, x.Explanation, x.LanguageCode, x.ReadingText, x.Romanization, x.ExampleText, x.ExampleTranslation, x.MemoryTip,
                HasImage = x.ImageStorageKey != null,
                HasPrivateSource = db.SourceReferences.Any(s => s.ContentId == x.Id),
                Likes = db.Reactions.Count(r => r.TargetId == x.Id && r.Kind == "CardLike"), IsLiked = db.Reactions.Any(r => r.UserId == userId && r.TargetId == x.Id && r.Kind == "CardLike")
            }).ToListAsync(ct)
        };
    }
    public async Task<object> PublishAsync(Guid userId, Guid id, PublicationRequest request, CancellationToken ct)
    {
        if (!await db.StudySets.AnyAsync(x => x.Id == id && db.Subjects.Any(s => s.Id == x.SubjectId && s.UserId == userId), ct)) throw Missing();
        Require(new[] { "Private", "FriendsOnly", "Public", "Unlisted" }.Contains(request.Visibility), "Quyền chia sẻ không hợp lệ.");
        Require(await Cards(id).CountAsync(ct) <= 500, "Một bộ học cộng đồng có tối đa 500 thẻ.");
        var p = await db.Publications.SingleOrDefaultAsync(x => x.StudySetId == id, ct);
        if (p?.IsRemoved == true) throw new SocialException(403, "Nội dung đã bị ẩn bởi quản trị viên.");
        if (p is null) { p = new StudySetPublication { StudySetId = id }; db.Publications.Add(p); }
        var announce = p.Visibility != "Public" && request.Visibility == "Public";
        await MyProfile(userId, ct);
        p.Visibility = request.Visibility; p.AllowComments = request.AllowComments; p.AllowRemix = request.AllowRemix; p.Tags = Clean(request.Tags, 200); p.Touch(Now);
        if (request.Visibility != "Private") p.PublishedAt ??= Now;
        if (announce)
        {
            var blocked = BlockedIds(userId);
            var recipients = await db.Relationships.Where(x => x.TargetUserId == userId && x.Kind == "Follow" && !blocked.Contains(x.UserId)
                && !db.Notifications.Any(n => n.UserId == x.UserId && n.ActorId == userId && n.Kind == "StudySetPublished" && n.EntityId == id))
                .Select(x => x.UserId).ToListAsync(ct);
            db.Notifications.AddRange(recipients.Select(recipient => new SocialNotification { UserId = recipient, ActorId = userId, Kind = "StudySetPublished", EntityId = id }));
            changedUsers.UnionWith(recipients);
        }
        return new { Success = true };
    }
    public async Task<object> ReactAsync(Guid userId, Guid id, string kind, bool enabled, CancellationToken ct)
    {
        Require(new[] { "Like", "Save", "CardLike" }.Contains(kind), "Tương tác không hợp lệ.");
        var setId = kind == "CardLike" && enabled ? await AccessibleCardSet(userId, id, ct) : id;
        // Removing a saved bookmark stays possible after access has been revoked.
        var existing = await db.Reactions.SingleOrDefaultAsync(x => x.UserId == userId && x.TargetId == id && x.Kind == kind, ct);
        if (!enabled) { if (existing is not null) db.Reactions.Remove(existing); return new { Success = true }; }
        var (_, owner, _) = await AccessibleSet(userId, setId, ct);
        if (existing is null) { db.Reactions.Add(new SocialReaction { UserId = userId, TargetId = id, Kind = kind }); await Notify(owner, userId, kind == "Save" ? "Save" : "Like", setId, ct); }
        return new { Success = true };
    }
    public async Task<object> RemixAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var (source, owner, pub) = await AccessibleSet(userId, id, ct);
        Require(owner == userId || pub?.AllowRemix == true, "Tác giả đã tắt tính năng tạo bản sao.");
        var subject = await db.Subjects.FirstOrDefaultAsync(x => x.UserId == userId && x.Name == "Bộ học đã sao chép", ct);
        if (subject is null) { subject = Subject.Create(userId, "Bộ học đã sao chép", "Bản sao riêng tư từ cộng đồng."); db.Subjects.Add(subject); }
        var copy = StudySet.Create(subject.Id, source.Title, source.Description); db.StudySets.Add(copy);
        var cards = await Cards(id).OrderBy(x => x.OrderIndex).Take(501).ToListAsync(ct);
        Require(cards.Count <= 500, "Bộ học vượt quá giới hạn 500 thẻ.");
        foreach (var c in cards)
        {
            var card = Flashcard.Create(copy.Id, c.FrontText, c.BackText, c.Explanation, c.OrderIndex, c.LanguageCode, c.ReadingText, c.Romanization, c.ExampleText, c.ExampleTranslation, c.MemoryTip, c.AcceptedAnswers, c.EnableReverseRecall);
            if (c.ImageStorageKey is not null && c.ImageContentType is not null)
            {
                var key = $"flashcards/{userId:N}/{card.Id:N}/{Guid.NewGuid():N}";
                await using var stream = await storage.OpenReadAsync(c.ImageStorageKey, ct);
                createdFiles.Add(key); await storage.SaveAsync(key, stream, ct);
                card.SetImage(key, c.ImageContentType, "flashcard-image");
            }
            db.Flashcards.Add(card);
        }
        var name = await db.UserProfiles.Where(x => x.UserId == owner).Select(x => x.DisplayName).SingleOrDefaultAsync(ct) ?? "Người học";
        db.Remixes.Add(new StudySetRemix { StudySetId = copy.Id, OriginalStudySetId = id, OriginalAuthorId = owner, OriginalTitleSnapshot = source.Title, OriginalAuthorDisplayNameSnapshot = name });
        return new { StudySetId = copy.Id };
    }
    public async Task<object> CommentsAsync(Guid userId, Guid id, int page, CancellationToken ct)
    {
        await AccessibleSet(userId, id, ct); var blocked = BlockedIds(userId);
        return Page(await (from comment in db.Comments.AsNoTracking() join profile in db.UserProfiles on comment.UserId equals profile.UserId
            where comment.StudySetId == id && !blocked.Contains(comment.UserId) && !profile.IsSuspended
            orderby comment.CreatedAt, comment.Id
            select new { comment.Id, comment.ParentCommentId, Content = comment.IsDeleted ? "Bình luận đã được xóa." : comment.Content, comment.IsDeleted, comment.CreatedAt, comment.UpdatedAt, IsOwn = comment.UserId == userId, Author = new { profile.UserId, profile.Username, profile.DisplayName, profile.AvatarUrl } }).Skip(Skip(page)).Take(21).ToListAsync(ct), page);
    }
    public async Task<SocialImage> ImageAsync(Guid userId, Guid setId, Guid cardId, CancellationToken ct)
    {
        await AccessibleSet(userId, setId, ct);
        var card = await Cards(setId).SingleOrDefaultAsync(x => x.Id == cardId, ct);
        if (card?.ImageStorageKey is null || card.ImageContentType is null) throw Missing();
        return new SocialImage(await storage.ReadAsync(card.ImageStorageKey, ct), card.ImageContentType);
    }
    public async Task<object> AnswerAsync(Guid userId, Guid setId, PublicAnswerRequest request, CancellationToken ct)
    {
        await AccessibleSet(userId, setId, ct);
        var card = await Cards(setId).SingleOrDefaultAsync(x => x.Id == request.FlashcardId, ct) ?? throw Missing();
        var answer = Clean(request.Answer, 5000, true);
        var result = evaluator.Evaluate(card, RecallDirection.Forward, answer, LearningAttemptType.Recall);
        if (!await db.Reactions.AnyAsync(x => x.UserId == userId && x.TargetId == setId && x.Kind == "Learned", ct)) db.Reactions.Add(new SocialReaction { UserId = userId, TargetId = setId, Kind = "Learned" });
        return new { Result = result.ToString(), card.BackText, card.Explanation };
    }
    public async Task<object> CommentAsync(Guid userId, Guid id, CommentRequest request, CancellationToken ct)
    {
        var (_, owner, pub) = await AccessibleSet(userId, id, ct);
        Require(pub?.AllowComments == true, "Bộ học đã tắt bình luận.");
        var text = Clean(request.Content, 2000, true);
        StudySetComment? parent = null;
        if (request.ParentCommentId is not null)
        {
            parent = await db.Comments.SingleOrDefaultAsync(x => x.Id == request.ParentCommentId && x.StudySetId == id && x.ParentCommentId == null && !x.IsDeleted, ct);
            if (parent is null || await Blocked(userId, parent.UserId, ct)) throw Missing();
        }
        await MyProfile(userId, ct);
        var comment = new StudySetComment { StudySetId = id, UserId = userId, Content = text, ParentCommentId = parent?.Id }; db.Comments.Add(comment);
        await Notify(parent?.UserId ?? owner, userId, parent is null ? "Comment" : "Reply", id, ct);
        return new { comment.Id };
    }
    public async Task<object> EditCommentAsync(Guid userId, Guid id, string? content, CancellationToken ct)
    {
        var comment = await db.Comments.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct) ?? throw Missing();
        if (content is null) { comment.IsDeleted = true; comment.Content = ""; }
        else { await AccessibleSet(userId, comment.StudySetId, ct); Require(!comment.IsDeleted, "Bình luận đã xóa."); comment.Content = Clean(content, 2000, true); }
        comment.Touch(Now); return new { Success = true };
    }
}
