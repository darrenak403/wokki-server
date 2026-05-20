namespace Wokki.Application.Dtos.Attendance;

public sealed record AdjustAttendanceRequest(
    DateTimeOffset ClockIn,
    DateTimeOffset ClockOut,
    string AdjustmentNote);
