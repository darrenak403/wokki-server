using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class EmployeeDepartmentMembershipRepository(AppDbContext context) : IEmployeeDepartmentMembershipRepository
{
    public async Task<IReadOnlyList<EmployeeDepartmentMembership>> ListByEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await context.EmployeeDepartmentMemberships
            .AsNoTracking()
            .Where(m => m.EmployeeId == employeeId)
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.DepartmentId)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(
        Guid employeeId,
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        context.EmployeeDepartmentMemberships.AnyAsync(
            m => m.EmployeeId == employeeId && m.DepartmentId == departmentId,
            cancellationToken);

    public async Task ReplaceForEmployeeAsync(
        Guid employeeId,
        IReadOnlyList<Guid> departmentIds,
        Guid primaryDepartmentId,
        CancellationToken cancellationToken = default)
    {
        var current = await context.EmployeeDepartmentMemberships
            .Where(m => m.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);
        context.EmployeeDepartmentMemberships.RemoveRange(current);

        var unique = departmentIds.Append(primaryDepartmentId).Distinct().ToList();
        await context.EmployeeDepartmentMemberships.AddRangeAsync(
            unique.Select(id => new EmployeeDepartmentMembership
            {
                EmployeeId = employeeId,
                DepartmentId = id,
                IsPrimary = id == primaryDepartmentId,
                CreatedAt = DateTime.UtcNow
            }),
            cancellationToken);
    }
}
