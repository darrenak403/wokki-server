namespace Wokki.Domain.Entities;

public class OrganizationSubscriptionLedgerEntry
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public int PreviousDurationDays { get; set; }
    public int NewDurationDays { get; set; }
    public DateTime? PreviousExpiresAt { get; set; }
    public DateTime? NewExpiresAt { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}
