using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

public sealed class ScheduleSuggestionContext
{
    public required Schedule Schedule { get; init; }
    public required Department Department { get; init; }
    public required IReadOnlyList<Employee> Employees { get; init; }
    public required IReadOnlyList<ShiftDefinition> Shifts { get; init; }
    public required IReadOnlyList<ShiftAssignment> ExistingAssignments { get; init; }
    public required IReadOnlyList<ShiftAssignment> HistoricalAssignments { get; init; }
    public required IReadOnlyList<EmployeeAvailability> Availabilities { get; init; }
    public required IReadOnlyList<SubmittedPreferenceChoice> SubmittedPreferences { get; init; }
    public required IReadOnlyList<JobPosition> JobPositions { get; init; }
    public DepartmentSchedulingPolicy? SchedulingPolicy { get; init; }
}
