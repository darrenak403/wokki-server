namespace Wokki.Application.Common;

internal static class SwapCutoffRules
{
    public static bool IsCutoffExceeded(DateOnly assignmentDate, TimeZoneInfo timeZone, bool isCreate)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var today = DateOnly.FromDateTime(now);
        var currentWeekMonday = GetWeekMonday(today);
        var nextWeekMonday = currentWeekMonday.AddDays(7);
        var assignmentWeekMonday = GetWeekMonday(assignmentDate);

        if (assignmentWeekMonday != nextWeekMonday)
            return false;

        if (isCreate)
        {
            var friday = currentWeekMonday.AddDays(4);
            return today > friday;
        }

        var deadline = assignmentWeekMonday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return now >= deadline;
    }

    public static DateOnly GetWeekMonday(DateOnly date)
    {
        var offset = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(-offset);
    }

    public static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
