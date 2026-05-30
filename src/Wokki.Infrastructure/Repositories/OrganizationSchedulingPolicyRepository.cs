using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class OrganizationSchedulingPolicyRepository(AppDbContext context)
    : IOrganizationSchedulingPolicyRepository
{
    public Task<OrganizationSchedulingPolicy?> GetByOrganizationIdAsync(
        Guid organizationId,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrganizationSchedulingPolicy> query = context.OrganizationSchedulingPolicies;
        if (!track)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(p => p.OrganizationId == organizationId, cancellationToken);
    }

    public Task AddAsync(OrganizationSchedulingPolicy entity, CancellationToken cancellationToken = default) =>
        context.OrganizationSchedulingPolicies.AddAsync(entity, cancellationToken).AsTask();

    public void Update(OrganizationSchedulingPolicy entity) =>
        context.OrganizationSchedulingPolicies.Update(entity);
}
