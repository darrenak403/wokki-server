using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IOrganizationSchedulingPolicyRepository
{
    Task<OrganizationSchedulingPolicy?> GetByOrganizationIdAsync(
        Guid organizationId,
        bool track = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationSchedulingPolicy entity, CancellationToken cancellationToken = default);

    void Update(OrganizationSchedulingPolicy entity);
}
