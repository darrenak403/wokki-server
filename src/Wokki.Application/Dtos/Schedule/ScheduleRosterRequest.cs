namespace Wokki.Application.Dtos.Schedule;

public sealed record ScheduleRosterRequest(
    DateOnly WeekStartDate,
    DateOnly? WeekEndDate = null,
    Guid? DepartmentId = null,
    Guid? EmployeeId = null);
