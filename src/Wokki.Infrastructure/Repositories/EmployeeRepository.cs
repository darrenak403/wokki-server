using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.Employees : context.Employees.AsNoTracking();
        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        Guid? departmentId = null,
        Guid? locationId = null,
        bool includeTerminated = false,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Employees.AsNoTracking().AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(e => e.OrganizationId == organizationId.Value);

        if (!includeTerminated)
            query = query.Where(e => e.TerminatedAt == null);

        if (departmentId.HasValue)
        {
            query = query.Where(e =>
                e.DepartmentId == departmentId.Value ||
                context.EmployeeDepartmentMemberships.Any(m =>
                    m.EmployeeId == e.Id &&
                    m.DepartmentId == departmentId.Value &&
                    m.Status == DepartmentMembershipStatus.Active));
        }

        if (locationId.HasValue)
        {
            query = query.Where(e =>
                context.LocationMemberships.Any(m =>
                    m.EmployeeId == e.Id &&
                    m.Status == LocationMembershipStatus.Active &&
                    m.LocationId == locationId.Value) ||
                context.Departments.Any(d => d.Id == e.DepartmentId && d.LocationId == locationId.Value) ||
                context.EmployeeDepartmentMemberships.Any(m =>
                    m.EmployeeId == e.Id &&
                    m.Status == DepartmentMembershipStatus.Active &&
                    context.Departments.Any(d => d.Id == m.DepartmentId && d.LocationId == locationId.Value)));
        }

        if (locationIds is not null)
        {
            var allowedLocationIds = locationIds.ToArray();
            query = allowedLocationIds.Length == 0
                ? query.Where(_ => false)
                : query.Where(e =>
                    context.LocationMemberships.Any(m =>
                        m.EmployeeId == e.Id &&
                        m.Status == LocationMembershipStatus.Active &&
                        allowedLocationIds.Contains(m.LocationId)));
        }

        query = query.OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Employee>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];
        return await context.Employees.AsNoTracking()
            .Where(e => idList.Contains(e.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetIdsByDepartmentIdsAsync(
        IEnumerable<Guid> departmentIds,
        CancellationToken cancellationToken = default)
    {
        var deptList = departmentIds.ToList();
        if (deptList.Count == 0) return [];
        return await context.Employees.AsNoTracking()
            .Where(e =>
                e.TerminatedAt == null &&
                (e.DepartmentId.HasValue && deptList.Contains(e.DepartmentId.Value) ||
                 context.EmployeeDepartmentMemberships.Any(m =>
                     m.EmployeeId == e.Id &&
                     m.Status == DepartmentMembershipStatus.Active &&
                     deptList.Contains(m.DepartmentId))))
            .Select(e => e.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsMemberOfDepartmentAsync(
        Guid employeeId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (await context.Employees.AnyAsync(e => e.Id == employeeId && e.DepartmentId == departmentId, cancellationToken))
            return true;

        return await context.EmployeeDepartmentMemberships.AnyAsync(
            m => m.EmployeeId == employeeId &&
                 m.DepartmentId == departmentId &&
                 m.Status == DepartmentMembershipStatus.Active,
            cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default) =>
        await context.Employees.AddAsync(employee, cancellationToken);

    public void Update(Employee employee) => context.Employees.Update(employee);

    public async Task<Employee?> GetSwapHoldByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.OrganizationId == organizationId && e.FirstName == "Swap" && e.LastName == "Hold",
                cancellationToken);
}
