namespace Wokki.Application.Dtos.Attendance;

public sealed record AttendanceResponse(
    Guid Id,
    Guid EmployeeId,
    Guid? AssignmentId,
    DateTimeOffset ClockIn,
    DateTimeOffset? ClockOut,
    int WorkedMinutes,
    Guid? AdjustedBy,
    string? AdjustmentNote,
    DateTime CreatedAt);
