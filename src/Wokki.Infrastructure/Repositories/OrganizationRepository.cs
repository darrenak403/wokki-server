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
        string? status = null,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Organizations.AsNoTracking().AsQueryable();
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(o => o.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = status.Trim().ToLowerInvariant() switch
            {
                "disabled" => query.Where(o => !o.IsActive),
                "notactivated" => query.Where(o =>
                    o.IsActive && (!o.SubscriptionEnabled || o.SubscriptionExpiresAt == null)),
                "expired" => query.Where(o =>
                    o.IsActive
                    && o.SubscriptionEnabled
                    && o.SubscriptionExpiresAt != null
                    && o.SubscriptionExpiresAt <= now),
                "active" => query.Where(o =>
                    o.IsActive
                    && o.SubscriptionEnabled
                    && o.SubscriptionExpiresAt != null
                    && o.SubscriptionExpiresAt > now),
                _ => query
            };
        }

        query = ApplyPlatformOrdering(query, sortBy, sortDirection);
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

    public async Task<(IReadOnlyList<PlatformSupportSearchSnapshot> Items, int TotalCount)> SearchPlatformSupportAsync(
        int page,
        int pageSize,
        string? query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return (Array.Empty<PlatformSupportSearchSnapshot>(), 0);

        var term = query.Trim();
        var lowered = term.ToLowerInvariant();
        var isGuid = Guid.TryParse(term, out var organizationId);
        var maxCandidates = page * pageSize;

        var organizationMatches = context.Organizations.AsNoTracking()
            .Where(o => (isGuid && o.Id == organizationId) || o.Name.ToLower().Contains(lowered));

        var userMatches =
            from user in context.Users.AsNoTracking()
            join organization in context.Organizations.AsNoTracking()
                on user.OrganizationId equals organization.Id
            where user.Email.ToLower().Contains(lowered)
            select new { User = user, Organization = organization };

        var organizationCount = await organizationMatches.CountAsync(cancellationToken);
        var userCount = await userMatches.CountAsync(cancellationToken);

        var organizationItems = await organizationMatches
            .OrderBy(o => o.Name)
            .Take(maxCandidates)
            .Select(o => new PlatformSupportSearchSnapshot(
                "Organization",
                o.Id,
                o.Name,
                o.IsActive,
                o.SubscriptionEnabled,
                o.SubscriptionDurationDays,
                o.SubscriptionActivatedAt,
                o.SubscriptionExpiresAt,
                o.SubscriptionUpdatedAt,
                o.CreatedAt,
                null,
                null,
                null,
                null,
                null,
                null,
                context.Users.Count(u => u.OrganizationId == o.Id),
                context.Locations.Count(l => l.OrganizationId == o.Id),
                context.Employees.Count(e => e.OrganizationId == o.Id && e.TerminatedAt == null),
                context.Schedules
                    .Where(s => s.OrganizationId == o.Id)
                    .Max(s => (DateTime?)s.CreatedAt),
                context.Schedules
                    .Where(s => s.OrganizationId == o.Id)
                    .Max(s => s.PublishedAt),
                context.AttendanceRecords
                    .Where(a => a.OrganizationId == o.Id)
                    .Max(a => (DateTimeOffset?)a.ClockIn),
                context.Messages
                    .Where(m => m.OrganizationId == o.Id)
                    .Max(m => (DateTime?)m.CreatedAt)))
            .ToListAsync(cancellationToken);

        var userItems = await userMatches
            .OrderBy(x => x.User.Email)
            .Take(maxCandidates)
            .Select(x => new PlatformSupportSearchSnapshot(
                "User",
                x.Organization.Id,
                x.Organization.Name,
                x.Organization.IsActive,
                x.Organization.SubscriptionEnabled,
                x.Organization.SubscriptionDurationDays,
                x.Organization.SubscriptionActivatedAt,
                x.Organization.SubscriptionExpiresAt,
                x.Organization.SubscriptionUpdatedAt,
                x.Organization.CreatedAt,
                x.User.Id,
                x.User.Email,
                x.User.Role,
                x.User.FirstName,
                x.User.LastName,
                x.User.CreatedAt,
                context.Users.Count(u => u.OrganizationId == x.Organization.Id),
                context.Locations.Count(l => l.OrganizationId == x.Organization.Id),
                context.Employees.Count(e => e.OrganizationId == x.Organization.Id && e.TerminatedAt == null),
                context.Schedules
                    .Where(s => s.OrganizationId == x.Organization.Id)
                    .Max(s => (DateTime?)s.CreatedAt),
                context.Schedules
                    .Where(s => s.OrganizationId == x.Organization.Id)
                    .Max(s => s.PublishedAt),
                context.AttendanceRecords
                    .Where(a => a.OrganizationId == x.Organization.Id)
                    .Max(a => (DateTimeOffset?)a.ClockIn),
                context.Messages
                    .Where(m => m.OrganizationId == x.Organization.Id)
                    .Max(m => (DateTime?)m.CreatedAt)))
            .ToListAsync(cancellationToken);

        var items = organizationItems
            .Concat(userItems)
            .OrderBy(x => x.MatchType)
            .ThenBy(x => x.OrganizationName)
            .ThenBy(x => x.UserEmail)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, organizationCount + userCount);
    }

    public async Task<PlatformOrganizationSupportContextSnapshot?> GetPlatformSupportContextAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var organization = await context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (organization is null)
            return null;

        var latestLedger = await context.OrganizationSubscriptionLedgerEntries
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.ChangedAt)
            .Select(x => new
            {
                x.Id,
                x.Action,
                x.PreviousStatus,
                x.NewStatus,
                x.ChangedAt,
                x.ChangedByUserId
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new PlatformOrganizationSupportContextSnapshot(
            organization.Id,
            organization.Name,
            organization.IsActive,
            organization.SubscriptionEnabled,
            organization.SubscriptionDurationDays,
            organization.SubscriptionActivatedAt,
            organization.SubscriptionExpiresAt,
            organization.SubscriptionUpdatedAt,
            organization.CreatedAt,
            await context.Users.CountAsync(u => u.OrganizationId == organizationId, cancellationToken),
            await context.Employees.CountAsync(
                e => e.OrganizationId == organizationId && e.TerminatedAt == null,
                cancellationToken),
            await context.Locations.CountAsync(l => l.OrganizationId == organizationId, cancellationToken),
            await context.Departments.CountAsync(d => d.OrganizationId == organizationId, cancellationToken),
            await context.Schedules
                .Where(s => s.OrganizationId == organizationId)
                .MaxAsync(s => (DateTime?)s.CreatedAt, cancellationToken),
            await context.Schedules
                .Where(s => s.OrganizationId == organizationId)
                .MaxAsync(s => s.PublishedAt, cancellationToken),
            await context.AttendanceRecords
                .Where(a => a.OrganizationId == organizationId)
                .MaxAsync(a => (DateTimeOffset?)a.ClockIn, cancellationToken),
            await context.Messages
                .Where(m => m.OrganizationId == organizationId)
                .MaxAsync(m => (DateTime?)m.CreatedAt, cancellationToken),
            latestLedger?.Id,
            latestLedger?.Action,
            latestLedger?.PreviousStatus,
            latestLedger?.NewStatus,
            latestLedger?.ChangedAt,
            latestLedger?.ChangedByUserId);
    }

    private static IQueryable<Organization> ApplyPlatformOrdering(
        IQueryable<Organization> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => descending
                ? query.OrderByDescending(o => o.Name).ThenByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.Name).ThenBy(o => o.CreatedAt),
            "expirydate" => descending
                ? query.OrderBy(o => o.SubscriptionExpiresAt == null)
                    .ThenByDescending(o => o.SubscriptionExpiresAt)
                    .ThenBy(o => o.Name)
                : query.OrderBy(o => o.SubscriptionExpiresAt == null)
                    .ThenBy(o => o.SubscriptionExpiresAt)
                    .ThenBy(o => o.Name),
            _ => descending
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt)
        };
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
