namespace Wokki.Application.Dtos.Schedule;

public sealed record ScheduleListRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? DepartmentId = null,
    DateOnly? WeekStartDate = null);
