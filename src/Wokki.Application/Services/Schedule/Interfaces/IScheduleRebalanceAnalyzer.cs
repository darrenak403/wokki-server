using Wokki.Application.Dtos.Schedule;

namespace Wokki.Application.Services.Schedule.Interfaces;

public interface IScheduleRebalanceAnalyzer
{
    Task<ScheduleRebalanceHintsResponse> AnalyzeAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default);
}
