using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Models;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class AttendanceRepository(AppDbContext context) : IAttendanceRepository
{
    public async Task<AttendanceRecord?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.AttendanceRecords : context.AttendanceRecords.AsNoTracking();
        return await query.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<AttendanceRecord?> GetByAssignmentIdAsync(Guid assignmentId, CancellationToken cancellationToken = default) =>
        await context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId, cancellationToken);

    public async Task<AttendanceRecord?> GetOpenByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.ClockOut == null, cancellationToken);

    public async Task<(IReadOnlyList<AttendanceRecord> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        Guid? employeeId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        IReadOnlySet<Guid>? locationIds = null,
        AttendanceMode? mode = null,
        bool? payrollEligible = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.AttendanceRecords.AsNoTracking().AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(a => a.OrganizationId == organizationId.Value);

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        if (fromDate.HasValue)
        {
            var from = new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            query = query.Where(a => a.ClockIn >= from);
        }

        if (toDate.HasValue)
        {
            var to = new DateTimeOffset(toDate.Value.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc));
            query = query.Where(a => a.ClockIn <= to);
        }

        if (mode.HasValue)
            query = query.Where(a => a.Mode == mode.Value);

        if (payrollEligible.HasValue)
            query = query.Where(a => a.PayrollEligible == payrollEligible.Value);

        if (locationIds is not null)
        {
            var allowedLocationIds = locationIds.ToArray();
            query = allowedLocationIds.Length == 0
                ? query.Where(_ => false)
                : query.Where(a =>
                    a.AssignmentId != null &&
                    context.ShiftAssignments.Any(sa =>
                        sa.Id == a.AssignmentId.Value &&
                        context.Schedules.Any(sc =>
                            sc.Id == sa.ScheduleId &&
                            context.Departments.Any(d =>
                                d.Id == sc.DepartmentId && allowedLocationIds.Contains(d.LocationId)))));
        }

        query = query.OrderByDescending(a => a.ClockIn);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> ListByEmployeeAsync(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.AttendanceRecords.AsNoTracking().Where(a => a.EmployeeId == employeeId);

        if (fromDate.HasValue)
        {
            var from = new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            query = query.Where(a => a.ClockIn >= from);
        }

        if (toDate.HasValue)
        {
            var to = new DateTimeOffset(toDate.Value.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc));
            query = query.Where(a => a.ClockIn <= to);
        }

        return await query.OrderByDescending(a => a.ClockIn).ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> SumWorkedMinutesByEmployeeAsync(
        IEnumerable<Guid> employeeIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, int>();

        var from = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var to = new DateTimeOffset(endDate.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc));

        var rows = await context.AttendanceRecords.AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId)
                        && a.ClockOut != null
                        && a.AssignmentId != null
                        && a.Mode == AttendanceMode.Assignment
                        && a.ClockIn >= from
                        && a.ClockIn <= to)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { g.Key, Total = g.Sum(x => x.WorkedMinutes) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Key, x => x.Total);
    }

    public async Task<Dictionary<Guid, int>> SumApprovedOvertimeByEmployeeAsync(
        IEnumerable<Guid> employeeIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, int>();

        var from = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var to = new DateTimeOffset(endDate.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc));

        var rows = await context.AttendanceRecords.AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId)
                        && a.ClockOut != null
                        && a.AssignmentId != null
                        && a.Mode == AttendanceMode.Assignment
                        && a.ClockIn >= from
                        && a.ClockIn <= to
                        && a.ApprovedOvertimeMinutes > 0)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { g.Key, Total = g.Sum(x => x.ApprovedOvertimeMinutes) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Key, x => x.Total);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetAllOpenAsync(CancellationToken cancellationToken = default) =>
        await context.AttendanceRecords
            .Where(ar => ar.ClockOut == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AttendanceRecord>> GetManyByIdsAsync(IEnumerable<Guid> ids, bool track = false, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        var query = track ? context.AttendanceRecords : context.AttendanceRecords.AsNoTracking();
        return await query.Where(a => idList.Contains(a.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpenAttendanceDetail>> GetAllOpenWithShiftInfoAsync(CancellationToken cancellationToken = default) =>
        await (from ar in context.AttendanceRecords
               where ar.ClockOut == null && ar.AssignmentId != null
               join sa in context.ShiftAssignments on ar.AssignmentId equals sa.Id
               join sd in context.ShiftDefinitions on sa.ShiftDefinitionId equals sd.Id
               join sc in context.Schedules on sa.ScheduleId equals sc.Id
               join dept in context.Departments on sc.DepartmentId equals dept.Id
               join loc in context.Locations on dept.LocationId equals loc.Id
               select new OpenAttendanceDetail(
                   ar.Id,
                   ar.EmployeeId,
                   ar.ClockIn,
                   sa.Date,
                   sd.StartTime,
                   sd.EndTime,
                   loc.TimeZone))
              .AsNoTracking()
              .ToListAsync(cancellationToken);

    public async Task AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default) =>
        await context.AttendanceRecords.AddAsync(record, cancellationToken);

    public void Update(AttendanceRecord record) => context.AttendanceRecords.Update(record);
}
