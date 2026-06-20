namespace Wokki.Application.Dtos.Attendance;

public sealed record AttendanceDailySummaryResponse(
    Guid LocationId,
    DateOnly Date,
    int ScheduledCount,
    int ClockedInCount,
    int ClockedOutCount,
    int NotClockedInCount);
