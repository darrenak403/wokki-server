using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<Location?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Location>> ListAsync(bool activeOnly = true, IReadOnlySet<Guid>? locationIds = null, CancellationToken cancellationToken = default);
    Task AddAsync(Location location, CancellationToken cancellationToken = default);
    void Update(Location location);
}
