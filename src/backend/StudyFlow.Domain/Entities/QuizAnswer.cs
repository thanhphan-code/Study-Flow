using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class QuizAnswer : BaseEntity
{
    private QuizAnswer() { }
    private QuizAnswer(Guid attemptId, Guid questionId, Guid answerOptionId, bool isCorrect, DateTimeOffset answeredAt) { QuizAttemptId = attemptId; QuestionId = questionId; AnswerOptionId = answerOptionId; IsCorrect = isCorrect; AnsweredAt = answeredAt; }
    public Guid QuizAttemptId { get; private init; }
    public Guid QuestionId { get; private init; }
    public Guid AnswerOptionId { get; private set; }
    public bool IsCorrect { get; private set; }
    public DateTimeOffset AnsweredAt { get; private set; }
    public static QuizAnswer Create(Guid attemptId, Guid questionId, Guid optionId, bool isCorrect, DateTimeOffset now) => new(attemptId, questionId, optionId, isCorrect, now);
    public void Change(Guid optionId, bool isCorrect, DateTimeOffset now) { AnswerOptionId = optionId; IsCorrect = isCorrect; AnsweredAt = now; UpdatedAt = now; }
}
