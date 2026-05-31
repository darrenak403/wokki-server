namespace Wokki.Domain.Entities;

public class ShiftAssignment
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid ShiftDefinitionId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
