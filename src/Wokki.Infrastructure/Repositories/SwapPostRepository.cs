using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class SwapPostRepository(AppDbContext context) : ISwapPostRepository
{
    public async Task<SwapPost?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.SwapPosts : context.SwapPosts.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<SwapPost?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.SwapPosts
            .FromSqlInterpolated($"""SELECT sp.*, sp.xmin FROM swap_posts AS sp WHERE sp."Id" = {id} FOR UPDATE""")
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(IReadOnlyList<SwapPost> Items, int TotalCount)> ListFeedAsync(
        Guid scheduleId,
        Guid departmentId,
        Guid locationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.SwapPosts.AsNoTracking()
            .Where(p =>
                p.ScheduleId == scheduleId &&
                p.DepartmentId == departmentId &&
                p.LocationId == locationId &&
                p.Status == SwapPostStatus.Pending &&
                context.Schedules.Any(s => s.Id == p.ScheduleId && s.Status == ScheduleStatus.Draft));

        query = query.OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<SwapPost> Items, int TotalCount)> ListByEmployeeAsync(
        Guid employeeId,
        Guid? scheduleId = null,
        SwapPostStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = context.SwapPosts.AsNoTracking()
            .Where(p => p.AuthorEmployeeId == employeeId || p.AcceptedByEmployeeId == employeeId);

        if (scheduleId.HasValue)
            query = query.Where(p => p.ScheduleId == scheduleId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        query = query.OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<bool> HasPendingForAuthorAssignmentAsync(
        Guid authorAssignmentId,
        Guid? excludePostId = null,
        CancellationToken cancellationToken = default) =>
        await context.SwapPosts.AsNoTracking()
            .AnyAsync(p =>
                p.AuthorAssignmentId == authorAssignmentId &&
                p.Status == SwapPostStatus.Pending &&
                (excludePostId == null || p.Id != excludePostId.Value),
                cancellationToken);

    public async Task<bool> HasPendingForAcceptorAssignmentAsync(
        Guid acceptorAssignmentId,
        CancellationToken cancellationToken = default) =>
        await context.SwapPosts.AsNoTracking()
            .AnyAsync(p =>
                p.Status == SwapPostStatus.Pending &&
                p.AuthorAssignmentId == acceptorAssignmentId,
                cancellationToken);

    public async Task<(IReadOnlyList<SwapPost> Items, int TotalCount)> ListAuditAsync(
        Guid organizationId,
        IReadOnlySet<Guid>? locationIds,
        Guid? scheduleId,
        DateOnly? weekStartDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.SwapPosts.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.Status == SwapPostStatus.Completed);

        if (locationIds is not null)
        {
            var allowed = locationIds.ToArray();
            query = allowed.Length == 0
                ? query.Where(_ => false)
                : query.Where(p => allowed.Contains(p.LocationId));
        }

        if (scheduleId.HasValue)
            query = query.Where(p => p.ScheduleId == scheduleId.Value);

        if (weekStartDate.HasValue)
            query = query.Where(p =>
                context.Schedules.Any(s => s.Id == p.ScheduleId && s.WeekStartDate == weekStartDate.Value));

        query = query.OrderByDescending(p => p.CompletedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(SwapPost swapPost, CancellationToken cancellationToken = default) =>
        await context.SwapPosts.AddAsync(swapPost, cancellationToken);

    public void Update(SwapPost swapPost) => context.SwapPosts.Update(swapPost);

    public async Task<int> HidePendingByScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        await context.SwapPosts
            .Where(p => p.ScheduleId == scheduleId && p.Status == SwapPostStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.Status, SwapPostStatus.Hidden)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
}
