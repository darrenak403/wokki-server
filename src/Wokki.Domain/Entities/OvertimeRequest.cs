using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class OvertimeRequest
{
    public Guid Id { get; set; }
    public Guid ShiftAssignmentId { get; set; }
    public ShiftAssignment ShiftAssignment { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public int? OvertimeMinutes { get; set; }
    public OvertimeStatus Status { get; set; } = OvertimeStatus.Pending;
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
