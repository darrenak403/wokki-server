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

        if (departmentId.HasValue)
            query = query.Where(r =>
                context.ShiftAssignments.Any(sa =>
                    sa.Id == r.ShiftAssignmentId &&
                    context.Schedules.Any(sc =>
                        sc.Id == sa.ScheduleId &&
                        sc.DepartmentId == departmentId.Value)));

        query = query.OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<(OvertimeRequest Request, string EmployeeFirstName, string EmployeeLastName, string? ShiftName, DateOnly? ScheduledDate)> Items, int TotalCount)>
        ListAllByDepartmentAsync(
            IReadOnlyList<Guid>? allowedEmployeeIds,
            Guid? departmentId,
            int month,
            int year,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        var query = from r in context.OvertimeRequests
                    join e in context.Employees on r.EmployeeId equals e.Id
                    join sa in context.ShiftAssignments on r.ShiftAssignmentId equals sa.Id
                    join sd in context.ShiftDefinitions on sa.ShiftDefinitionId equals sd.Id into sdGroup
                    from sd in sdGroup.DefaultIfEmpty()
                    select new { r, e, sa, sd };

        query = query.Where(x => x.r.StartedAt.Month == month && x.r.StartedAt.Year == year);

        if (allowedEmployeeIds is not null)
            query = query.Where(x => allowedEmployeeIds.Contains(x.r.EmployeeId));

        if (departmentId.HasValue)
            query = query.Where(x => context.Schedules.Any(sc => sc.Id == x.sa.ScheduleId && sc.DepartmentId == departmentId.Value));

        query = query.OrderByDescending(x => x.r.StartedAt);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var items = rows
            .Select(x => (
                Request: x.r,
                EmployeeFirstName: x.e.FirstName,
                EmployeeLastName: x.e.LastName,
                ShiftName: (string?)x.sd?.Name,
                ScheduledDate: (DateOnly?)x.sa.Date))
            .ToList();

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
