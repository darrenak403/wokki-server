using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? AssignmentId { get; set; }
    public AttendanceMode Mode { get; set; } = AttendanceMode.Assignment;
    public bool PayrollEligible { get; set; } = true;
    public Guid? EligibleMarkedBy { get; set; }
    public DateTime? EligibleMarkedAt { get; set; }
    public DateTimeOffset ClockIn { get; set; }
    public DateTimeOffset? ClockOut { get; set; }
    public int WorkedMinutes { get; set; }
    public bool AutoClosed { get; set; } = false;
    public int ApprovedOvertimeMinutes { get; set; } = 0;
    public Guid? AdjustedBy { get; set; }
    public string? AdjustmentNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
