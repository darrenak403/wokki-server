using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class SwapPost
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid LocationId { get; set; }
    public Guid AuthorEmployeeId { get; set; }
    public Guid AuthorAssignmentId { get; set; }
    public SwapPostType Type { get; set; }
    public SwapPostStatus Status { get; set; } = SwapPostStatus.Pending;
    public string? Note { get; set; }
    public Guid? AcceptedByEmployeeId { get; set; }
    public Guid? AcceptorAssignmentId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
