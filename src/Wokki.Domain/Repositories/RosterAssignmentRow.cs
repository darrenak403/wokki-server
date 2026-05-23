using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public sealed class RosterAssignmentRow
{
    public required ShiftAssignment Assignment { get; init; }
    public required Schedule Schedule { get; init; }
    public required ShiftDefinition ShiftDefinition { get; init; }
    public required Employee Employee { get; init; }
    public required Department Department { get; init; }
}
