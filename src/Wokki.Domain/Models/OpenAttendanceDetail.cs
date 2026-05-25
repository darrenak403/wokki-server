namespace Wokki.Domain.Models;

public sealed record OpenAttendanceDetail(
    Guid RecordId,
    Guid EmployeeId,
    DateTimeOffset ClockIn,
    DateOnly AssignmentDate,
    TimeOnly ShiftStartTime,
    TimeOnly ShiftEndTime,
    string? LocationTimeZone
);
