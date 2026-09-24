using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class QuizAttempt : BaseEntity
{
    private QuizAttempt() { }
    private QuizAttempt(Guid userId, Guid quizId, DateTimeOffset startedAt) { UserId = userId; QuizId = quizId; StartedAt = startedAt; }
    public Guid UserId { get; private init; }
    public Guid QuizId { get; private init; }
    public DateTimeOffset StartedAt { get; private init; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public double? Score { get; private set; }
    public int CorrectCount { get; private set; }
    public int WrongCount { get; private set; }
    public bool IsCompleted => CompletedAt.HasValue;
    public static QuizAttempt Start(Guid userId, Guid quizId, DateTimeOffset now) => new(userId, quizId, now);
    public void Complete(int correctCount, int questionCount, DateTimeOffset now)
    {
        if (IsCompleted) throw new InvalidOperationException("Quiz attempt is already completed.");
        CorrectCount = correctCount; WrongCount = questionCount - correctCount; Score = questionCount == 0 ? 0 : Math.Round(correctCount * 100d / questionCount, 2); CompletedAt = now; UpdatedAt = now;
    }
}
