using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class LearningAttempt : BaseEntity
{
    private LearningAttempt() { }
    private LearningAttempt(Guid clientAttemptId, Guid userId, Guid flashcardId, Guid? studySessionId, StudyMode mode, LearningAttemptType attemptType, RecallDirection direction, string submittedAnswer, LearningAttemptResult result, int confidence, int responseTimeMs, bool hintUsed, DateTimeOffset now)
    {
        ClientAttemptId = clientAttemptId; UserId = userId; FlashcardId = flashcardId; StudySessionId = studySessionId; Mode = mode; AttemptType = attemptType; Direction = direction; SubmittedAnswer = submittedAnswer.Trim(); Result = result; Confidence = confidence; ResponseTimeMs = responseTimeMs; HintUsed = hintUsed; CreatedAt = now; UpdatedAt = now;
    }
    public Guid ClientAttemptId { get; private init; }
    public Guid UserId { get; private init; }
    public Guid FlashcardId { get; private init; }
    public Guid? StudySessionId { get; private init; }
    public StudyMode Mode { get; private init; }
    public LearningAttemptType AttemptType { get; private init; }
    public RecallDirection Direction { get; private init; }
    public string SubmittedAnswer { get; private init; } = string.Empty;
    public LearningAttemptResult Result { get; private init; }
    public int Confidence { get; private init; }
    public int ResponseTimeMs { get; private init; }
    public bool HintUsed { get; private init; }
    public bool SrsCommitted { get; private set; }
    public ReviewRating? CommittedRating { get; private set; }
    public LearningCardOutcome? CommittedOutcome { get; private set; }
    public void MarkSrsCommitted(ReviewRating rating, LearningCardOutcome outcome, DateTimeOffset now) { SrsCommitted = true; CommittedRating = rating; CommittedOutcome = outcome; UpdatedAt = now; }
    public static LearningAttempt Create(Guid clientAttemptId, Guid userId, Guid flashcardId, Guid? sessionId, StudyMode mode, LearningAttemptType type, RecallDirection direction, string submittedAnswer, LearningAttemptResult result, int confidence, int responseTimeMs, bool hintUsed, DateTimeOffset now) => new(clientAttemptId, userId, flashcardId, sessionId, mode, type, direction, submittedAnswer, result, confidence, responseTimeMs, hintUsed, now);
}
