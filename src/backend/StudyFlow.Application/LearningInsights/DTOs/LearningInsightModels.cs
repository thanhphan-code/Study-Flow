using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.LearningInsights.DTOs;

public sealed record ProgressOverviewDto(int TotalStudySets, int TotalCards, int NewCards, int LearningCards, int ReviewingCards, int MasteredCards, int CardsReviewed, int QuizAttempts, decimal QuizAccuracy, int WeakCards, int RecommendedReviewCards);
public sealed record StudySetProgressDto(Guid StudySetId, string Title, int TotalCards, int NewCards, int LearningCards, int ReviewingCards, int MasteredCards, int CardsReviewed, int QuizAttempts, decimal QuizAccuracy, int WeakCards);
public sealed record WeakCardDto(Guid FlashcardId, Guid StudySetId, string StudySetTitle, string FrontText, FlashcardStatus Status, int CorrectCount, int WrongCount, double EaseFactor, int IntervalDays, DateTimeOffset? NextReviewAt, int WeaknessScore, bool IsDue, string Insight);
