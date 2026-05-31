namespace Wokki.Domain.Entities;

public class PayrollLine
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PayPeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public int TotalWorkedMinutes { get; set; }
    public int RegularMinutes { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal GrossPay { get; set; }
    public int ApprovedOvertimeMinutes { get; set; } = 0;
    public decimal OvertimePay { get; set; } = 0;
    public DateTime? PaidAt { get; set; }
    public Guid? PaidMarkedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
