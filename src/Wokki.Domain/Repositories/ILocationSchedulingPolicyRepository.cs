using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface ILocationSchedulingPolicyRepository
{
    Task<LocationSchedulingPolicy?> GetByLocationIdAsync(
        Guid locationId,
        bool track = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(LocationSchedulingPolicy entity, CancellationToken cancellationToken = default);

    void Update(LocationSchedulingPolicy entity);
}
