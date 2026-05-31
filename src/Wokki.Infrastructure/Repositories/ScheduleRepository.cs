using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class ScheduleRepository(AppDbContext context) : IScheduleRepository
{
    public async Task<Schedule?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.Schedules : context.Schedules.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Schedule?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Schedules
            .FromSqlInterpolated($"""SELECT * FROM schedules WHERE "Id" = {id} FOR UPDATE""")
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Schedule?> GetByDepartmentAndWeekAsync(
        Guid departmentId,
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default) =>
        await context.Schedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.DepartmentId == departmentId && s.WeekStartDate == weekStartDate, cancellationToken);

    public async Task<(IReadOnlyList<Schedule> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        Guid? departmentId = null,
        DateOnly? weekStartDate = null,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Schedules.AsNoTracking().AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(s => s.OrganizationId == organizationId.Value);

        if (departmentId.HasValue)
            query = query.Where(s => s.DepartmentId == departmentId.Value);

        if (locationIds is not null)
        {
            var allowedLocationIds = locationIds.ToArray();
            query = allowedLocationIds.Length == 0
                ? query.Where(_ => false)
                : query.Where(s => context.Departments.Any(d =>
                    d.Id == s.DepartmentId && allowedLocationIds.Contains(d.LocationId)));
        }

        if (weekStartDate.HasValue)
            query = query.Where(s => s.WeekStartDate == weekStartDate.Value);

        query = query.OrderByDescending(s => s.WeekStartDate);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default) =>
        await context.Schedules.AddAsync(schedule, cancellationToken);

    public void Update(Schedule schedule) => context.Schedules.Update(schedule);

    public void Remove(Schedule schedule) => context.Schedules.Remove(schedule);
}
