using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.LearningHistory.DTOs;
using StudyFlow.Application.LearningHistory.Interfaces;
using StudyFlow.Application.Time;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class LearningHistoryService(StudyFlowDbContext dbContext, IUserCalendar calendar, TimeProvider timeProvider) : ILearningHistoryService
{
    public async Task<StreakDto> GetStreakAsync(Guid userId, CancellationToken cancellationToken)
    {
        var timeZoneId = await GetTimeZoneAsync(userId, cancellationToken);
        var timestamps = await ValidSessions(userId).Select(x => x.StartedAt).ToListAsync(cancellationToken);
        var dates = timestamps.Select(x => calendar.GetLocalDate(x, timeZoneId)).Distinct().OrderBy(x => x).ToList();
        if (dates.Count == 0) return new StreakDto(0, 0, 0, null);

        var longest = 1; var run = 1;
        for (var index = 1; index < dates.Count; index++)
        {
            run = dates[index].DayNumber == dates[index - 1].DayNumber + 1 ? run + 1 : 1;
            longest = Math.Max(longest, run);
        }

        var today = calendar.GetLocalDate(timeProvider.GetUtcNow(), timeZoneId);
        var cursor = dates[^1]; var current = cursor == today || cursor == today.AddDays(-1) ? 1 : 0;
        for (var index = dates.Count - 2; current > 0 && index >= 0 && dates[index].DayNumber == cursor.DayNumber - 1; index--)
        {
            current++; cursor = dates[index];
        }
        return new StreakDto(current, longest, dates.Count, dates[^1]);
    }

    public async Task<LearningHistoryDto> GetHistoryAsync(Guid userId, int days, CancellationToken cancellationToken)
    {
        var timeZoneId = await GetTimeZoneAsync(userId, cancellationToken);
        var today = calendar.GetLocalDate(timeProvider.GetUtcNow(), timeZoneId);
        var from = today.AddDays(-(days - 1));
        var range = calendar.GetDayRange(from, timeZoneId);
        var sessions = await ValidSessions(userId).Where(x => x.StartedAt >= range.StartUtc)
            .Select(x => new { x.StartedAt, x.DurationSeconds, x.CardsStudied, x.QuestionsAnswered, x.CorrectAnswers })
            .ToListAsync(cancellationToken);
        var grouped = sessions.GroupBy(x => calendar.GetLocalDate(x.StartedAt, timeZoneId)).ToDictionary(x => x.Key);
        var result = Enumerable.Range(0, days).Select(offset =>
        {
            var date = from.AddDays(offset);
            if (!grouped.TryGetValue(date, out var rows)) return new LearningHistoryDayDto(date, 0, 0, 0, 0, 0, 0);
            var values = rows.ToList(); var answers = values.Sum(x => x.CardsStudied + x.QuestionsAnswered); var correct = values.Sum(x => x.CorrectAnswers);
            return new LearningHistoryDayDto(date, values.Count, values.Sum(x => x.DurationSeconds), values.Sum(x => x.CardsStudied), values.Sum(x => x.QuestionsAnswered), correct, answers == 0 ? 0 : Math.Round((decimal)correct * 100 / answers, 2));
        }).ToList();
        return new LearningHistoryDto(from, today, result.Count(x => x.Sessions > 0), sessions.Count, sessions.Sum(x => x.DurationSeconds), result);
    }

    private IQueryable<Domain.Entities.StudySession> ValidSessions(Guid userId) => dbContext.StudySessions.AsNoTracking().Where(x => x.UserId == userId && x.EndedAt != null && x.CardsStudied + x.QuestionsAnswered > 0);
    private async Task<string> GetTimeZoneAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.TimeZoneId).SingleAsync(cancellationToken);
}
