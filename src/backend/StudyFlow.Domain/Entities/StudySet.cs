using StudyFlow.Domain.Common;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Domain.Entities;

public sealed class StudySet : BaseEntity
{
    private StudySet() { }
    private StudySet(Guid subjectId, string title, string? description, StudySetType type) { SubjectId = subjectId; Title = title.Trim(); Description = Normalize(description); Type = type; }
    public Guid SubjectId { get; private init; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public StudySetType Type { get; private init; }
    public static StudySet Create(Guid subjectId, string title, string? description) => new(subjectId, title, description, StudySetType.Standard);
    public static StudySet CreateCombined(Guid subjectId, string title, string? description) => new(subjectId, title, description, StudySetType.Combined);
    public void Update(string title, string? description) { Title = title.Trim(); Description = Normalize(description); UpdatedAt = DateTimeOffset.UtcNow; }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
