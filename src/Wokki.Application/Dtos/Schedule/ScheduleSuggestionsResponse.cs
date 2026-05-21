namespace Wokki.Application.Dtos.Schedule;

public sealed record ScheduleSuggestionsResponse(
    IReadOnlyList<ScheduleSuggestionItem> Suggestions,
    string? Reason);

public sealed record ScheduleSuggestionItem(
    Guid Id,
    Guid ShiftDefinitionId,
    string ShiftName,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    int Score);
