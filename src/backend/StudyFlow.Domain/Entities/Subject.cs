using StudyFlow.Domain.Common;

namespace StudyFlow.Domain.Entities;

public sealed class Subject : BaseEntity
{
    private Subject() { }

    private Subject(Guid userId, string name, string? description)
    {
        UserId = userId;
        Name = name.Trim();
        Description = NormalizeDescription(description);
    }

    public Guid UserId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public static Subject Create(Guid userId, string name, string? description) => new(userId, name, description);

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = NormalizeDescription(description);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeDescription(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
