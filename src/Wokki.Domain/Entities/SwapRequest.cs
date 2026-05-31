using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

// Deprecated: retained only for historical swap_requests rows. New shift trades use SwapPost.
public class SwapRequest
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RequesterAssignmentId { get; set; }
    public Guid TargetAssignmentId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid TargetEmployeeId { get; set; }
    public SwapStatus Status { get; set; } = SwapStatus.Pending;
    public string? RequesterNote { get; set; }
    public string? TargetNote { get; set; }
    public string? ManagerNote { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
