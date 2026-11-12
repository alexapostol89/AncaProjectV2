namespace api.Tests.Infrastructure;

/// <summary>Dates and slot times in Romania time, the way the booking rules see them.</summary>
public static class TestTime
{
    public static readonly TimeZoneInfo Romania = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "GTB Standard Time" : "Europe/Bucharest");

    /// <summary>A Monday–Saturday date at least <paramref name="daysAhead"/> days from today (Romania time).</summary>
    public static DateOnly FutureWorkday(int daysAhead = 7)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Romania).DateTime);
        var date = today.AddDays(daysAhead);
        return date.DayOfWeek == DayOfWeek.Sunday ? date.AddDays(1) : date;
    }

    /// <summary>The first Sunday at least a week away.</summary>
    public static DateOnly FutureSunday()
    {
        var date = FutureWorkday(7);
        while (date.DayOfWeek != DayOfWeek.Sunday) date = date.AddDays(1);
        return date;
    }

    /// <summary>Romania wall-clock time on <paramref name="date"/> as a DateTimeOffset with the correct offset (handles summer time).</summary>
    public static DateTimeOffset At(DateOnly date, int hour, int minute)
    {
        var local = date.ToDateTime(new TimeOnly(hour, minute));
        return new DateTimeOffset(local, Romania.GetUtcOffset(local));
    }

    /// <summary>The n-th 50-minute slot of the day (0 = 10:00, 1 = 10:50, ... 11 = 19:10).</summary>
    public static DateTimeOffset Slot(DateOnly date, int index)
    {
        var minutes = 600 + index * 50;
        return At(date, minutes / 60, minutes % 60);
    }
}
