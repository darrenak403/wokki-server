using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IScheduleInsightContextRepository
{
    Task<ScheduleInsightContext?> GetByScheduleIdAsync(
        Guid scheduleId,
        bool track = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(ScheduleInsightContext entity, CancellationToken cancellationToken = default);

    void Update(ScheduleInsightContext entity);
}
