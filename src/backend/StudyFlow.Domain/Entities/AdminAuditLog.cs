using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class AdminAuditLog : BaseEntity
{
    private AdminAuditLog() { }

    private AdminAuditLog(Guid actorUserId, Guid targetUserId, string action, string reason, string metadataJson)
    {
        ActorUserId = actorUserId;
        TargetUserId = targetUserId;
        Action = action;
        Reason = reason;
        MetadataJson = metadataJson;
    }

    public Guid ActorUserId { get; private init; }
    public Guid TargetUserId { get; private init; }
    public string Action { get; private init; } = string.Empty;
    public string Reason { get; private init; } = string.Empty;
    public string MetadataJson { get; private init; } = "{}";

    public static AdminAuditLog Create(Guid actorUserId, Guid targetUserId, string action, string reason, string metadataJson) =>
        new(actorUserId, targetUserId, action, reason.Trim(), metadataJson);
}
