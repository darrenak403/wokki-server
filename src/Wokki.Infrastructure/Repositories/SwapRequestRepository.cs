using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class SwapRequestRepository(AppDbContext context) : ISwapRequestRepository
{
    public async Task<SwapRequest?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.SwapRequests : context.SwapRequests.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<SwapRequest> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        SwapStatus? status = null,
        Guid? departmentId = null,
        DateOnly? weekStartDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.SwapRequests.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (departmentId.HasValue || weekStartDate.HasValue)
        {
            query = query.Where(s =>
                context.ShiftAssignments.Any(ra =>
                    ra.Id == s.RequesterAssignmentId &&
                    context.Schedules.Any(sch =>
                        sch.Id == ra.ScheduleId &&
                        (!departmentId.HasValue || sch.DepartmentId == departmentId.Value) &&
                        (!weekStartDate.HasValue || sch.WeekStartDate == weekStartDate.Value))));
        }

        query = query.OrderByDescending(s => s.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<SwapRequest>> ListByEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await context.SwapRequests.AsNoTracking()
            .Where(s => s.RequesterId == employeeId || s.TargetEmployeeId == employeeId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasOpenSwapForAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        await context.SwapRequests.AnyAsync(
            s => (s.RequesterAssignmentId == assignmentId || s.TargetAssignmentId == assignmentId)
                 && s.Status == SwapStatus.Pending,
            cancellationToken);

    public async Task<bool> HasPeerAcceptedForAssignmentAsync(
        Guid assignmentId,
        Guid? excludeSwapId = null,
        CancellationToken cancellationToken = default) =>
        await context.SwapRequests.AnyAsync(
            s => (s.RequesterAssignmentId == assignmentId || s.TargetAssignmentId == assignmentId)
                 && s.Status == SwapStatus.PeerAccepted
                 && (excludeSwapId == null || s.Id != excludeSwapId.Value),
            cancellationToken);

    public async Task AddAsync(SwapRequest swapRequest, CancellationToken cancellationToken = default) =>
        await context.SwapRequests.AddAsync(swapRequest, cancellationToken);

    public void Update(SwapRequest swapRequest) => context.SwapRequests.Update(swapRequest);
}
