using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class LocationManagerRepository(AppDbContext context) : ILocationManagerRepository
{
    public async Task<bool> IsManagerOfLocationAsync(Guid userId, Guid locationId, CancellationToken cancellationToken = default) =>
        await context.LocationManagers
            .AnyAsync(lm => lm.UserId == userId && lm.LocationId == locationId, cancellationToken);

    public async Task<LocationManager?> GetAsync(Guid locationId, Guid userId, CancellationToken cancellationToken = default) =>
        await context.LocationManagers
            .Include(lm => lm.Location)
            .Include(lm => lm.User)
            .FirstOrDefaultAsync(lm => lm.LocationId == locationId && lm.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<LocationManager>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken = default) =>
        await context.LocationManagers
            .AsNoTracking()
            .Include(lm => lm.Location)
            .Include(lm => lm.User)
            .Where(lm => lm.LocationId == locationId)
            .OrderBy(lm => lm.AssignedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LocationManager>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.LocationManagers
            .AsNoTracking()
            .Include(lm => lm.Location)
            .Include(lm => lm.User)
            .Where(lm => lm.UserId == userId)
            .OrderBy(lm => lm.AssignedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(LocationManager manager, CancellationToken cancellationToken = default) =>
        await context.LocationManagers.AddAsync(manager, cancellationToken);

    public void Remove(LocationManager manager) =>
        context.LocationManagers.Remove(manager);
}
