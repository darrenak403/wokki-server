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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
