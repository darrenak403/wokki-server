using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class PlatformActivityEventRepository(AppDbContext context) : IPlatformActivityEventRepository
{
    public async Task AddAsync(PlatformActivityEvent activityEvent, CancellationToken cancellationToken = default) =>
        await context.PlatformActivityEvents.AddAsync(activityEvent, cancellationToken);

    public async Task<int> CountActiveOrganizationsAsync(
        DateTime from,
        DateTime to,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyWindow(context.PlatformActivityEvents.AsNoTracking(), from, to, organizationId);
        return await query.Select(x => x.OrganizationId).Distinct().CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformUsageOrgActivitySnapshot>> ListOrgActivityAsync(
        DateTime from,
        DateTime to,
        Guid? organizationId = null,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyWindow(context.PlatformActivityEvents.AsNoTracking(), from, to, organizationId);

        var ordered = await query
            .GroupBy(x => x.OrganizationId)
            .Select(g => new
            {
                OrganizationId = g.Key,
                LastActivityAt = g.Max(x => x.OccurredAt),
                ActivityCount = g.Count()
            })
            .Join(
                context.Organizations.AsNoTracking(),
                activity => activity.OrganizationId,
                organization => organization.Id,
                (activity, organization) => new
                {
                    activity.OrganizationId,
                    OrganizationName = organization.Name,
                    activity.LastActivityAt,
                    activity.ActivityCount
                })
            .OrderByDescending(x => x.ActivityCount)
            .ThenByDescending(x => x.LastActivityAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return ordered
            .Select(x => new PlatformUsageOrgActivitySnapshot(
                x.OrganizationId,
                x.OrganizationName,
                x.LastActivityAt,
                x.ActivityCount))
            .ToList();
    }

    public async Task<IReadOnlyList<PlatformUsageEventTypeCountSnapshot>> CountByEventTypeAsync(
        DateTime from,
        DateTime to,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyWindow(context.PlatformActivityEvents.AsNoTracking(), from, to, organizationId);

        var ordered = await query
            .GroupBy(x => x.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.EventType)
            .ToListAsync(cancellationToken);

        return ordered
            .Select(x => new PlatformUsageEventTypeCountSnapshot(x.EventType, x.Count))
            .ToList();
    }

    public async Task<IReadOnlyList<PlatformUsageWeeklyActiveSnapshot>> CountWeeklyActiveOrganizationsAsync(
        DateTime from,
        DateTime to,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyWindow(context.PlatformActivityEvents.AsNoTracking(), from, to, organizationId);
        var events = await query
            .Select(x => new { x.OrganizationId, x.OccurredAt })
            .ToListAsync(cancellationToken);

        return events
            .GroupBy(x => GetWeekStart(DateOnly.FromDateTime(x.OccurredAt)))
            .Select(g => new PlatformUsageWeeklyActiveSnapshot(
                g.Key,
                g.Select(x => x.OrganizationId).Distinct().Count()))
            .OrderBy(x => x.WeekStartDate)
            .ToList();
    }

    public async Task<IReadOnlyList<PlatformUsageDailyEventTypeCountSnapshot>> CountDailyByEventTypeAsync(
        DateTime from,
        DateTime to,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyWindow(context.PlatformActivityEvents.AsNoTracking(), from, to, organizationId);
        var events = await query
            .Select(x => new { x.OccurredAt, x.EventType })
            .ToListAsync(cancellationToken);

        return events
            .GroupBy(x => (Date: DateOnly.FromDateTime(x.OccurredAt), x.EventType))
            .Select(g => new PlatformUsageDailyEventTypeCountSnapshot(g.Key.Date, g.Key.EventType, g.Count()))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.EventType)
            .ToList();
    }

    private static IQueryable<PlatformActivityEvent> ApplyWindow(
        IQueryable<PlatformActivityEvent> query,
        DateTime from,
        DateTime to,
        Guid? organizationId)
    {
        query = query.Where(x => x.OccurredAt >= from && x.OccurredAt <= to);
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        return query;
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }
}
