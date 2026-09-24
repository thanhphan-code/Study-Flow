using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Bio { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string Visibility { get; set; } = "Public";
    public bool ShowStreak { get; set; }
    public bool ShowBattleHistory { get; set; }
    public bool ShowActivityStatus { get; set; }
    public bool IsSuspended { get; set; }
}

public sealed class StudySetPublication : BaseEntity
{
    public Guid StudySetId { get; set; }
    public string Visibility { get; set; } = "Private";
    public bool AllowComments { get; set; } = true;
    public bool AllowRemix { get; set; } = true;
    public string Tags { get; set; } = "";
    public DateTimeOffset? PublishedAt { get; set; }
    public bool IsRemoved { get; set; }
}

// A unique directed edge models follows, blocks, requests, and accepted friends.
// Accepted friendships have both directions, committed together in one transaction.
public sealed class SocialRelationship : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TargetUserId { get; set; }
    public string Kind { get; set; } = "";
    public string Status { get; set; } = "Active";
}

public sealed class SocialReaction : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TargetId { get; set; }
    public string Kind { get; set; } = "";
}

public sealed class StudySetRemix : BaseEntity
{
    public Guid StudySetId { get; set; }
    public Guid OriginalStudySetId { get; set; }
    public Guid OriginalAuthorId { get; set; }
    public string OriginalTitleSnapshot { get; set; } = "";
    public string OriginalAuthorDisplayNameSnapshot { get; set; } = "";
}

public sealed class StudySetComment : BaseEntity
{
    public Guid StudySetId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = "";
    public bool IsDeleted { get; set; }
}

public sealed class SocialNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ActorId { get; set; }
    public string Kind { get; set; } = "";
    public Guid EntityId { get; set; }
    public bool IsRead { get; set; }
}

public sealed class DirectConversation : BaseEntity
{
    public Guid UserAId { get; set; }
    public Guid UserBId { get; set; }
    public DateTimeOffset? UserAReadAt { get; set; }
    public DateTimeOffset? UserBReadAt { get; set; }
}

public sealed class DirectMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Kind { get; set; } = "Text";
    public string Content { get; set; } = "";
    public Guid? SharedId { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class ContentReport : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Details { get; set; } = "";
    public string Status { get; set; } = "Pending";
}
