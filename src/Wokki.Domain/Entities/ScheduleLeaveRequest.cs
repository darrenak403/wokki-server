using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class ScheduleLeaveRequest
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ShiftDefinitionId { get; set; }
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ScheduleLeaveRequestStatus Status { get; set; } = ScheduleLeaveRequestStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
