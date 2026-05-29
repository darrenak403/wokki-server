using Microsoft.EntityFrameworkCore.Storage;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IOrganizationRepository? _organizations;
    private IUserRepository? _users;
    private IEmployeeRepository? _employees;
    private ILocationRepository? _locations;
    private IDepartmentRepository? _departments;
    private IShiftDefinitionRepository? _shiftDefinitions;
    private IScheduleRepository? _schedules;
    private IShiftAssignmentRepository? _shiftAssignments;
    private ISwapRequestRepository? _swapRequests;
    private IAttendanceRepository? _attendance;
    private IPayPeriodRepository? _payPeriods;
    private IPayrollLineRepository? _payrollLines;
    private IChannelRepository? _channels;
    private IMessageRepository? _messages;
    private IEmployeeAvailabilityRepository? _employeeAvailabilities;
    private ILocationSchedulingPolicyRepository? _locationSchedulingPolicies;
    private IEmployeeDepartmentMembershipRepository? _employeeDepartmentMemberships;
    private ISchedulePreferenceRepository? _schedulePreferences;
    private IScheduleInsightContextRepository? _scheduleInsightContexts;
    private IOvertimeRequestRepository? _overtimeRequests;
    private ILocationMembershipRepository? _locationMemberships;
    private ILocationManagerRepository? _locationManagers;
    private IDbContextTransaction? _transaction;

    public IOrganizationRepository Organizations => _organizations ??= new OrganizationRepository(context);
    public IUserRepository Users => _users ??= new UserRepository(context);
    public IEmployeeRepository Employees => _employees ??= new EmployeeRepository(context);
    public ILocationRepository Locations => _locations ??= new LocationRepository(context);
    public IDepartmentRepository Departments => _departments ??= new DepartmentRepository(context);
    public IShiftDefinitionRepository ShiftDefinitions => _shiftDefinitions ??= new ShiftDefinitionRepository(context);
    public IScheduleRepository Schedules => _schedules ??= new ScheduleRepository(context);
    public IShiftAssignmentRepository ShiftAssignments => _shiftAssignments ??= new ShiftAssignmentRepository(context);
    public ISwapRequestRepository SwapRequests => _swapRequests ??= new SwapRequestRepository(context);
    public IAttendanceRepository Attendance => _attendance ??= new AttendanceRepository(context);
    public IPayPeriodRepository PayPeriods => _payPeriods ??= new PayPeriodRepository(context);
    public IPayrollLineRepository PayrollLines => _payrollLines ??= new PayrollLineRepository(context);
    public IChannelRepository Channels => _channels ??= new ChannelRepository(context);
    public IMessageRepository Messages => _messages ??= new MessageRepository(context);
    public IEmployeeAvailabilityRepository EmployeeAvailabilities =>
        _employeeAvailabilities ??= new EmployeeAvailabilityRepository(context);
    public ILocationSchedulingPolicyRepository LocationSchedulingPolicies =>
        _locationSchedulingPolicies ??= new LocationSchedulingPolicyRepository(context);
    public IEmployeeDepartmentMembershipRepository EmployeeDepartmentMemberships =>
        _employeeDepartmentMemberships ??= new EmployeeDepartmentMembershipRepository(context);
    public ISchedulePreferenceRepository SchedulePreferences =>
        _schedulePreferences ??= new SchedulePreferenceRepository(context);
    public IScheduleInsightContextRepository ScheduleInsightContexts =>
        _scheduleInsightContexts ??= new ScheduleInsightContextRepository(context);
    public IOvertimeRequestRepository OvertimeRequests =>
        _overtimeRequests ??= new OvertimeRequestRepository(context);
    public ILocationMembershipRepository LocationMemberships =>
        _locationMemberships ??= new LocationMembershipRepository(context);
    public ILocationManagerRepository LocationManagers =>
        _locationManagers ??= new LocationManagerRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            return;

        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();

        await context.DisposeAsync();
    }
}
