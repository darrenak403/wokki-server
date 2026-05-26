using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.LocationScope.Implementations;

public sealed class LocationScopeService(IUnitOfWork unitOfWork) : ILocationScopeService
{
    public async Task<IReadOnlySet<Guid>?> GetManagedLocationIdsAsync(Guid userId, string role, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return null;
        if (role != RoleConstants.Manager) return new HashSet<Guid>();
        var rows = await unitOfWork.LocationManagers.GetByUserAsync(userId, ct);
        return rows.Select(r => r.LocationId).ToHashSet();
    }

    public async Task<bool> CanManageLocationAsync(Guid userId, string role, Guid locationId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var ids = await GetManagedLocationIdsAsync(userId, role, ct);
        return ids?.Contains(locationId) ?? false;
    }

    public async Task<bool> CanManageScheduleAsync(Guid userId, string role, Guid scheduleId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: ct);
        if (schedule is null) return true; // 404 handled by service
        var dept = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: ct);
        if (dept is null) return true; // 404 handled by service
        return await CanManageLocationAsync(userId, role, dept.LocationId, ct);
    }

    public async Task<bool> CanManageDepartmentAsync(Guid userId, string role, Guid departmentId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var dept = await unitOfWork.Departments.GetByIdAsync(departmentId, cancellationToken: ct);
        if (dept is null) return true; // 404 handled by service
        return await CanManageLocationAsync(userId, role, dept.LocationId, ct);
    }

    public async Task<bool> CanManageEmployeeAsync(Guid userId, string role, Guid employeeId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken: ct);
        if (employee is null) return true; // 404 handled by service
        var managedIds = await GetManagedLocationIdsAsync(userId, role, ct);
        if (managedIds is null || managedIds.Count == 0) return false;
        // Collect all department IDs: primary + multi-department memberships (BR-074)
        var deptIds = new HashSet<Guid> { employee.DepartmentId };
        var memberships = await unitOfWork.EmployeeDepartmentMemberships.ListByEmployeeAsync(employeeId, ct);
        foreach (var m in memberships) deptIds.Add(m.DepartmentId);
        foreach (var deptId in deptIds)
        {
            var dept = await unitOfWork.Departments.GetByIdAsync(deptId, cancellationToken: ct);
            if (dept is not null && managedIds.Contains(dept.LocationId)) return true;
        }
        return false;
    }

    public async Task<bool> CanManageAttendanceAsync(Guid userId, string role, Guid attendanceId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var record = await unitOfWork.Attendance.GetByIdAsync(attendanceId, cancellationToken: ct);
        if (record is null) return true; // 404 handled by service
        if (record.AssignmentId is null) return true; // ad-hoc clock-in, no location to scope against
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(record.AssignmentId.Value, cancellationToken: ct);
        if (assignment is null) return true; // 404 handled by service
        return await CanManageScheduleAsync(userId, role, assignment.ScheduleId, ct);
    }

    public async Task<bool> CanManageOvertimeRequestAsync(Guid userId, string role, Guid overtimeRequestId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var ot = await unitOfWork.OvertimeRequests.GetByIdAsync(overtimeRequestId, cancellationToken: ct);
        if (ot is null) return true; // 404 handled by service
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(ot.ShiftAssignmentId, cancellationToken: ct);
        if (assignment is null) return true; // 404 handled by service
        return await CanManageScheduleAsync(userId, role, assignment.ScheduleId, ct);
    }

    public async Task<bool> CanManageSwapRequestAsync(Guid userId, string role, Guid swapRequestId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var swap = await unitOfWork.SwapRequests.GetByIdAsync(swapRequestId, cancellationToken: ct);
        if (swap is null) return true; // 404 handled by service
        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(swap.RequesterAssignmentId, cancellationToken: ct);
        if (assignment is null) return true; // 404 handled by service
        return await CanManageScheduleAsync(userId, role, assignment.ScheduleId, ct);
    }

    public async Task<bool> CanManageShiftAsync(Guid userId, string role, Guid shiftDefinitionId, CancellationToken ct = default)
    {
        if (role == RoleConstants.Admin) return true;
        if (role != RoleConstants.Manager) return false;
        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(shiftDefinitionId, cancellationToken: ct);
        if (shift is null) return true; // 404 handled by service
        return await CanManageLocationAsync(userId, role, shift.LocationId, ct);
    }
}
