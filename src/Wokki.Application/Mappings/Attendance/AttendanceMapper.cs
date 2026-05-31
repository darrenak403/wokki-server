using Wokki.Application.Dtos.Attendance;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Application.Mappings.Attendance;

public static class AttendanceMapper
{
    public static AttendanceResponse ToResponse(
        this AttendanceRecord record,
        ShiftAssignment? assignment = null,
        ShiftDefinition? shift = null,
        Department? department = null,
        Location? location = null,
        AttendanceStatus? status = null) =>
        new(
            record.Id,
            record.EmployeeId,
            record.AssignmentId,
            record.Mode,
            record.PayrollEligible,
            record.ApprovedOvertimeMinutes,
            record.ClockIn,
            record.ClockOut,
            record.WorkedMinutes,
            record.AutoClosed,
            status ?? (record.ClockOut is null ? AttendanceStatus.Open : AttendanceStatus.OnTime),
            record.AdjustedBy,
            record.AdjustmentNote,
            record.CreatedAt,
            shift?.Id,
            shift?.Name,
            shift?.Color,
            assignment?.Date,
            shift?.StartTime,
            shift?.EndTime,
            department?.Id,
            department?.Name,
            location?.Id,
            location?.Name);
}
