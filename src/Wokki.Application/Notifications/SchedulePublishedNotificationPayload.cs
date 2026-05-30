namespace Wokki.Application.Notifications;

public sealed record SchedulePublishedShiftLine(
    DateOnly Date,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record SchedulePublishedNotificationPayload(
    string EmployeeFirstName,
    DateOnly WeekStartDate,
    string? LocationName,
    string? DepartmentName,
    IReadOnlyList<SchedulePublishedShiftLine> Shifts);
