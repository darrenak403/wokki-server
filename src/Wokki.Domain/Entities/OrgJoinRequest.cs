using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class OrgJoinRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public OrgJoinRequestStatus Status { get; set; } = OrgJoinRequestStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? RejectNote { get; set; }

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
