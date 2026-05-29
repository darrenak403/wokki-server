using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class OrganizationRepository(AppDbContext context) : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

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
}
