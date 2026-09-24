using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class Quiz : BaseEntity
{
    private Quiz() { }
    private Quiz(Guid studySetId, string title, int questionCount) { StudySetId = studySetId; Title = title.Trim(); QuestionCount = questionCount; }
    public Guid StudySetId { get; private init; }
    public string Title { get; private set; } = string.Empty;
    public int QuestionCount { get; private init; }
    public static Quiz Create(Guid studySetId, string title, int questionCount) => new(studySetId, title, questionCount);
}
