using Wokki.Application.Dtos.Schedule;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Schedule.Interfaces;

public interface IScheduleLeaveRequestService
{
    Task<ApiResponse<ScheduleLeaveRequestResponse>> CreateMineAsync(
        Guid userId,
        CreateScheduleLeaveRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>> ListMineAsync(
        Guid userId,
        Guid? scheduleId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> CancelMineAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>> ListForReviewAsync(
        Guid userId,
        string role,
        Guid? scheduleId,
        string? status,
        IReadOnlySet<Guid>? locationIds,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ScheduleLeaveRequestResponse>> ApproveAsync(
        Guid id,
        Guid reviewerUserId,
        string role,
        IReadOnlySet<Guid>? locationIds,
        ReviewScheduleLeaveRequest? request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ScheduleLeaveRequestResponse>> RejectAsync(
        Guid id,
        Guid reviewerUserId,
        string role,
        IReadOnlySet<Guid>? locationIds,
        ReviewScheduleLeaveRequest? request,
        CancellationToken cancellationToken = default);
}
