using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Dashboard.DTOs;
using StudyFlow.Application.Dashboard.Interfaces;
using StudyFlow.Application.Time;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class DashboardService(StudyFlowDbContext dbContext, IUserCalendar calendar, TimeProvider timeProvider) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var timeZoneId = await dbContext.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.TimeZoneId).SingleAsync(cancellationToken);
        var localToday = calendar.GetLocalDate(now, timeZoneId);
        var dayRange = calendar.GetDayRange(localToday, timeZoneId);
        var today = await dbContext.StudySessions.AsNoTracking()
            .Where(x => x.UserId == userId && x.EndedAt != null && x.StartedAt >= dayRange.StartUtc && x.StartedAt < dayRange.EndUtc && x.CardsStudied + x.QuestionsAnswered > 0)
            .Select(x => new { x.DurationSeconds, x.CardsStudied, x.QuestionsAnswered, x.CorrectAnswers })
            .ToListAsync(cancellationToken);
        var dueCards = await dbContext.FlashcardProgress.CountAsync(x => x.UserId == userId && x.NextReviewAt != null && x.NextReviewAt <= now, cancellationToken);
        var totalAnswers = today.Sum(x => x.CardsStudied + x.QuestionsAnswered);

        var recentActivity = await dbContext.StudySessions.AsNoTracking()
            .Where(x => x.UserId == userId && x.EndedAt != null && x.CardsStudied + x.QuestionsAnswered > 0)
            .GroupBy(x => x.StudySetId)
            .Select(group => new { StudySetId = group.Key, LastStudiedAt = group.Max(x => x.LastActivityAt!.Value) })
            .OrderByDescending(x => x.LastStudiedAt).Take(5)
            .Join(dbContext.StudySets, activity => activity.StudySetId, set => set.Id,
                (activity, set) => new RecentStudySetDto(set.Id, set.SubjectId, set.Title, activity.LastStudiedAt))
            .ToListAsync(cancellationToken);

        return new DashboardDto(dueCards, today.Sum(x => x.DurationSeconds), today.Sum(x => x.CardsStudied),
            today.Sum(x => x.QuestionsAnswered), totalAnswers == 0 ? 0 : Math.Round((decimal)today.Sum(x => x.CorrectAnswers) * 100 / totalAnswers, 2), recentActivity);
    }
}
