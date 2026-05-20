namespace Wokki.Application.Dtos.Schedule;

public sealed record UpdateScheduleRequest(Guid DepartmentId, DateOnly WeekStartDate);
