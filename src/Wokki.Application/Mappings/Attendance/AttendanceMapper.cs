using Wokki.Application.Dtos.Attendance;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.Attendance;

public static class AttendanceMapper
{
    public static AttendanceResponse ToResponse(this AttendanceRecord record) =>
        new(
            record.Id,
            record.EmployeeId,
            record.AssignmentId,
            record.ClockIn,
            record.ClockOut,
            record.WorkedMinutes,
            record.AdjustedBy,
            record.AdjustmentNote,
            record.CreatedAt);
}
