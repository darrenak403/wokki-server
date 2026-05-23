using Wokki.Application.Dtos.Schedule;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Schedule.Interfaces;

public interface ISchedulePreferenceService
{
    Task<ApiResponse<EmployeeDraftScheduleResponse?>> GetDraftScheduleForEmployeeAsync(
        Guid userId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MySchedulePreferenceResponse>> GetMineAsync(
        Guid userId,
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MySchedulePreferenceResponse>> SaveMineAsync(
        Guid userId,
        Guid scheduleId,
        SaveSchedulePreferencesRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MySchedulePreferenceResponse>> SubmitMineAsync(
        Guid userId,
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SchedulePreferenceBoardResponse>> GetBoardAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default);
}
