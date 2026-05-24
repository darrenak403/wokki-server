using Wokki.Application.Dtos.Schedule;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Schedule.Interfaces;

public interface IScheduleInsightService
{
    Task<ApiResponse<ScheduleInsightContextResponse>> GenerateContextAsync(
        Guid scheduleId,
        GenerateScheduleInsightContextRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ScheduleInsightContextResponse>> GetContextAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ScheduleInsightChatResponse>> ChatAsync(
        Guid scheduleId,
        ScheduleInsightChatRequest request,
        CancellationToken cancellationToken = default);
}
