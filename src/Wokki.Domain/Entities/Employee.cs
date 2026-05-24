namespace Wokki.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public Guid DepartmentId { get; set; }
    public DateTime EmployedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TerminatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
