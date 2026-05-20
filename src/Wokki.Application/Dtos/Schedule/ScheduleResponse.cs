using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.Schedule;

public sealed record ScheduleResponse(
    Guid Id,
    Guid DepartmentId,
    DateOnly WeekStartDate,
    ScheduleStatus Status,
    Guid CreatedBy,
    DateTime? PublishedAt,
    DateTime CreatedAt);
