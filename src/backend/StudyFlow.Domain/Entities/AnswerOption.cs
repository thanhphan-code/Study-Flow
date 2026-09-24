using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class AnswerOption : BaseEntity
{
    private AnswerOption() { }
    private AnswerOption(Guid questionId, string text, bool isCorrect, int orderIndex) { QuestionId = questionId; Text = text.Trim(); IsCorrect = isCorrect; OrderIndex = orderIndex; }
    public Guid QuestionId { get; private init; }
    public string Text { get; private init; } = string.Empty;
    public bool IsCorrect { get; private init; }
    public int OrderIndex { get; private init; }
    public static AnswerOption Create(Guid questionId, string text, bool isCorrect, int orderIndex) => new(questionId, text, isCorrect, orderIndex);
}
