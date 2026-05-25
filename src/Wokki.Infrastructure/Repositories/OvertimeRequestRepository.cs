using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class OvertimeRequestRepository(AppDbContext context) : IOvertimeRequestRepository
{
    public async Task<OvertimeRequest?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.OvertimeRequests : context.OvertimeRequests.AsNoTracking();
        return await query.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<OvertimeRequest?> GetActiveByShiftAndEmployeeAsync(
        Guid shiftAssignmentId,
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await context.OvertimeRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.ShiftAssignmentId == shiftAssignmentId &&
                r.EmployeeId == employeeId &&
                (r.Status == OvertimeStatus.Pending || r.Status == OvertimeStatus.PendingApproval),
                cancellationToken);

    public async Task<(IReadOnlyList<OvertimeRequest> Items, int TotalCount)> ListByEmployeeAsync(
        Guid employeeId,
        Guid? shiftAssignmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.OvertimeRequests.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId);

        if (shiftAssignmentId.HasValue)
            query = query.Where(r => r.ShiftAssignmentId == shiftAssignmentId.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<OvertimeRequest> Items, int TotalCount)> ListPendingApprovalAsync(
        IReadOnlyList<Guid>? allowedEmployeeIds,
        Guid? departmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.OvertimeRequests.AsNoTracking()
            .Where(r => r.Status == OvertimeStatus.PendingApproval || r.Status == OvertimeStatus.AutoClosed);

        if (allowedEmployeeIds is not null)
            query = query.Where(r => allowedEmployeeIds.Contains(r.EmployeeId));

        query = query.OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<OvertimeRequest>> GetExpiredPendingAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default) =>
        await context.OvertimeRequests
            .Where(r => r.Status == OvertimeStatus.Pending && r.StartedAt < cutoff)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(OvertimeRequest request, CancellationToken cancellationToken = default) =>
        await context.OvertimeRequests.AddAsync(request, cancellationToken);

    public void Update(OvertimeRequest request) => context.OvertimeRequests.Update(request);
}
