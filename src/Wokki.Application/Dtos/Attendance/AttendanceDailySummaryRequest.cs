namespace Wokki.Application.Dtos.Attendance;

public sealed record AttendanceDailySummaryRequest(
    Guid LocationId,
    DateOnly Date);
