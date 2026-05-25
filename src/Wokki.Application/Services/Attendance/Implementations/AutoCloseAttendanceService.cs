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
        var openRecords = await unitOfWork.Attendance.GetAllOpenAsync(ct);
        if (openRecords.Count == 0)
            return;

        var cutoff = DateTimeOffset.UtcNow - IAutoCloseAttendanceService.GracePeriod;
        var hasChanges = false;

        foreach (var record in openRecords)
        {
            if (record.AssignmentId is null)
                continue;

            var assignment = await unitOfWork.ShiftAssignments.GetByIdAsync(record.AssignmentId.Value, cancellationToken: ct);
            if (assignment is null)
                continue;

            var shift = await unitOfWork.ShiftDefinitions.GetByIdAsync(assignment.ShiftDefinitionId, cancellationToken: ct);
            if (shift is null)
                continue;

            var schedule = await unitOfWork.Schedules.GetByIdAsync(assignment.ScheduleId, cancellationToken: ct);
            var department = schedule is null ? null : await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: ct);
            var location = department is null ? null : await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: ct);

            var tz = ResolveTimeZone(location?.TimeZone ?? "UTC");
            var shiftEndLocal = assignment.Date.ToDateTime(shift.EndTime, DateTimeKind.Unspecified);
            var shiftEndUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(shiftEndLocal, tz));

            if (shiftEndUtc >= cutoff)
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
