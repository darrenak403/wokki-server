namespace Wokki.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool SubscriptionEnabled { get; set; }
    /// <summary>Last admin-chosen package length in days; 0 until first activation.</summary>
    public int SubscriptionDurationDays { get; set; }
    public DateTime? SubscriptionActivatedAt { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public DateTime? SubscriptionUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
