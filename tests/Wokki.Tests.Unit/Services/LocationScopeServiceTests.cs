using Moq;
using Wokki.Application.Services.LocationScope.Implementations;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;

namespace Wokki.Tests.Unit.Services;

public class LocationScopeServiceTests
{
    // ─── helpers ────────────────────────────────────────────────────────────

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid LocationId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid AttendanceId = Guid.NewGuid();
    private static readonly Guid AssignmentId = Guid.NewGuid();
    private static readonly Guid ScheduleId = Guid.NewGuid();

    private static (LocationScopeService svc, Mock<IUnitOfWork> uow) Build()
    {
        var uow = new Mock<IUnitOfWork>();

        // default: no location-manager rows
        uow.Setup(u => u.LocationManagers.GetByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(Array.Empty<LocationManager>());

        // default: entities not found
        uow.Setup(u => u.Employees.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((Employee?)null);
        uow.Setup(u => u.Departments.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((Department?)null);
        uow.Setup(u => u.Schedules.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((Schedule?)null);
        uow.Setup(u => u.ShiftAssignments.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((ShiftAssignment?)null);
        uow.Setup(u => u.Attendance.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((AttendanceRecord?)null);
        uow.Setup(u => u.EmployeeDepartmentMemberships.ListByEmployeeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(Array.Empty<EmployeeDepartmentMembership>());

        var svc = new LocationScopeService(uow.Object);
        return (svc, uow);
    }

    // ─── CanManageEmployeeAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CanManageEmployeeAsync_Admin_ReturnsTrue()
    {
        var (svc, _) = Build();
        var result = await svc.CanManageEmployeeAsync(UserId, RoleConstants.Admin, EmployeeId);
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageEmployeeAsync_NonManagerRole_ReturnsFalse()
    {
        var (svc, _) = Build();
        var result = await svc.CanManageEmployeeAsync(UserId, RoleConstants.User, EmployeeId);
        Assert.False(result);
    }

    [Fact]
    public async Task CanManageEmployeeAsync_Manager_EmployeeNotFound_ReturnsTrue()
    {
        // non-existent employee → 404 handled by service → return true
        var (svc, uow) = Build();
        uow.Setup(u => u.Employees.GetByIdAsync(EmployeeId, false, default))
           .ReturnsAsync((Employee?)null);

        var result = await svc.CanManageEmployeeAsync(UserId, RoleConstants.Manager, EmployeeId);
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageEmployeeAsync_Manager_EmployeeInManagedLocation_ReturnsTrue()
    {
        var (svc, uow) = Build();

        var employee = new Employee { Id = EmployeeId, DepartmentId = DepartmentId };
        uow.Setup(u => u.Employees.GetByIdAsync(EmployeeId, false, default)).ReturnsAsync(employee);

        var department = new Department { Id = DepartmentId, LocationId = LocationId };
        uow.Setup(u => u.Departments.GetByIdAsync(DepartmentId, false, default)).ReturnsAsync(department);

        var managedRow = new LocationManager { LocationId = LocationId, UserId = UserId };
        uow.Setup(u => u.LocationManagers.GetByUserAsync(UserId, default))
           .ReturnsAsync(new[] { managedRow });

        var result = await svc.CanManageEmployeeAsync(UserId, RoleConstants.Manager, EmployeeId);
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageEmployeeAsync_Manager_EmployeeInOtherLocation_ReturnsFalse()
    {
        var (svc, uow) = Build();
        var otherLocationId = Guid.NewGuid();

        var employee = new Employee { Id = EmployeeId, DepartmentId = DepartmentId };
        uow.Setup(u => u.Employees.GetByIdAsync(EmployeeId, false, default)).ReturnsAsync(employee);

        var department = new Department { Id = DepartmentId, LocationId = otherLocationId };
        uow.Setup(u => u.Departments.GetByIdAsync(DepartmentId, false, default)).ReturnsAsync(department);

        // manager only has LocationId, not otherLocationId
        var managedRow = new LocationManager { LocationId = LocationId, UserId = UserId };
        uow.Setup(u => u.LocationManagers.GetByUserAsync(UserId, default))
           .ReturnsAsync(new[] { managedRow });

        var result = await svc.CanManageEmployeeAsync(UserId, RoleConstants.Manager, EmployeeId);
        Assert.False(result);
    }

    [Fact]
    public async Task CanManageEmployeeAsync_Manager_EmployeeViaSecondaryMembership_ReturnsTrue()
    {
        // Primary dept → other location; secondary membership dept → manager's location → should grant access (BR-074)
        var (svc, uow) = Build();
        var otherLocationId = Guid.NewGuid();
        var secondaryDeptId = Guid.NewGuid();

        var employee = new Employee { Id = EmployeeId, DepartmentId = DepartmentId };
        uow.Setup(u => u.Employees.GetByIdAsync(EmployeeId, false, default)).ReturnsAsync(employee);

        // primary dept is in a location this manager does NOT own
        var primaryDept = new Department { Id = DepartmentId, LocationId = otherLocationId };
        uow.Setup(u => u.Departments.GetByIdAsync(DepartmentId, false, default)).ReturnsAsync(primaryDept);

        // secondary dept is in a location this manager DOES own
        var secondaryDept = new Department { Id = secondaryDeptId, LocationId = LocationId };
        uow.Setup(u => u.Departments.GetByIdAsync(secondaryDeptId, false, default)).ReturnsAsync(secondaryDept);

        var membership = new EmployeeDepartmentMembership { EmployeeId = EmployeeId, DepartmentId = secondaryDeptId };
        uow.Setup(u => u.EmployeeDepartmentMemberships.ListByEmployeeAsync(EmployeeId, default))
           .ReturnsAsync(new[] { membership });

        var managedRow = new LocationManager { LocationId = LocationId, UserId = UserId };
        uow.Setup(u => u.LocationManagers.GetByUserAsync(UserId, default))
           .ReturnsAsync(new[] { managedRow });

        var result = await svc.CanManageEmployeeAsync(UserId, RoleConstants.Manager, EmployeeId);
        Assert.True(result);
    }

    // ─── CanManageAttendanceAsync ────────────────────────────────────────────

    [Fact]
    public async Task CanManageAttendanceAsync_Admin_ReturnsTrue()
    {
        var (svc, _) = Build();
        var result = await svc.CanManageAttendanceAsync(UserId, RoleConstants.Admin, AttendanceId);
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageAttendanceAsync_NonManagerRole_ReturnsFalse()
    {
        var (svc, _) = Build();
        var result = await svc.CanManageAttendanceAsync(UserId, RoleConstants.User, AttendanceId);
        Assert.False(result);
    }

    [Fact]
    public async Task CanManageAttendanceAsync_Manager_RecordNotFound_ReturnsTrue()
    {
        var (svc, uow) = Build();
        uow.Setup(u => u.Attendance.GetByIdAsync(AttendanceId, false, default))
           .ReturnsAsync((AttendanceRecord?)null);

        var result = await svc.CanManageAttendanceAsync(UserId, RoleConstants.Manager, AttendanceId);
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageAttendanceAsync_Manager_AssignmentIdNull_ReturnsTrue()
    {
        // ad-hoc clock-in — no location to scope against
        var (svc, uow) = Build();
        var record = new AttendanceRecord { Id = AttendanceId, AssignmentId = null };
        uow.Setup(u => u.Attendance.GetByIdAsync(AttendanceId, false, default)).ReturnsAsync(record);

        var result = await svc.CanManageAttendanceAsync(UserId, RoleConstants.Manager, AttendanceId);
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageAttendanceAsync_Manager_AssignmentNotFound_ReturnsTrue()
    {
        var (svc, uow) = Build();
        var record = new AttendanceRecord { Id = AttendanceId, AssignmentId = AssignmentId };
        uow.Setup(u => u.Attendance.GetByIdAsync(AttendanceId, false, default)).ReturnsAsync(record);
        uow.Setup(u => u.ShiftAssignments.GetByIdAsync(AssignmentId, false, default))
           .ReturnsAsync((ShiftAssignment?)null);

        var result = await svc.CanManageAttendanceAsync(UserId, RoleConstants.Manager, AttendanceId);
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageAttendanceAsync_Manager_AssignmentInManagedLocation_ReturnsTrue()
    {
        var (svc, uow) = Build();

        var record = new AttendanceRecord { Id = AttendanceId, AssignmentId = AssignmentId };
        uow.Setup(u => u.Attendance.GetByIdAsync(AttendanceId, false, default)).ReturnsAsync(record);

        var assignment = new ShiftAssignment { Id = AssignmentId, ScheduleId = ScheduleId };
        uow.Setup(u => u.ShiftAssignments.GetByIdAsync(AssignmentId, false, default)).ReturnsAsync(assignment);

        var schedule = new Schedule { Id = ScheduleId, DepartmentId = DepartmentId };
        uow.Setup(u => u.Schedules.GetByIdAsync(ScheduleId, false, default)).ReturnsAsync(schedule);

        var department = new Department { Id = DepartmentId, LocationId = LocationId };
        uow.Setup(u => u.Departments.GetByIdAsync(DepartmentId, false, default)).ReturnsAsync(department);

        var managedRow = new LocationManager { LocationId = LocationId, UserId = UserId };
        uow.Setup(u => u.LocationManagers.GetByUserAsync(UserId, default))
           .ReturnsAsync(new[] { managedRow });

        var result = await svc.CanManageAttendanceAsync(UserId, RoleConstants.Manager, AttendanceId);
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageAttendanceAsync_Manager_AssignmentInOtherLocation_ReturnsFalse()
    {
        var (svc, uow) = Build();
        var otherLocationId = Guid.NewGuid();

        var record = new AttendanceRecord { Id = AttendanceId, AssignmentId = AssignmentId };
        uow.Setup(u => u.Attendance.GetByIdAsync(AttendanceId, false, default)).ReturnsAsync(record);

        var assignment = new ShiftAssignment { Id = AssignmentId, ScheduleId = ScheduleId };
        uow.Setup(u => u.ShiftAssignments.GetByIdAsync(AssignmentId, false, default)).ReturnsAsync(assignment);

        var schedule = new Schedule { Id = ScheduleId, DepartmentId = DepartmentId };
        uow.Setup(u => u.Schedules.GetByIdAsync(ScheduleId, false, default)).ReturnsAsync(schedule);

        var department = new Department { Id = DepartmentId, LocationId = otherLocationId };
        uow.Setup(u => u.Departments.GetByIdAsync(DepartmentId, false, default)).ReturnsAsync(department);

        // manager manages LocationId, assignment is in otherLocationId
        var managedRow = new LocationManager { LocationId = LocationId, UserId = UserId };
        uow.Setup(u => u.LocationManagers.GetByUserAsync(UserId, default))
           .ReturnsAsync(new[] { managedRow });

        var result = await svc.CanManageAttendanceAsync(UserId, RoleConstants.Manager, AttendanceId);
        Assert.False(result);
    }

    // ─── GetManagedLocationIdsAsync (pre-existing; sanity) ──────────────────

    [Fact]
    public async Task GetManagedLocationIdsAsync_Admin_ReturnsNull()
    {
        var (svc, _) = Build();
        var result = await svc.GetManagedLocationIdsAsync(UserId, RoleConstants.Admin);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetManagedLocationIdsAsync_NonManager_ReturnsEmptySet()
    {
        var (svc, _) = Build();
        var result = await svc.GetManagedLocationIdsAsync(UserId, RoleConstants.User);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetManagedLocationIdsAsync_Manager_ReturnsAssignedLocationIds()
    {
        var (svc, uow) = Build();
        var row = new LocationManager { LocationId = LocationId, UserId = UserId };
        uow.Setup(u => u.LocationManagers.GetByUserAsync(UserId, default))
           .ReturnsAsync(new[] { row });

        var result = await svc.GetManagedLocationIdsAsync(UserId, RoleConstants.Manager);
        Assert.NotNull(result);
        Assert.Contains(LocationId, result);
    }
}
