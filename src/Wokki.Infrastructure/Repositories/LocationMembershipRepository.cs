using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class LocationMembershipRepository(AppDbContext context) : ILocationMembershipRepository
{
    public async Task<LocationMembership?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.LocationMemberships : context.LocationMemberships.AsNoTracking();
        return await query
            .Include(m => m.Location)
            .Include(m => m.Employee)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<LocationMembership?> GetActiveByEmployeeAsync(
        Guid employeeId,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        var query = track ? context.LocationMemberships : context.LocationMemberships.AsNoTracking();
        return await query
            .Include(m => m.Location)
            .Include(m => m.Employee)
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.Status == LocationMembershipStatus.Active, cancellationToken);
    }

    public async Task<LocationMembership?> GetLatestPendingByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await context.LocationMemberships
            .AsNoTracking()
            .Include(m => m.Location)
            .Include(m => m.Employee)
            .Where(m => m.EmployeeId == employeeId && m.Status == LocationMembershipStatus.Pending)
            .OrderByDescending(m => m.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> HasPendingOrActiveAsync(Guid employeeId, Guid locationId, CancellationToken cancellationToken = default) =>
        await context.LocationMemberships
            .AnyAsync(m =>
                m.EmployeeId == employeeId &&
                m.LocationId == locationId &&
                (m.Status == LocationMembershipStatus.Pending || m.Status == LocationMembershipStatus.Active),
                cancellationToken);

    public async Task<IReadOnlyList<LocationMembership>> ListPendingAsync(
        IReadOnlySet<Guid>? locationIds,
        CancellationToken cancellationToken = default)
    {
        var query = context.LocationMemberships
            .AsNoTracking()
            .Include(m => m.Location)
            .Include(m => m.Employee)
            .Where(m => m.Status == LocationMembershipStatus.Pending);

        if (locationIds is not null)
            query = query.Where(m => locationIds.Contains(m.LocationId));

        return await query.OrderByDescending(m => m.RequestedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocationMembership>> ListByLocationAsync(
        Guid locationId,
        LocationMembershipStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = context.LocationMemberships
            .AsNoTracking()
            .Include(m => m.Location)
            .Include(m => m.Employee)
            .Where(m => m.LocationId == locationId);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        return await query.OrderByDescending(m => m.RequestedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LocationMembership membership, CancellationToken cancellationToken = default) =>
        await context.LocationMemberships.AddAsync(membership, cancellationToken);

    public void Update(LocationMembership membership) =>
        context.LocationMemberships.Update(membership);
}
