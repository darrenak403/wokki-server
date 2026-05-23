namespace Wokki.Application.Common.Interfaces;

public interface IScheduleSuggestionService
{
    Task<ScheduleSuggestionGenerationResult> GenerateAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default);
}

public sealed record ScheduleSuggestionGenerationResult(
    IReadOnlyList<ScheduleSuggestionDto> Suggestions,
    string? Reason,
    string Provider = "heuristic",
    bool FallbackUsed = false);

public sealed record ScheduleSuggestionDto(
    Guid Id,
    Guid ShiftDefinitionId,
    Guid EmployeeId,
    DateOnly Date,
    int Score);
