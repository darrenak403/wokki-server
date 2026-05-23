using Wokki.Domain.Enums;

namespace Wokki.Application.Scheduling;

public sealed record SubmittedPreferenceChoice(
    Guid EmployeeId,
    Guid ShiftDefinitionId,
    DateOnly Date,
    PreferenceType PreferenceType);
