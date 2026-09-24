using StudyFlow.Application.Flashcards.DTOs;
namespace StudyFlow.Application.StudySets.DTOs;

public sealed record CreateStudySetRequest(string Title, string? Description);
public sealed record UpdateStudySetRequest(string Title, string? Description);
public sealed record StudySetSourceDto(Guid Id, string Title, int OrderIndex);
public sealed record StudySetDto(Guid Id, Guid SubjectId, string Title, string? Description, string Type, IReadOnlyList<StudySetSourceDto> Sources, int TotalCards, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record CreateCombinedStudySetRequest(string Title, string? Description, IReadOnlyList<Guid> SourceStudySetIds);
public sealed record UpdateStudySetSourcesRequest(IReadOnlyList<Guid> SourceStudySetIds);
public sealed record StudyTogetherRequest(IReadOnlyList<Guid> SourceStudySetIds);
public sealed record StudyTogetherDto(IReadOnlyList<StudySetSourceDto> Sources, IReadOnlyList<FlashcardDto> Flashcards);
