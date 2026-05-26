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

    public async Task<EmployeeDepartmentMembership?> GetByEmployeeAndDepartmentAsync(
        Guid employeeId,
        Guid departmentId,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        var query = track
            ? context.EmployeeDepartmentMemberships
            : context.EmployeeDepartmentMemberships.AsNoTracking();
        return await query.FirstOrDefaultAsync(
            m => m.EmployeeId == employeeId && m.DepartmentId == departmentId,
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
        return await query.FirstOrDefaultAsync(
            m => m.EmployeeId == employeeId && m.Status == DepartmentMembershipStatus.Active && m.IsPrimary,
            cancellationToken);
    }

    public async Task AddAsync(EmployeeDepartmentMembership membership, CancellationToken cancellationToken = default) =>
        await context.EmployeeDepartmentMemberships.AddAsync(membership, cancellationToken);

    public void Update(EmployeeDepartmentMembership membership) =>
        context.EmployeeDepartmentMemberships.Update(membership);

    public async Task ReplaceForEmployeeAsync(
        Guid employeeId,
        IReadOnlyList<Guid> departmentIds,
        Guid primaryDepartmentId,
        CancellationToken cancellationToken = default)
    {
        var unique = departmentIds.Append(primaryDepartmentId).Distinct().ToHashSet();

        var existing = await context.EmployeeDepartmentMemberships
            .Where(m => m.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        // Soft-delete memberships no longer in the new set, preserving history.
        foreach (var m in existing.Where(m => !unique.Contains(m.DepartmentId) && m.Status == DepartmentMembershipStatus.Active))
        {
            m.Status = DepartmentMembershipStatus.Transferred;
            m.LeftAt = DateTime.UtcNow;
            m.IsPrimary = false;
        }

        var existingIds = existing.Select(m => m.DepartmentId).ToHashSet();
        var toAdd = unique.Where(id => !existingIds.Contains(id)).ToList();

        await context.EmployeeDepartmentMemberships.AddRangeAsync(
            toAdd.Select(id => new EmployeeDepartmentMembership
            {
                EmployeeId = employeeId,
                DepartmentId = id,
                IsPrimary = id == primaryDepartmentId,
                Status = DepartmentMembershipStatus.Active,
                JoinedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }),
            cancellationToken);

        // Reactivate or update existing rows in the new set (handles both retained-active and previously-transferred).
        foreach (var m in existing.Where(m => unique.Contains(m.DepartmentId)))
        {
            m.Status = DepartmentMembershipStatus.Active;
            m.IsPrimary = m.DepartmentId == primaryDepartmentId;
            if (m.LeftAt is not null)
            {
                m.JoinedAt = DateTime.UtcNow;
                m.LeftAt = null;
            }
        }
    }
}
