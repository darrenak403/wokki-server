namespace Wokki.Domain.Entities;

public class LocationSchedulingPolicy
{
    public Guid LocationId { get; set; }
    public Guid OrganizationId { get; set; }

    public string RulesJson { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
