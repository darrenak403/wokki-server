using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface ILocationManagerRepository
{
    Task<bool> IsManagerOfLocationAsync(Guid userId, Guid locationId, CancellationToken cancellationToken = default);
    Task<LocationManager?> GetAsync(Guid locationId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationManager>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationManager>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(LocationManager manager, CancellationToken cancellationToken = default);
    void Remove(LocationManager manager);
}
