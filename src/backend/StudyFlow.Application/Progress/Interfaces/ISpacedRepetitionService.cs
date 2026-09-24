using StudyFlow.Application.Progress.DTOs;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.Progress.Interfaces;

public interface ISpacedRepetitionService
{
    ReviewSchedule Calculate(FlashcardStatus currentStatus, int currentIntervalDays, double currentEaseFactor, ReviewRating rating, DateTimeOffset now);
}

public interface ISrsScheduler
{
    string Version { get; }
    ReviewSchedule Calculate(SrsSchedulingContext context);
}
