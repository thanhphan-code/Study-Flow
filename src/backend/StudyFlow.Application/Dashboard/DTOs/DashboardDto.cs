namespace StudyFlow.Application.Dashboard.DTOs;

public sealed record RecentStudySetDto(Guid Id, Guid SubjectId, string Title, DateTimeOffset LastStudiedAt);
public sealed record DashboardDto(int DueCards, int StudyTimeSecondsToday, int CardsReviewedToday, int QuestionsAnsweredToday, decimal AccuracyToday, IReadOnlyList<RecentStudySetDto> RecentStudySets);
