using Wokki.Application.Common;
using Wokki.Application.Dtos.Attendance;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Attendance.Interfaces;

public interface IAttendanceService
{
    Task<ApiResponse<AttendanceResponse>> ClockInAsync(
        Guid userId,
        ClockInRequest? request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AttendanceResponse>> ClockOutAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<AttendanceResponse>>> ListAsync(
        AttendanceListRequest request,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<AttendanceResponse>>> ListMineAsync(
        Guid userId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AttendanceResponse>> AdjustAsync(
        Guid id,
        AdjustAttendanceRequest request,
        Guid adjustedByUserId,
        CancellationToken cancellationToken = default);
}
