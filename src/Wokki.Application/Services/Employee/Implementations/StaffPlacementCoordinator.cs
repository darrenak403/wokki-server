using Wokki.Application.Common.Interfaces;
using Wokki.Application.Mappings.Employees;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using DepartmentEntity = Wokki.Domain.Entities.Department;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using LocationManagerEntity = Wokki.Domain.Entities.LocationManager;
using LocationMembershipEntity = Wokki.Domain.Entities.LocationMembership;

namespace Wokki.Application.Services.Employee.Implementations;

public sealed class StaffPlacementCoordinator(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IStaffPlacementCoordinator
{
    public async Task AssignManagerLocationsAsync(
        Guid userId,
        Guid organizationId,
        IReadOnlyList<Guid> locationIds,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
            throw new InvalidOperationException("Current user is required to assign manager locations.");

        var assignedById = currentUser.UserId.Value;
        foreach (var locationId in locationIds.Distinct())
        {
            if (locationId == Guid.Empty)
                continue;

            var existing = await unitOfWork.LocationManagers.GetAsync(locationId, userId, cancellationToken);
            if (existing is not null)
                continue;

            await unitOfWork.LocationManagers.AddAsync(new LocationManagerEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                LocationId = locationId,
                UserId = userId,
                AssignedById = assignedById,
                AssignedAt = DateTime.UtcNow,
            }, cancellationToken);
        }
    }

    public async Task EnsureActiveLocationMembershipAsync(
        Guid employeeId,
        Guid organizationId,
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        var active = await unitOfWork.LocationMemberships.GetActiveByEmployeeAsync(
            employeeId, track: true, cancellationToken: cancellationToken);
        if (active?.LocationId == locationId)
            return;

        if (active is not null)
        {
            active.Status = LocationMembershipStatus.Transferred;
            active.ReviewedAt = DateTime.UtcNow;
            unitOfWork.LocationMemberships.Update(active);
        }

        await unitOfWork.LocationMemberships.AddAsync(new LocationMembershipEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            LocationId = locationId,
            Status = LocationMembershipStatus.Active,
            RequestedAt = DateTime.UtcNow,
            ReviewedAt = DateTime.UtcNow,
        }, cancellationToken);
    }

    public async Task EndActiveLocationMembershipAsync(
        Guid employeeId,
        Guid? reviewedById,
        CancellationToken cancellationToken = default)
    {
        var active = await unitOfWork.LocationMemberships.GetActiveByEmployeeAsync(
            employeeId, track: true, cancellationToken: cancellationToken);
        if (active is null)
            return;

        active.Status = LocationMembershipStatus.Transferred;
        active.ReviewedById = reviewedById;
        active.ReviewedAt = DateTime.UtcNow;
        unitOfWork.LocationMemberships.Update(active);
    }

    public async Task ClearDepartmentMembershipsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await unitOfWork.EmployeeDepartmentMemberships.ListByEmployeeAsync(employeeId, cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var membership in memberships.Where(m => m.Status == DepartmentMembershipStatus.Active))
        {
            membership.Status = DepartmentMembershipStatus.Transferred;
            membership.LeftAt = now;
            membership.IsPrimary = false;
            unitOfWork.EmployeeDepartmentMemberships.Update(membership);
        }
    }

    public async Task ApplyEmployeeDepartmentAsync(
        Guid employeeId,
        Guid organizationId,
        DepartmentEntity department,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, track: true, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException($"Employee {employeeId} not found.");

        await unitOfWork.EmployeeDepartmentMemberships.ReplaceForEmployeeAsync(
            employeeId,
            organizationId,
            [department.Id],
            department.Id,
            cancellationToken);

        employee.DepartmentId = department.Id;
        EmployeeMapper.SyncPositionFromDepartment(employee, department);
        unitOfWork.Employees.Update(employee);
    }

    public async Task RemoveAllLocationManagersForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await unitOfWork.LocationManagers.GetTrackedByUserIdAsync(userId, cancellationToken);
        foreach (var row in rows)
            unitOfWork.LocationManagers.Remove(row);
    }
}
