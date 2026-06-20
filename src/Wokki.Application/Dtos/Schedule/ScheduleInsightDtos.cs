using Wokki.Application.Dtos.Scheduling;

namespace Wokki.Application.Dtos.Schedule;

public sealed record GenerateScheduleInsightContextRequest(
    IReadOnlyList<ScheduleInsightSuggestionInput>? Suggestions = null,
    string Provider = "heuristic",
    bool FallbackUsed = false,
    string? SolveStatus = null,
    int? SolveDurationMs = null,
    IReadOnlyList<string>? Warnings = null);

public sealed record ScheduleInsightSuggestionInput(
    Guid ShiftDefinitionId,
    Guid EmployeeId,
    DateOnly Date,
    int Score,
    IReadOnlyList<string>? Explanations = null,
    IReadOnlyList<string>? Warnings = null);

public sealed record ScheduleInsightContextResponse(
    Guid ScheduleId,
    Guid LocationId,
    Guid DepartmentId,
    DateOnly WeekStartDate,
    string SchemaVersion,
    string Provider,
    bool FallbackUsed,
    DateTime GeneratedAt,
    DateTime UpdatedAt,
    DateTime ExpiresAt,
    string JsonContent);

public sealed record ScheduleInsightChatRequest(string Question, bool HintMode = false);

public sealed record ScheduleInsightChatResponse(
    Guid ScheduleId,
    string Answer,
    string Provider,
    DateTime ContextGeneratedAt,
    bool HintMode = false,
    ScheduleSuggestionHint? Hint = null,
    string? HintSummary = null,
    bool HintUnderstood = false);
