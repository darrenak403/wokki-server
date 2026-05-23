namespace Wokki.Application.Dtos.Schedule;

public sealed record ScheduleSuggestionsResponse(
    IReadOnlyList<ScheduleSuggestionItem> Suggestions,
    string? Reason,
    string Provider = "heuristic",
    bool FallbackUsed = false);

public sealed record ScheduleSuggestionItem(
    Guid Id,
    Guid ShiftDefinitionId,
    string ShiftName,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    int Score);
