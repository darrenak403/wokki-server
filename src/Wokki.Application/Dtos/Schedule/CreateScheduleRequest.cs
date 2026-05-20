namespace Wokki.Application.Dtos.Schedule;

public sealed record CreateScheduleRequest(Guid DepartmentId, DateOnly WeekStartDate);
