using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Organization?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PlatformOrganizationSnapshot> Items, int TotalCount)> ListPlatformAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
    Task<PlatformStatsSnapshot> GetPlatformStatsAsync(CancellationToken cancellationToken = default);
    Task<OrgStatsSnapshot> GetOrgStatsAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
