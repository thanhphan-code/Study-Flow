using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;
namespace StudyFlow.Domain.Entities;
public sealed class AIJob : BaseEntity
{
    private AIJob() { }
    private AIJob(Guid userId, Guid studySetId, Guid? documentId, AIJobType type) { UserId=userId; StudySetId=studySetId; DocumentId=documentId; JobType=type; }
    public Guid UserId { get; private init; } public Guid StudySetId { get; private init; } public Guid? DocumentId { get; private init; }
    public AIJobType JobType { get; private init; } public AIJobStatus Status { get; private set; }=AIJobStatus.Pending;
    public string? ErrorMessage { get; private set; } public string? ResultJson { get; private set; } public DateTimeOffset? CompletedAt { get; private set; }
    public string? GroundingContextJson { get; private set; } public string? RetrievalRevision { get; private set; }
    public DateTimeOffset? SavedAt { get; private set; }
    public static AIJob Create(Guid userId,Guid studySetId,Guid? documentId,AIJobType type)=>new(userId,studySetId,documentId,type);
    public void Start(DateTimeOffset now){Status=AIJobStatus.Processing;UpdatedAt=now;}
    public void SetGroundingContext(string contextJson,string retrievalRevision){GroundingContextJson=contextJson;RetrievalRevision=retrievalRevision;}
    public void Complete(string resultJson,DateTimeOffset now){Status=AIJobStatus.Completed;ResultJson=resultJson;ErrorMessage=null;CompletedAt=now;UpdatedAt=now;}
    public void Fail(string error,DateTimeOffset now){Status=AIJobStatus.Failed;ErrorMessage=error[..Math.Min(error.Length,1000)];CompletedAt=now;UpdatedAt=now;}
    public void MarkSaved(DateTimeOffset now){if(SavedAt.HasValue)throw new InvalidOperationException("AI draft was already saved.");SavedAt=now;UpdatedAt=now;}
}
