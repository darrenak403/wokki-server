namespace Wokki.Application.Services.LocationScope.Interfaces;

public interface ILocationScopeService
{
    // Admin → null (bypass sentinel, do not load all IDs into memory).
    // Manager → set of LocationIds they manage.
    // Other → empty set.
    Task<IReadOnlySet<Guid>?> GetManagedLocationIdsAsync(Guid userId, string role, CancellationToken ct = default);

    Task<bool> CanManageLocationAsync(Guid userId, string role, Guid locationId, CancellationToken ct = default);

    // Resolves: Schedule → DepartmentId → Department → LocationId
    Task<bool> CanManageScheduleAsync(Guid userId, string role, Guid scheduleId, CancellationToken ct = default);

    // Resolves: Department → LocationId
    Task<bool> CanManageDepartmentAsync(Guid userId, string role, Guid departmentId, CancellationToken ct = default);

    // Resolves: Employee → DepartmentId → Department → LocationId
    Task<bool> CanManageEmployeeAsync(Guid userId, string role, Guid employeeId, CancellationToken ct = default);

    // Resolves: AttendanceRecord → AssignmentId → ShiftAssignment → ScheduleId → Schedule → DepartmentId → Department → LocationId
    // Returns true when AssignmentId is null (ad-hoc clock-in — no location to scope against).
    Task<bool> CanManageAttendanceAsync(Guid userId, string role, Guid attendanceId, CancellationToken ct = default);
}
