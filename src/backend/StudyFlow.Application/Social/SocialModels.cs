namespace StudyFlow.Application.Social;

public sealed record ProfileRequest(string Username, string DisplayName, string Bio = "", string Visibility = "Public", string? AvatarUrl = null, bool ShowStreak = false, bool ShowBattleHistory = false, bool ShowActivityStatus = false);
public sealed record PublicationRequest(string Visibility, bool AllowComments = true, bool AllowRemix = true, string Tags = "");
public sealed record CommentRequest(string Content, Guid? ParentCommentId = null);
public sealed record MessageRequest(string Kind = "Text", string Content = "", Guid? SharedId = null);
public sealed record ReportRequest(string TargetType, Guid TargetId, string Reason, string Details = "");
public sealed record ModerationRequest(string Action);
public sealed record PublicAnswerRequest(Guid FlashcardId, string Answer);
public sealed record SocialImage(byte[] Content, string ContentType);
public sealed class SocialException(int status, string message) : Exception(message) { public int Status { get; } = status; }
public interface ISocialNotifier { Task ChangedAsync(Guid userId, CancellationToken ct); }
public interface ISocialService
{
    Task<object> WriteAsync(Guid userId, Func<Task<object>> action, CancellationToken ct);
    Task<object> ProfileAsync(Guid userId, string username, CancellationToken ct);
    Task<object> UpdateProfileAsync(Guid userId, ProfileRequest request, CancellationToken ct);
    Task<object> RelationshipAsync(Guid userId, Guid targetId, string action, CancellationToken ct);
    Task<object> PeopleAsync(Guid userId, string kind, int page, string? query, CancellationToken ct);
    Task<object> ExploreAsync(Guid userId, string tab, string? query, string sort, int page, Guid? authorId, CancellationToken ct);
    Task<object> UnavailableSavesAsync(Guid userId, int page, CancellationToken ct);
    Task<object> SetAsync(Guid userId, Guid id, CancellationToken ct);
    Task<SocialImage> ImageAsync(Guid userId, Guid setId, Guid cardId, CancellationToken ct);
    Task<object> AnswerAsync(Guid userId, Guid setId, PublicAnswerRequest request, CancellationToken ct);
    Task<object> PublishAsync(Guid userId, Guid id, PublicationRequest request, CancellationToken ct);
    Task<object> ReactAsync(Guid userId, Guid id, string kind, bool enabled, CancellationToken ct);
    Task<object> RemixAsync(Guid userId, Guid id, CancellationToken ct);
    Task<object> CommentsAsync(Guid userId, Guid id, int page, CancellationToken ct);
    Task<object> CommentAsync(Guid userId, Guid id, CommentRequest request, CancellationToken ct);
    Task<object> EditCommentAsync(Guid userId, Guid id, string? content, CancellationToken ct);
    Task<object> NotificationsAsync(Guid userId, int page, CancellationToken ct);
    Task<object> ReadNotificationsAsync(Guid userId, Guid? id, CancellationToken ct);
    Task<object> ConversationsAsync(Guid userId, int page, CancellationToken ct);
    Task<object> DirectAsync(Guid userId, Guid targetId, CancellationToken ct);
    Task<object> MessagesAsync(Guid userId, Guid id, int page, CancellationToken ct);
    Task<object> SendAsync(Guid userId, Guid id, MessageRequest request, CancellationToken ct);
    Task<object> ReadConversationAsync(Guid userId, Guid id, CancellationToken ct);
    Task<object> ReportAsync(Guid userId, ReportRequest request, CancellationToken ct);
    Task<object> ReportsAsync(int page, CancellationToken ct);
    Task<object> ModerateAsync(Guid id, string action, CancellationToken ct);
}
