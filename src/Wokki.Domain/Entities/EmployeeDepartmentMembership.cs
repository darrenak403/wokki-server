using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class EmployeeDepartmentMembership
{
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public bool IsPrimary { get; set; }
    public DepartmentMembershipStatus Status { get; set; } = DepartmentMembershipStatus.Active;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
