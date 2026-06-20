namespace Wokki.Application.Dtos.Scheduling;

public enum ScheduleSuggestionHintKind
{
    PreferenceWeight,
    TempMinMax,
    AvoidPairing
}

/// <summary>
/// Closed set of group vocabularies a hint can target. EmploymentType is intentionally
/// excluded — Employee has no full-time/part-time field, only Position (job title) and
/// DepartmentId; RoleDepartment covers both via the same fields the solver already reads.
/// </summary>
public enum HintGroupType
{
    RoleDepartment,
    ExplicitEmployeeIds
}

public enum HintWeightDirection
{
    Boost,
    Reduce
}

public sealed record HintGroupRef
{
    public required HintGroupType GroupType { get; init; }
    public string? Role { get; init; }
    public IReadOnlyList<Guid>? EmployeeIds { get; init; }
}

public sealed record ScheduleSuggestionHint
{
    public required ScheduleSuggestionHintKind Kind { get; init; }

    // PreferenceWeight
    public HintGroupRef? Group { get; init; }
    public HintWeightDirection? Direction { get; init; }

    // TempMinMax (reuses Group above)
    public int? MinCount { get; init; }
    public int? MaxCount { get; init; }

    // AvoidPairing
    public Guid? EmployeeId1 { get; init; }
    public Guid? EmployeeId2 { get; init; }
}

public enum ScheduleSuggestionHintValidationError
{
    None,
    UnrecognizedKind,
    UnrecognizedGroupType,
    EmptyGroup,
    EmployeeNotFound,
    InvalidWeightHint,
    InvalidMinMaxHint,
    InvalidPairingHint
}

public sealed record ScheduleSuggestionHintValidationResult(
    bool IsValid,
    ScheduleSuggestionHintValidationError Error = ScheduleSuggestionHintValidationError.None,
    string? Detail = null)
{
    public static ScheduleSuggestionHintValidationResult Success() => new(true);

    public static ScheduleSuggestionHintValidationResult Failure(
        ScheduleSuggestionHintValidationError error,
        string detail) => new(false, error, detail);
}
