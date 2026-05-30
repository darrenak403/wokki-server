using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class ScheduleLeaveRequestRepository(AppDbContext context) : IScheduleLeaveRequestRepository
{
    public async Task<ScheduleLeaveRequest?> GetByIdAsync(
        Guid id,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        var query = track ? context.ScheduleLeaveRequests : context.ScheduleLeaveRequests.AsNoTracking();
        return await query.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsPendingForSlotAsync(
        Guid scheduleId,
        Guid employeeId,
        Guid shiftDefinitionId,
        DateOnly date,
        CancellationToken cancellationToken = default) =>
        await context.ScheduleLeaveRequests.AsNoTracking()
            .AnyAsync(r =>
                r.ScheduleId == scheduleId &&
                r.EmployeeId == employeeId &&
                r.ShiftDefinitionId == shiftDefinitionId &&
                r.Date == date &&
                r.Status == ScheduleLeaveRequestStatus.Pending,
                cancellationToken);

    public async Task<int> CountPendingByScheduleAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default) =>
        await context.ScheduleLeaveRequests.AsNoTracking()
            .CountAsync(r => r.ScheduleId == scheduleId && r.Status == ScheduleLeaveRequestStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<ScheduleLeaveRequest>> ListByEmployeeAsync(
        Guid employeeId,
        Guid? scheduleId,
        CancellationToken cancellationToken = default)
    {
        var query = context.ScheduleLeaveRequests.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId);

        if (scheduleId.HasValue)
            query = query.Where(r => r.ScheduleId == scheduleId.Value);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleLeaveRequest>> ListByScheduleAsync(
        Guid scheduleId,
        ScheduleLeaveRequestStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = context.ScheduleLeaveRequests.AsNoTracking()
            .Where(r => r.ScheduleId == scheduleId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ScheduleLeaveRequest request, CancellationToken cancellationToken = default) =>
        await context.ScheduleLeaveRequests.AddAsync(request, cancellationToken);

    public void Update(ScheduleLeaveRequest request) => context.ScheduleLeaveRequests.Update(request);

    public void Remove(ScheduleLeaveRequest request) => context.ScheduleLeaveRequests.Remove(request);
}
