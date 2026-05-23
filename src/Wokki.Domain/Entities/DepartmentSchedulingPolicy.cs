namespace Wokki.Domain.Entities;

public class DepartmentSchedulingPolicy
{
    public Guid DepartmentId { get; set; }
    public int MaxShiftsPerEmployeePerWeek { get; set; } = 20;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
