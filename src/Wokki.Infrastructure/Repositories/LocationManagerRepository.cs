using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class LocationManagerRepository(AppDbContext context) : ILocationManagerRepository
{
    public async Task<bool> IsManagerOfLocationAsync(Guid userId, Guid locationId, CancellationToken cancellationToken = default) =>
        await context.LocationManagers
            .AnyAsync(lm => lm.UserId == userId && lm.LocationId == locationId, cancellationToken);
}
