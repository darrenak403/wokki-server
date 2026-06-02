namespace Wokki.Domain.Entities;

public class PlatformActivityEvent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
}
