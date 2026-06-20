using Wokki.Application.Dtos.Scheduling;
using EmployeeEntity = Wokki.Domain.Entities.Employee;

namespace Wokki.Application.Validators.Scheduling;

/// <summary>
/// Validates a parsed (e.g. AI-generated) scheduling hint against the closed set of 3 allowed
/// shapes, checking employee/role references against the same active-employee roster the
/// CP-SAT solver consumes (<see cref="ScheduleSuggestionHintValidator.Validate"/> takes the
/// roster, never an unfiltered employee query), so a hint can never validate successfully
/// against a terminated employee or a role with nobody on it.
/// </summary>
public static class ScheduleSuggestionHintValidator
{
    public static ScheduleSuggestionHintValidationResult Validate(
        ScheduleSuggestionHint hint,
        IReadOnlyCollection<EmployeeEntity> activeRoster)
    {
        var activeEmployeeIds = activeRoster.Select(e => e.Id).ToHashSet();

        return hint.Kind switch
        {
            ScheduleSuggestionHintKind.PreferenceWeight => ValidatePreferenceWeight(hint, activeRoster, activeEmployeeIds),
            ScheduleSuggestionHintKind.TempMinMax => ValidateTempMinMax(hint, activeRoster, activeEmployeeIds),
            ScheduleSuggestionHintKind.AvoidPairing => ValidateAvoidPairing(hint, activeEmployeeIds),
            _ => Fail(ScheduleSuggestionHintValidationError.UnrecognizedKind, $"Unrecognized hint kind: {hint.Kind}.")
        };
    }

    private static ScheduleSuggestionHintValidationResult ValidatePreferenceWeight(
        ScheduleSuggestionHint hint,
        IReadOnlyCollection<EmployeeEntity> activeRoster,
        IReadOnlySet<Guid> activeEmployeeIds)
    {
        if (hint.Direction is null)
            return Fail(ScheduleSuggestionHintValidationError.InvalidWeightHint, "Preference-weight hint requires a direction (Boost or Reduce).");

        return ValidateGroup(hint.Group, activeRoster, activeEmployeeIds);
    }

    private static ScheduleSuggestionHintValidationResult ValidateTempMinMax(
        ScheduleSuggestionHint hint,
        IReadOnlyCollection<EmployeeEntity> activeRoster,
        IReadOnlySet<Guid> activeEmployeeIds)
    {
        if (hint.MinCount is null && hint.MaxCount is null)
            return Fail(ScheduleSuggestionHintValidationError.InvalidMinMaxHint, "Temp min/max hint requires at least one of MinCount or MaxCount.");

        if (hint.MinCount is < 0 || hint.MaxCount is < 0)
            return Fail(ScheduleSuggestionHintValidationError.InvalidMinMaxHint, "MinCount/MaxCount must be non-negative.");

        if (hint.MinCount.HasValue && hint.MaxCount.HasValue && hint.MinCount > hint.MaxCount)
            return Fail(ScheduleSuggestionHintValidationError.InvalidMinMaxHint, "MinCount cannot exceed MaxCount.");

        return ValidateGroup(hint.Group, activeRoster, activeEmployeeIds);
    }

    private static ScheduleSuggestionHintValidationResult ValidateAvoidPairing(
        ScheduleSuggestionHint hint,
        IReadOnlySet<Guid> activeEmployeeIds)
    {
        if (hint.EmployeeId1 is null || hint.EmployeeId2 is null)
            return Fail(ScheduleSuggestionHintValidationError.InvalidPairingHint, "Avoid-pairing hint requires exactly two employee references.");

        if (hint.EmployeeId1 == hint.EmployeeId2)
            return Fail(ScheduleSuggestionHintValidationError.InvalidPairingHint, "Avoid-pairing hint requires two distinct employees.");

        if (!activeEmployeeIds.Contains(hint.EmployeeId1.Value))
            return Fail(ScheduleSuggestionHintValidationError.EmployeeNotFound, $"Employee {hint.EmployeeId1} not found in this schedule's active roster.");

        if (!activeEmployeeIds.Contains(hint.EmployeeId2.Value))
            return Fail(ScheduleSuggestionHintValidationError.EmployeeNotFound, $"Employee {hint.EmployeeId2} not found in this schedule's active roster.");

        return ScheduleSuggestionHintValidationResult.Success();
    }

    private static ScheduleSuggestionHintValidationResult ValidateGroup(
        HintGroupRef? group,
        IReadOnlyCollection<EmployeeEntity> activeRoster,
        IReadOnlySet<Guid> activeEmployeeIds)
    {
        if (group is null)
            return Fail(ScheduleSuggestionHintValidationError.EmptyGroup, "A target group is required.");

        switch (group.GroupType)
        {
            case HintGroupType.RoleDepartment:
                if (string.IsNullOrWhiteSpace(group.Role))
                    return Fail(ScheduleSuggestionHintValidationError.EmptyGroup, "RoleDepartment group requires a non-empty Role value.");

                var hasMatchingEmployee = activeRoster.Any(e =>
                    string.Equals(e.Position, group.Role, StringComparison.OrdinalIgnoreCase));
                if (!hasMatchingEmployee)
                    return Fail(ScheduleSuggestionHintValidationError.EmptyGroup, $"No active employee in this schedule's roster has role '{group.Role}'.");

                return ScheduleSuggestionHintValidationResult.Success();

            case HintGroupType.ExplicitEmployeeIds:
                if (group.EmployeeIds is null || group.EmployeeIds.Count == 0)
                    return Fail(ScheduleSuggestionHintValidationError.EmptyGroup, "ExplicitEmployeeIds group requires at least one employee id.");

                var missing = group.EmployeeIds.Where(id => !activeEmployeeIds.Contains(id)).ToList();
                if (missing.Count > 0)
                    return Fail(
                        ScheduleSuggestionHintValidationError.EmployeeNotFound,
                        $"Employee id(s) not found in this schedule's active roster: {string.Join(", ", missing)}.");

                return ScheduleSuggestionHintValidationResult.Success();

            default:
                return Fail(ScheduleSuggestionHintValidationError.UnrecognizedGroupType, $"Unrecognized group type: {group.GroupType}.");
        }
    }

    private static ScheduleSuggestionHintValidationResult Fail(
        ScheduleSuggestionHintValidationError error,
        string detail) => ScheduleSuggestionHintValidationResult.Failure(error, detail);
}
