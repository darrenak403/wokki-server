using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Domain.Repositories;

public interface IScheduleLeaveRequestRepository
{
    Task<ScheduleLeaveRequest?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);

    Task<bool> ExistsPendingForSlotAsync(
        Guid scheduleId,
        Guid employeeId,
        Guid shiftDefinitionId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingByScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleLeaveRequest>> ListByEmployeeAsync(
        Guid employeeId,
        Guid? scheduleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleLeaveRequest>> ListByScheduleAsync(
        Guid scheduleId,
        ScheduleLeaveRequestStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(ScheduleLeaveRequest request, CancellationToken cancellationToken = default);

    void Update(ScheduleLeaveRequest request);

    void Remove(ScheduleLeaveRequest request);
}
