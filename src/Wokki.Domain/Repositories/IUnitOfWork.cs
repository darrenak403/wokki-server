namespace Wokki.Domain.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IOrganizationRepository Organizations { get; }
    IUserRepository Users { get; }
    IEmployeeRepository Employees { get; }
    ILocationRepository Locations { get; }
    IDepartmentRepository Departments { get; }
    IShiftDefinitionRepository ShiftDefinitions { get; }
    IScheduleRepository Schedules { get; }
    IShiftAssignmentRepository ShiftAssignments { get; }
    ISwapRequestRepository SwapRequests { get; }
    ISwapPostRepository SwapPosts { get; }
    IAuditLogRepository AuditLogs { get; }
    IAttendanceRepository Attendance { get; }
    IPayPeriodRepository PayPeriods { get; }
    IPayrollLineRepository PayrollLines { get; }
    IChannelRepository Channels { get; }
    IMessageRepository Messages { get; }
    IEmployeeAvailabilityRepository EmployeeAvailabilities { get; }
    IOrganizationSchedulingPolicyRepository OrganizationSchedulingPolicies { get; }
    IEmployeeDepartmentMembershipRepository EmployeeDepartmentMemberships { get; }
    ISchedulePreferenceRepository SchedulePreferences { get; }
    IScheduleInsightContextRepository ScheduleInsightContexts { get; }
    IScheduleLeaveRequestRepository ScheduleLeaveRequests { get; }
    IOvertimeRequestRepository OvertimeRequests { get; }
    ILocationMembershipRepository LocationMemberships { get; }
    ILocationManagerRepository LocationManagers { get; }
    IOrgJoinRequestRepository OrgJoinRequests { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
