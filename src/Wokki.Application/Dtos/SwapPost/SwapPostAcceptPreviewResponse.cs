namespace Wokki.Application.Dtos.SwapPost;

public sealed record SwapPostAcceptPreviewResponse(bool IsValid, string? ErrorCode, string? ErrorMessage);
