namespace Wokki.Domain.Entities;

public class ShiftDefinition
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string RequiredRole { get; set; } = string.Empty;
    /// <summary>Max employees assignable to this shift on the same date.</summary>
    public int MaxStaffPerSlot { get; set; } = 1;
    public string Color { get; set; } = "#3B82F6";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
