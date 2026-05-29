using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class LocationRepository(AppDbContext context) : ILocationRepository
{
    public async Task<Location?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.Locations : context.Locations.AsNoTracking();
        return await query.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Location?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await context.Locations.FirstOrDefaultAsync(l => l.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Location>> ListAsync(
        bool activeOnly = true,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Locations.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(l => l.IsActive);

        if (locationIds is not null)
        {
            var allowedLocationIds = locationIds.ToArray();
            query = allowedLocationIds.Length == 0
                ? query.Where(_ => false)
                : query.Where(l => allowedLocationIds.Contains(l.Id));
        }

        return await query.OrderBy(l => l.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Location location, CancellationToken cancellationToken = default) =>
        await context.Locations.AddAsync(location, cancellationToken);

    public void Update(Location location) => context.Locations.Update(location);
}
