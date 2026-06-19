namespace Wokki.Domain.Models;

public sealed record LocationAttendanceSummary(
    int ScheduledCount,
    int ClockedInCount,
    int ClockedOutCount,
    int NotClockedInCount
);
