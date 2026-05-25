using Wokki.Application.Services.Attendance.Interfaces;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Attendance.Implementations;

public sealed class AutoCloseAttendanceService(IUnitOfWork unitOfWork) : IAutoCloseAttendanceService
{
    public async Task AutoCloseIfExpiredAsync(AttendanceRecord record, CancellationToken ct = default)
    {
        if (record.ClockOut is not null || record.AssignmentId is null)
            return;

        var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(record.AssignmentId.Value, cancellationToken: ct);
        if (assignment is null)
            return;

        var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(assignment.ShiftDefinitionId, cancellationToken: ct);
        if (shift is null)
            return;

        var schedule = await unitOfWork.Schedules.GetByIdAsync(assignment.ScheduleId, cancellationToken: ct);
        var department = schedule is null ? null : await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: ct);
        var location = department is null ? null : await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: ct);

        var tz = ResolveTimeZone(location?.TimeZone ?? "UTC");
        var shiftEndLocal = assignment.Date.ToDateTime(shift.EndTime, DateTimeKind.Unspecified);
        var shiftEndUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(shiftEndLocal, tz));

        if (DateTimeOffset.UtcNow <= shiftEndUtc.Add(IAutoCloseAttendanceService.GracePeriod))
            return;

        record.ClockOut = shiftEndUtc;
        record.AutoClosed = true;
        record.WorkedMinutes = (int)Math.Max(0, (shiftEndUtc - record.ClockIn).TotalMinutes);
        unitOfWork.Attendance.Update(record);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task BulkAutoCloseExpiredAsync(CancellationToken ct = default)
    {
        var details = await unitOfWork.Attendance.GetAllOpenWithShiftInfoAsync(ct);
        if (details.Count == 0)
            return;

        var cutoff = DateTimeOffset.UtcNow - IAutoCloseAttendanceService.GracePeriod;

        // Compute shift-end per record (all in-memory, no extra queries)
        var toClose = new Dictionary<Guid, DateTimeOffset>();
        foreach (var detail in details)
        {
            var tz = ResolveTimeZone(detail.LocationTimeZone ?? "UTC");
            var shiftEndLocal = detail.AssignmentDate.ToDateTime(detail.ShiftEndTime, DateTimeKind.Unspecified);
            // Overnight shift: end time is before start time, so shift ends the next day
            if (detail.ShiftEndTime < detail.ShiftStartTime)
                shiftEndLocal = shiftEndLocal.AddDays(1);
            var shiftEndUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(shiftEndLocal, tz));
            if (shiftEndUtc < cutoff)
                toClose[detail.RecordId] = shiftEndUtc;
        }

        if (toClose.Count == 0)
            return;

        // Batch-load only the records that need closing (single WHERE id IN (...) query)
        var tracked = await unitOfWork.Attendance.GetManyByIdsAsync(toClose.Keys, track: true, cancellationToken: ct);
        var hasChanges = false;
        foreach (var record in tracked)
        {
            if (record.ClockOut is not null || !toClose.TryGetValue(record.Id, out var shiftEndUtc))
                continue;

            record.ClockOut = shiftEndUtc;
            record.AutoClosed = true;
            record.WorkedMinutes = (int)Math.Max(0, (shiftEndUtc - record.ClockIn).TotalMinutes);
            unitOfWork.Attendance.Update(record);
            hasChanges = true;
        }

        if (hasChanges)
            await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task BulkAutoCloseOTSessionsAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - IAutoCloseAttendanceService.MaxOTDuration;
        var expired = await unitOfWork.OvertimeRequests.GetExpiredPendingAsync(cutoff, ct);
        if (expired.Count == 0)
            return;

        foreach (var request in expired)
        {
            request.Status = OvertimeStatus.AutoClosed;
            request.EndedAt = request.StartedAt.Add(IAutoCloseAttendanceService.MaxOTDuration);
            request.OvertimeMinutes = (int)IAutoCloseAttendanceService.MaxOTDuration.TotalMinutes;
            unitOfWork.OvertimeRequests.Update(request);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}
