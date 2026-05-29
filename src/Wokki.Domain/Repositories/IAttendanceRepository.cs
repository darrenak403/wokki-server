using Wokki.Domain.Entities;
using Wokki.Domain.Models;

namespace Wokki.Domain.Repositories;

public interface IAttendanceRepository
{
    Task<AttendanceRecord?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<AttendanceRecord?> GetByAssignmentIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<AttendanceRecord?> GetOpenByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AttendanceRecord> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? employeeId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> ListByEmployeeAsync(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> SumWorkedMinutesByEmployeeAsync(
        IEnumerable<Guid> employeeIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> SumApprovedOvertimeByEmployeeAsync(
        IEnumerable<Guid> employeeIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetAllOpenAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpenAttendanceDetail>> GetAllOpenWithShiftInfoAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecord>> GetManyByIdsAsync(IEnumerable<Guid> ids, bool track = false, CancellationToken cancellationToken = default);
    Task AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default);
    void Update(AttendanceRecord record);
}
