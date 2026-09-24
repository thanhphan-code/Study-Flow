using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class Question : BaseEntity
{
    private Question() { }
    private Question(Guid quizId, Guid? flashcardId, QuestionType type, string text, string? explanation, int orderIndex) { QuizId = quizId; FlashcardId = flashcardId; Type = type; QuestionText = text; Explanation = explanation; OrderIndex = orderIndex; }
    public Guid QuizId { get; private init; }
    public Guid? FlashcardId { get; private init; }
    public QuestionType Type { get; private init; }
    public string QuestionText { get; private init; } = string.Empty;
    public string? Explanation { get; private init; }
    public int OrderIndex { get; private init; }
    public static Question Create(Guid quizId, Guid flashcardId, QuestionType type, string text, string? explanation, int orderIndex) => new(quizId, flashcardId, type, text.Trim(), explanation, orderIndex);
    public static Question CreateGenerated(Guid quizId, QuestionType type, string text, string? explanation, int orderIndex) => new(quizId, null, type, text.Trim(), explanation, orderIndex);
}
