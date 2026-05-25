using Wokki.Domain.Entities;

namespace Wokki.Application.Services.Attendance.Interfaces;

public interface IAutoCloseAttendanceService
{
    static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(30);

    // MVP constant — maxOTHours = 2 (not configurable in MVP, per spec decision)
    static readonly TimeSpan MaxOTDuration = TimeSpan.FromHours(2);

    Task AutoCloseIfExpiredAsync(AttendanceRecord record, CancellationToken ct = default);
    Task BulkAutoCloseExpiredAsync(CancellationToken ct = default);
    Task BulkAutoCloseOTSessionsAsync(CancellationToken ct = default);
}
