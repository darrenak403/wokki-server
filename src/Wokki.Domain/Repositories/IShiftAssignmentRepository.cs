using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IShiftAssignmentRepository
{
    Task<ShiftAssignment?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftAssignment>> ListByScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftAssignment>> ListByEmployeeInDateRangeAsync(
        Guid employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(
        Guid scheduleId,
        Guid shiftDefinitionId,
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default);
    Task<bool> HasTimeOverlapAsync(
        Guid scheduleId,
        Guid employeeId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeAssignmentId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(ShiftAssignment assignment, CancellationToken cancellationToken = default);
    void Update(ShiftAssignment assignment);
    void Remove(ShiftAssignment assignment);
}
