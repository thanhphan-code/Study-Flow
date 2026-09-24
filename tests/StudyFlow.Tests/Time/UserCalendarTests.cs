using StudyFlow.Infrastructure.Time;

namespace StudyFlow.Tests.Time;

public sealed class UserCalendarTests
{
    private readonly UserCalendar _calendar = new();

    [Fact]
    public void VietnamAfterMidnight_BelongsToNextLocalDay()
    {
        var instant = new DateTimeOffset(2026, 9, 20, 17, 30, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 9, 21), _calendar.GetLocalDate(instant, "Asia/Ho_Chi_Minh"));
    }

    [Fact]
    public void NewYorkDstDay_HasTwentyThreeHours()
    {
        var range = _calendar.GetDayRange(new DateOnly(2026, 3, 8), "America/New_York");
        Assert.Equal(TimeSpan.FromHours(23), range.EndUtc - range.StartUtc);
    }
}
