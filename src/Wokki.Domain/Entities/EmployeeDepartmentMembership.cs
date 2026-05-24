namespace Wokki.Domain.Entities;

public class EmployeeDepartmentMembership
{
    public Guid EmployeeId { get; set; }
    public Guid DepartmentId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
