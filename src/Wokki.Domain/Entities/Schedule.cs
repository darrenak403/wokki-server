using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class Schedule
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Draft;
    public Guid CreatedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
