using Wokki.Domain.Enums;

namespace Wokki.Application.Dtos.SwapPost;

public sealed record CreateSwapPostRequest(
    Guid AuthorAssignmentId,
    SwapPostType Type,
    string? Note);
