namespace Wokki.Application.Dtos.Schedule;

public sealed record ApplyScheduleSuggestionsRequest(
    IReadOnlyList<ApplyScheduleSuggestionItem> Suggestions,
    /// <summary>Remove affected employees' draft assignments whose (shift, employee, date) tuple is not included in <see cref="Suggestions"/>.</summary>
    bool ClearOrphanAssignments = false);

public sealed record ApplyScheduleSuggestionItem(
    Guid ShiftDefinitionId,
    Guid EmployeeId,
    DateOnly Date,
    string? Note = null);
