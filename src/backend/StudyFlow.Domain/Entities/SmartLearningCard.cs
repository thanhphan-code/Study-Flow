using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class SmartLearningCard : BaseEntity
{
    private SmartLearningCard() { }
    private SmartLearningCard(Guid sessionId, Guid flashcardId, int orderIndex, LearningAttemptType attemptType)
    {
        StudySessionId = sessionId; FlashcardId = flashcardId; OrderIndex = orderIndex; AttemptType = attemptType; Direction = RecallDirection.Forward;
    }
    public Guid StudySessionId { get; private init; }
    public Guid FlashcardId { get; private init; }
    public int OrderIndex { get; private init; }
    public LearningAttemptType AttemptType { get; private set; }
    public RecallDirection Direction { get; private set; }
    public LearningCardOutcome Outcome { get; private set; } = LearningCardOutcome.InProgress;
    public int NextEligibleAttemptNumber { get; private set; }
    public static SmartLearningCard Create(Guid sessionId, Guid flashcardId, int orderIndex, LearningAttemptType attemptType) => new(sessionId, flashcardId, orderIndex, attemptType);
    public void Schedule(LearningAttemptType type, RecallDirection direction, int nextEligibleAttemptNumber, DateTimeOffset now) { AttemptType = type; Direction = direction; NextEligibleAttemptNumber = nextEligibleAttemptNumber; UpdatedAt = now; }
    public void Finish(LearningCardOutcome outcome, DateTimeOffset now) { Outcome = outcome; UpdatedAt = now; }
}
