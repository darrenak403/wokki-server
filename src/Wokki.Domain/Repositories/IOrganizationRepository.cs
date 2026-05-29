using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
    Task<PlatformStatsSnapshot> GetPlatformStatsAsync(CancellationToken cancellationToken = default);
    Task<OrgStatsSnapshot> GetOrgStatsAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
