namespace StudyFlow.Application.LearningHistory.DTOs;

public sealed record StreakDto(int CurrentStreak, int LongestStreak, int TotalActiveDays, DateOnly? LastStudyDate);
public sealed record LearningHistoryDayDto(DateOnly Date, int Sessions, int StudyTimeSeconds, int CardsReviewed, int QuestionsAnswered, int CorrectAnswers, decimal Accuracy);
public sealed record LearningHistoryDto(DateOnly From, DateOnly To, int ActiveDays, int TotalSessions, int TotalStudyTimeSeconds, IReadOnlyList<LearningHistoryDayDto> Days);
