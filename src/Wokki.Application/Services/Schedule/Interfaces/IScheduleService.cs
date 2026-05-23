using Wokki.Application.Dtos.Schedule;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Schedule.Interfaces;

public interface IScheduleService
{
    Task<ApiResponse<PagedResponse<ScheduleResponse>>> ListAsync(
        ScheduleListRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<ScheduleDetailResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ScheduleResponse>> CreateAsync(
        CreateScheduleRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<ScheduleResponse>> UpdateAsync(
        Guid id,
        UpdateScheduleRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ScheduleResponse>> PublishAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ScheduleResponse>> UnpublishAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ScheduleResponse>> CopyAsync(
        Guid id,
        CopyScheduleRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>> ListAssignmentsAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<ShiftAssignmentResponse>> CreateAssignmentAsync(
        Guid scheduleId,
        CreateShiftAssignmentRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAssignmentAsync(
        Guid scheduleId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>> GetMyScheduleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<ScheduleSuggestionsResponse>> SuggestAsync(
        Guid scheduleId,
        SuggestScheduleRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>> ApplySuggestionsAsync(
        Guid scheduleId,
        ApplyScheduleSuggestionsRequest request,
        CancellationToken cancellationToken = default);
}
