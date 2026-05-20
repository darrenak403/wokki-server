namespace Wokki.Application.Dtos.Schedule;

public sealed record CreateShiftAssignmentRequest(
    Guid ShiftDefinitionId,
    Guid EmployeeId,
    DateOnly Date,
    string? Note = null);
