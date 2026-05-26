using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface ILocationManagerRepository
{
    Task<bool> IsManagerOfLocationAsync(Guid userId, Guid locationId, CancellationToken cancellationToken = default);
}
