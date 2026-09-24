namespace StudyFlow.Application.Time;

public sealed record UtcDayRange(DateOnly LocalDate, DateTimeOffset StartUtc, DateTimeOffset EndUtc);

public interface IUserCalendar
{
    bool IsValidTimeZone(string timeZoneId);
    DateOnly GetLocalDate(DateTimeOffset instant, string timeZoneId);
    UtcDayRange GetDayRange(DateOnly localDate, string timeZoneId);
}
