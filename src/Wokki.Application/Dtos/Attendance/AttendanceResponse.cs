using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.Attendance;

public sealed record AttendanceResponse(
    Guid Id,
    Guid EmployeeId,
    Guid? AssignmentId,
    DateTimeOffset ClockIn,
    DateTimeOffset? ClockOut,
    int WorkedMinutes,
    bool AutoClosed,
    AttendanceStatus Status,
    Guid? AdjustedBy,
    string? AdjustmentNote,
    DateTime CreatedAt,
    Guid? ShiftDefinitionId = null,
    string? ShiftName = null,
    string? ShiftColor = null,
    DateOnly? ScheduledDate = null,
    TimeOnly? ScheduledStartTime = null,
    TimeOnly? ScheduledEndTime = null,
    Guid? DepartmentId = null,
    string? DepartmentName = null,
    Guid? LocationId = null,
    string? LocationName = null);
