namespace Wokki.Domain.Entities;

public class LocationManager
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid AssignedById { get; set; }
    public User AssignedBy { get; set; } = null!;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
