using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class ScheduleInsightContextRepository(AppDbContext context) : IScheduleInsightContextRepository
{
    public Task<ScheduleInsightContext?> GetByScheduleIdAsync(
        Guid scheduleId,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ScheduleInsightContext> query = context.ScheduleInsightContexts;
        if (!track)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(x => x.ScheduleId == scheduleId, cancellationToken);
    }

    public Task AddAsync(ScheduleInsightContext entity, CancellationToken cancellationToken = default) =>
        context.ScheduleInsightContexts.AddAsync(entity, cancellationToken).AsTask();

    public void Update(ScheduleInsightContext entity) =>
        context.ScheduleInsightContexts.Update(entity);
}
