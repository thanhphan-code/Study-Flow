using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class StudySession : BaseEntity
{
    private StudySession() { }
    private StudySession(Guid userId, Guid studySetId, StudyMode mode, DateTimeOffset startedAt) { UserId = userId; StudySetId = studySetId; Mode = mode; StartedAt = startedAt; }
    public Guid UserId { get; private init; }
    public Guid StudySetId { get; private init; }
    public StudyMode Mode { get; private init; }
    public DateTimeOffset StartedAt { get; private init; }
    public DateTimeOffset? LastActivityAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public int CardsStudied { get; private set; }
    public int QuestionsAnswered { get; private set; }
    public int CorrectAnswers { get; private set; }
    public int DurationSeconds { get; private set; }
    public bool IsCompleted => EndedAt.HasValue;
    public int TotalAnswers => CardsStudied + QuestionsAnswered;
    public static StudySession Start(Guid userId, Guid studySetId, StudyMode mode, DateTimeOffset now) => new(userId, studySetId, mode, now);
    public static StudySession FromCompletedQuiz(Guid userId, Guid studySetId, DateTimeOffset startedAt, DateTimeOffset endedAt, int questions, int correct)
    {
        var session = new StudySession(userId, studySetId, StudyMode.Quiz, startedAt); session.QuestionsAnswered = questions; session.CorrectAnswers = correct; session.Complete(endedAt); return session;
    }
    public void RecordCardReview(bool correct, DateTimeOffset now)
    {
        if (IsCompleted) throw new InvalidOperationException("Study session is completed."); CardsStudied++; if (correct) CorrectAnswers++; LastActivityAt = now; UpdatedAt = now;
    }
    public void Complete(DateTimeOffset now)
    {
        if (IsCompleted) return; EndedAt = now; LastActivityAt ??= now; DurationSeconds = TotalAnswers == 0 ? 0 : Math.Max(1, (int)(now - StartedAt).TotalSeconds); UpdatedAt = now;
    }
}
