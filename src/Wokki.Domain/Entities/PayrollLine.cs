namespace Wokki.Domain.Entities;

public class PayrollLine
{
    public Guid Id { get; set; }
    public Guid PayPeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public int TotalWorkedMinutes { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal GrossPay { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
