namespace Wokki.Application.Dtos.Schedule;

public sealed record ApplyScheduleSuggestionsRequest(
    IReadOnlyList<ApplyScheduleSuggestionItem> Suggestions);

public sealed record ApplyScheduleSuggestionItem(
    Guid ShiftDefinitionId,
    Guid EmployeeId,
    DateOnly Date,
    string? Note = null);
