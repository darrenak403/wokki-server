namespace Wokki.Application.Dtos.Shift;

public sealed record CopyShiftDefinitionsRequest(
    Guid LocationId,
    Guid SourceDepartmentId,
    IReadOnlyList<Guid> TargetDepartmentIds,
    IReadOnlyList<Guid>? ShiftIds = null);
