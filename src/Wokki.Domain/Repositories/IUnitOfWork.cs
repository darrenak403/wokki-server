namespace Wokki.Domain.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IEmployeeRepository Employees { get; }
    ILocationRepository Locations { get; }
    IDepartmentRepository Departments { get; }
    IShiftDefinitionRepository ShiftDefinitions { get; }
    IScheduleRepository Schedules { get; }
    IShiftAssignmentRepository ShiftAssignments { get; }
    ISwapRequestRepository SwapRequests { get; }
    IAttendanceRepository Attendance { get; }
    IPayPeriodRepository PayPeriods { get; }
    IPayrollLineRepository PayrollLines { get; }
    IChannelRepository Channels { get; }
    IMessageRepository Messages { get; }
    IEmployeeAvailabilityRepository EmployeeAvailabilities { get; }
    IJobPositionRepository JobPositions { get; }
    ILocationSchedulingPolicyRepository LocationSchedulingPolicies { get; }
    IEmployeeDepartmentMembershipRepository EmployeeDepartmentMemberships { get; }
    ISchedulePreferenceRepository SchedulePreferences { get; }
    IScheduleInsightContextRepository ScheduleInsightContexts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
