namespace Wokki.Application.Dtos.SwapRequest;

public sealed record CreateSwapRequestRequest(
    Guid RequesterAssignmentId,
    Guid TargetAssignmentId,
    string? RequesterNote = null);
