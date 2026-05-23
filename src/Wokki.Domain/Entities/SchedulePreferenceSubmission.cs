using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class SchedulePreferenceSubmission
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid EmployeeId { get; set; }
    public SchedulePreferenceStatus Status { get; set; } = SchedulePreferenceStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<SchedulePreferenceLine> Lines { get; set; } = new List<SchedulePreferenceLine>();
}
