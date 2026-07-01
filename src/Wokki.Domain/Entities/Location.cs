namespace Wokki.Domain.Entities;

public class Location
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    /// <summary>Expected office network address (single IP or CIDR) for check-in IP matching.</summary>
    public string? NetworkIpOrCidr { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
