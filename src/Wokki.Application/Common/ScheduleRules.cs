namespace Wokki.Application.Common;

internal static class ScheduleRules
{
    public static bool IsMonday(DateOnly weekStartDate) =>
        weekStartDate.DayOfWeek == DayOfWeek.Monday;
}
