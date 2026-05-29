using Wokki.Application.Common.Interfaces;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.LocationScope.Implementations;

public sealed class LocationScopeService(IUnitOfWork unitOfWork, ICurrentUserService currentUser) : ILocationScopeService
{
    public async Task<IReadOnlySet<Guid>?> GetManagedLocationIdsAsync(Guid userId, string role, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin)
        {
            if (currentUser.OrganizationId is null)
                return new HashSet<Guid>();
            var locations = await unitOfWork.Locations.ListAsync(currentUser.OrganizationId, activeOnly: false, cancellationToken: ct);
            return locations.Select(l => l.Id).ToHashSet();
        }

        if (role != RoleConstants.Manager) return new HashSet<Guid>();
        var rows = await unitOfWork.LocationManagers.GetByUserAsync(userId, ct);
        return rows.Select(r => r.LocationId).ToHashSet();
    }

    public async Task<bool> CanManageLocationAsync(Guid userId, string role, Guid locationId, CancellationToken ct = default)
    {
        var location = await unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken: ct);
        if (location is null) return true;
        if (!IsSameOrganization(location.OrganizationId)) return false;

        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var ids = await GetManagedLocationIdsAsync(userId, role, ct);
        return ids?.Contains(locationId) ?? false;
    }

    public async Task<bool> CanManageScheduleAsync(Guid userId, string role, Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: ct);
        if (schedule is null) return true;
        if (!IsSameOrganization(schedule.OrganizationId)) return false;

        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var dept = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: ct);
        if (dept is null) return true;
        return await CanManageLocationAsync(userId, role, dept.LocationId, ct);
    }

    public async Task<bool> CanManageDepartmentAsync(Guid userId, string role, Guid departmentId, CancellationToken ct = default)
    {
        var dept = await unitOfWork.Departments.GetByIdAsync(departmentId, cancellationToken: ct);
        if (dept is null) return true;
        if (!IsSameOrganization(dept.OrganizationId)) return false;

        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        return await CanManageLocationAsync(userId, role, dept.LocationId, ct);
    }

    public async Task<bool> CanManageEmployeeAsync(Guid userId, string role, Guid employeeId, CancellationToken ct = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken: ct);
        if (employee is null) return true;
        if (!IsSameOrganization(employee.OrganizationId)) return false;

        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var managedIds = await GetManagedLocationIdsAsync(userId, role, ct);
        if (managedIds is null || managedIds.Count == 0) return false;

        var activeLocationMembership = await unitOfWork.LocationMemberships.GetActiveByEmployeeAsync(employeeId, cancellationToken: ct);
        return activeLocationMembership is not null && managedIds.Contains(activeLocationMembership.LocationId);
    }

    public async Task<bool> CanManageAttendanceAsync(Guid userId, string role, Guid attendanceId, CancellationToken ct = default)
    {
        var record = await unitOfWork.Attendance.GetByIdAsync(attendanceId, cancellationToken: ct);
        if (record is null) return true;
        if (!IsSameOrganization(record.OrganizationId)) return false;

        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        if (record.AssignmentId is null) return false;
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(record.AssignmentId.Value, cancellationToken: ct);
        if (assignment is null) return true;
        return await CanManageScheduleAsync(userId, role, assignment.ScheduleId, ct);
    }

    public async Task<bool> CanManageOvertimeRequestAsync(Guid userId, string role, Guid overtimeRequestId, CancellationToken ct = default)
    {
        var ot = await unitOfWork.OvertimeRequests.GetByIdAsync(overtimeRequestId, cancellationToken: ct);
        if (ot is null) return true;
        if (!IsSameOrganization(ot.OrganizationId)) return false;

        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(ot.ShiftAssignmentId, cancellationToken: ct);
        if (assignment is null) return true;
        return await CanManageScheduleAsync(userId, role, assignment.ScheduleId, ct);
    }

    public async Task<bool> CanManageSwapRequestAsync(Guid userId, string role, Guid swapRequestId, CancellationToken ct = default)
    {
        var swap = await unitOfWork.SwapRequests.GetByIdAsync(swapRequestId, cancellationToken: ct);
        if (swap is null) return true;
        if (!IsSameOrganization(swap.OrganizationId)) return false;

        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(swap.RequesterAssignmentId, cancellationToken: ct);
        if (assignment is null) return true;
        return await CanManageScheduleAsync(userId, role, assignment.ScheduleId, ct);
    }

    public async Task<bool> CanManageShiftAsync(Guid userId, string role, Guid shiftDefinitionId, CancellationToken ct = default)
    {
        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(shiftDefinitionId, cancellationToken: ct);
        if (shift is null) return true;
        if (!IsSameOrganization(shift.OrganizationId)) return false;

        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        return await CanManageLocationAsync(userId, role, shift.LocationId, ct);
    }

    private bool IsSameOrganization(Guid organizationId) =>
        currentUser.OrganizationId is not null && currentUser.OrganizationId == organizationId;
}
