using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IPlatformActivityEventRepository
{
    Task AddAsync(PlatformActivityEvent activityEvent, CancellationToken cancellationToken = default);

    Task<int> CountActiveOrganizationsAsync(
        DateTime from,
        DateTime to,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformUsageOrgActivitySnapshot>> ListOrgActivityAsync(
        DateTime from,
        DateTime to,
        Guid? organizationId = null,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformUsageEventTypeCountSnapshot>> CountByEventTypeAsync(
        DateTime from,
        DateTime to,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformUsageWeeklyActiveSnapshot>> CountWeeklyActiveOrganizationsAsync(
        DateTime from,
        DateTime to,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
}
