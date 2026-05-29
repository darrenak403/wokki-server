using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
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
            .OrderByDescending(m => m.JoinedAt)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(
        Guid employeeId,
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        context.EmployeeDepartmentMemberships.AnyAsync(
            m => m.EmployeeId == employeeId &&
                 m.DepartmentId == departmentId &&
                 m.Status == DepartmentMembershipStatus.Active,
            cancellationToken);

    public async Task<EmployeeDepartmentMembership?> GetByEmployeeAndDepartmentAsync(
        Guid employeeId,
        Guid departmentId,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        var query = track
            ? context.EmployeeDepartmentMemberships
            : context.EmployeeDepartmentMemberships.AsNoTracking();
        return await query
            .Where(m => m.EmployeeId == employeeId && m.DepartmentId == departmentId)
            .OrderByDescending(m => m.Status == DepartmentMembershipStatus.Active)
            .ThenByDescending(m => m.JoinedAt)
            .FirstOrDefaultAsync(
            cancellationToken);
    }

    public async Task<EmployeeDepartmentMembership?> GetActivePrimaryByEmployeeAsync(
        Guid employeeId,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        var query = track
            ? context.EmployeeDepartmentMemberships
            : context.EmployeeDepartmentMemberships.AsNoTracking();
        return await query
            .Where(m => m.EmployeeId == employeeId && m.Status == DepartmentMembershipStatus.Active && m.IsPrimary)
            .OrderByDescending(m => m.JoinedAt)
            .FirstOrDefaultAsync(
            cancellationToken);
    }

    public async Task AddAsync(EmployeeDepartmentMembership membership, CancellationToken cancellationToken = default) =>
        await context.EmployeeDepartmentMemberships.AddAsync(membership, cancellationToken);

    public void Update(EmployeeDepartmentMembership membership) =>
        context.EmployeeDepartmentMemberships.Update(membership);

    public async Task ReplaceForEmployeeAsync(
        Guid employeeId,
        Guid organizationId,
        IReadOnlyList<Guid> departmentIds,
        Guid primaryDepartmentId,
        CancellationToken cancellationToken = default)
    {
        var unique = departmentIds.Append(primaryDepartmentId).Distinct().ToHashSet();
        var now = DateTime.UtcNow;

        var existing = await context.EmployeeDepartmentMemberships
            .Where(m => m.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var activeRows = existing.Where(m => m.Status == DepartmentMembershipStatus.Active).ToList();

        // Close active memberships no longer in the new set, preserving full history rows.
        foreach (var m in activeRows.Where(m => !unique.Contains(m.DepartmentId)))
        {
            m.Status = DepartmentMembershipStatus.Transferred;
            m.LeftAt = now;
            m.IsPrimary = false;
        }

        var activeByDepartment = activeRows
            .GroupBy(m => m.DepartmentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.JoinedAt).First());

        var toAdd = unique.Where(id => !activeByDepartment.ContainsKey(id)).ToList();

        await context.EmployeeDepartmentMemberships.AddRangeAsync(
            toAdd.Select(id => new EmployeeDepartmentMembership
            {
                EmployeeId = employeeId,
                OrganizationId = organizationId,
                DepartmentId = id,
                IsPrimary = id == primaryDepartmentId,
                Status = DepartmentMembershipStatus.Active,
                JoinedAt = now,
                CreatedAt = now
            }),
            cancellationToken);

        // Keep only active rows in the target set and make the intended one primary.
        foreach (var m in activeRows.Where(m => unique.Contains(m.DepartmentId)))
        {
            m.IsPrimary = m.DepartmentId == primaryDepartmentId;
        }
    }
}
