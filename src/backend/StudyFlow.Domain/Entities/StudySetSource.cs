namespace StudyFlow.Domain.Entities;
public sealed class StudySetSource
{
    private StudySetSource() { }
    private StudySetSource(Guid combinedStudySetId, Guid sourceStudySetId, int orderIndex) { CombinedStudySetId = combinedStudySetId; SourceStudySetId = sourceStudySetId; OrderIndex = orderIndex; }
    public Guid CombinedStudySetId { get; private init; }
    public Guid SourceStudySetId { get; private init; }
    public int OrderIndex { get; private set; }
    public static StudySetSource Create(Guid combinedStudySetId, Guid sourceStudySetId, int orderIndex) => new(combinedStudySetId, sourceStudySetId, orderIndex);
}
