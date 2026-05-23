using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class SchedulePreferenceLine
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ShiftDefinitionId { get; set; }
    public DateOnly Date { get; set; }
    public PreferenceType PreferenceType { get; set; } = PreferenceType.Available;
}
