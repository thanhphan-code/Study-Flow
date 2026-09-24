using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.StudySessions.DTOs;

public sealed record StartStudySessionRequest(Guid StudySetId, StudyMode Mode);
public sealed record StudySessionDto(Guid Id, Guid StudySetId, StudyMode Mode, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, int CardsStudied, int QuestionsAnswered, int CorrectAnswers, int DurationSeconds);
