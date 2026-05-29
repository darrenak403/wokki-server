using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class PayPeriod
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public PayPeriodStatus Status { get; set; } = PayPeriodStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
