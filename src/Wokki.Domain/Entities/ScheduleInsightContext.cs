namespace Wokki.Domain.Entities;

public class ScheduleInsightContext
{
    public Guid ScheduleId { get; set; }
    public Guid LocationId { get; set; }
    public Guid DepartmentId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string SchemaVersion { get; set; } = "1.0";
    public string Provider { get; set; } = "heuristic";
    public bool FallbackUsed { get; set; }
    public string JsonContent { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(14);
}
