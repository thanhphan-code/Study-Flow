namespace StudyFlow.Application.Subjects.DTOs;

public sealed record CreateSubjectRequest(string Name, string? Description);
public sealed record UpdateSubjectRequest(string Name, string? Description);
public sealed record SubjectDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
