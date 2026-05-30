namespace Wokki.Application.Dtos.Shift;

public sealed record CopyShiftDefinitionsResponse(
    int CopiedCount,
    int SkippedCount,
    IReadOnlyList<Guid> CreatedShiftIds,
    IReadOnlyList<CopyShiftSkippedItem> Skipped);

public sealed record CopyShiftSkippedItem(
    Guid TargetDepartmentId,
    string Name,
    string Reason);
