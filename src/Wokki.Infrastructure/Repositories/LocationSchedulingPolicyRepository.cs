using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class LocationSchedulingPolicyRepository(AppDbContext context) : ILocationSchedulingPolicyRepository
{
    public Task<LocationSchedulingPolicy?> GetByLocationIdAsync(
        Guid locationId,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<LocationSchedulingPolicy> query = context.LocationSchedulingPolicies;
        if (!track)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(p => p.LocationId == locationId, cancellationToken);
    }

    public Task AddAsync(LocationSchedulingPolicy entity, CancellationToken cancellationToken = default) =>
        context.LocationSchedulingPolicies.AddAsync(entity, cancellationToken).AsTask();

    public void Update(LocationSchedulingPolicy entity) =>
        context.LocationSchedulingPolicies.Update(entity);
}
