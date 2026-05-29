using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Domain.Repositories;

public interface IOvertimeRequestRepository
{
    Task<OvertimeRequest?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<OvertimeRequest?> GetActiveByShiftAndEmployeeAsync(Guid shiftAssignmentId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<OvertimeRequest> Items, int TotalCount)> ListByEmployeeAsync(
        Guid employeeId,
        Guid? shiftAssignmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<OvertimeRequest> Items, int TotalCount)> ListPendingApprovalAsync(
        IReadOnlyList<Guid>? allowedEmployeeIds,
        Guid? departmentId,
        int page,
        int pageSize,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<(OvertimeRequest Request, string EmployeeFirstName, string EmployeeLastName, string? ShiftName, DateOnly? ScheduledDate)> Items, int TotalCount)>
        ListAllByDepartmentAsync(
            IReadOnlyList<Guid>? allowedEmployeeIds,
            Guid? departmentId,
            int month,
            int year,
            int page,
            int pageSize,
            IReadOnlySet<Guid>? locationIds = null,
            CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OvertimeRequest>> GetExpiredPendingAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
    Task AddAsync(OvertimeRequest request, CancellationToken cancellationToken = default);
    void Update(OvertimeRequest request);
}
