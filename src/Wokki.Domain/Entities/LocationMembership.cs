using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class LocationMembership
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public LocationMembershipStatus Status { get; set; } = LocationMembershipStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Note { get; set; }
}
