using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class OrganizationRepository(AppDbContext context) : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Organization?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<PlatformOrganizationSnapshot> Items, int TotalCount)> ListPlatformAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Organizations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(o => o.Name.ToLower().Contains(term));
        }

        query = query.OrderByDescending(o => o.CreatedAt);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new PlatformOrganizationSnapshot(
                o.Id,
                o.Name,
                o.IsActive,
                o.SubscriptionEnabled,
                o.SubscriptionDurationDays,
                o.SubscriptionActivatedAt,
                o.SubscriptionExpiresAt,
                o.SubscriptionUpdatedAt,
                o.CreatedAt,
                context.Users.Count(u => u.OrganizationId == o.Id),
                context.Locations.Count(l => l.OrganizationId == o.Id),
                context.Employees.Count(e => e.OrganizationId == o.Id && e.TerminatedAt == null)))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default) =>
        await context.Organizations.AddAsync(organization, cancellationToken);

    public async Task<PlatformStatsSnapshot> GetPlatformStatsAsync(CancellationToken cancellationToken = default)
    {
        var orgCount = await context.Organizations.CountAsync(cancellationToken);
        var userCount = await context.Users.CountAsync(u => u.OrganizationId != null, cancellationToken);
        var locationCount = await context.Locations.CountAsync(cancellationToken);
        var employeeCount = await context.Employees.CountAsync(e => e.TerminatedAt == null, cancellationToken);
        return new PlatformStatsSnapshot(orgCount, userCount, locationCount, employeeCount);
    }

    public async Task<OrgStatsSnapshot> GetOrgStatsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var userCount = await context.Users.CountAsync(u => u.OrganizationId == organizationId, cancellationToken);
        var locationCount = await context.Locations.CountAsync(l => l.OrganizationId == organizationId, cancellationToken);
        var departmentCount = await context.Departments.CountAsync(d => d.OrganizationId == organizationId, cancellationToken);
        var employeeCount = await context.Employees.CountAsync(
            e => e.OrganizationId == organizationId && e.TerminatedAt == null,
            cancellationToken);
        var activeMembershipCount = await context.LocationMemberships.CountAsync(
            m => m.Status == Domain.Enums.LocationMembershipStatus.Active &&
                 context.Employees.Any(e => e.Id == m.EmployeeId && e.OrganizationId == organizationId),
            cancellationToken);
        return new OrgStatsSnapshot(userCount, locationCount, departmentCount, employeeCount, activeMembershipCount);
    }

    public async Task<(IReadOnlyList<OrganizationDirectoryItem> Items, int TotalCount)> ListDirectoryAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = context.Organizations.AsNoTracking()
            .Where(o => o.IsActive
                        && o.SubscriptionEnabled
                        && o.SubscriptionExpiresAt != null
                        && o.SubscriptionExpiresAt > now);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(o => o.Name.ToLower().Contains(term));
        }

        query = query.OrderBy(o => o.Name);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrganizationDirectoryItem(o.Id, o.Name))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> HasActivePackageAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return context.Organizations.AsNoTracking()
            .AnyAsync(
                o => o.Id == organizationId
                     && o.IsActive
                     && o.SubscriptionEnabled
                     && o.SubscriptionExpiresAt != null
                     && o.SubscriptionExpiresAt > now,
                cancellationToken);
    }
}
