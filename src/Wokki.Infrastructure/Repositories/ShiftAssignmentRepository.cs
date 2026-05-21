using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class ShiftAssignmentRepository(AppDbContext context) : IShiftAssignmentRepository
{
    public async Task<ShiftAssignment?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.ShiftAssignments : context.ShiftAssignments.AsNoTracking();
        return await query.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public void Update(ShiftAssignment assignment) => context.ShiftAssignments.Update(assignment);

    public async Task<IReadOnlyList<ShiftAssignment>> ListByScheduleAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default) =>
        await context.ShiftAssignments.AsNoTracking()
            .Where(a => a.ScheduleId == scheduleId)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShiftAssignment>> ListPublishedByDepartmentInDateRangeAsync(
        Guid departmentId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default) =>
        await (
            from a in context.ShiftAssignments.AsNoTracking()
            join s in context.Schedules.AsNoTracking() on a.ScheduleId equals s.Id
            where s.DepartmentId == departmentId
                  && s.Status == ScheduleStatus.Published
                  && a.Date >= fromDate
                  && a.Date <= toDate
            select a
        ).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShiftAssignment>> ListByEmployeeInDateRangeAsync(
        Guid employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default) =>
        await (
            from a in context.ShiftAssignments.AsNoTracking()
            join s in context.Schedules.AsNoTracking() on a.ScheduleId equals s.Id
            where a.EmployeeId == employeeId
                  && a.Date >= fromDate
                  && a.Date <= toDate
                  && s.Status == ScheduleStatus.Published
            orderby a.Date
            select a
        ).ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        Guid scheduleId,
        Guid shiftDefinitionId,
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default) =>
        await context.ShiftAssignments.AnyAsync(
            a => a.ScheduleId == scheduleId
                 && a.ShiftDefinitionId == shiftDefinitionId
                 && a.EmployeeId == employeeId
                 && a.Date == date,
            cancellationToken);

    public async Task<bool> HasTimeOverlapAsync(
        Guid scheduleId,
        Guid employeeId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeAssignmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query =
            from a in context.ShiftAssignments.AsNoTracking()
            join d in context.ShiftDefinitions.AsNoTracking() on a.ShiftDefinitionId equals d.Id
            where a.ScheduleId == scheduleId
                  && a.EmployeeId == employeeId
                  && a.Date == date
                  && (excludeAssignmentId == null || a.Id != excludeAssignmentId.Value)
            select new { d.StartTime, d.EndTime };

        var existing = await query.ToListAsync(cancellationToken);
        return existing.Any(x => TimeRangesOverlap(startTime, endTime, x.StartTime, x.EndTime));
    }

    public async Task AddAsync(ShiftAssignment assignment, CancellationToken cancellationToken = default) =>
        await context.ShiftAssignments.AddAsync(assignment, cancellationToken);

    public void Remove(ShiftAssignment assignment) => context.ShiftAssignments.Remove(assignment);

    private static bool TimeRangesOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2) =>
        start1 < end2 && end1 > start2;
}
