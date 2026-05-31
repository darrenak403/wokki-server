namespace Wokki.Domain.Entities;

public class OrganizationSchedulingPolicy
{
    public Guid OrganizationId { get; set; }

    public string RulesJson { get; set; } = "[]";
    public string SchemaVersion { get; set; } = "org-scheduling-policy.v1";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
