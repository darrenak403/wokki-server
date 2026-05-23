namespace Wokki.Application.Dtos.Bedrock;

public sealed record BedrockConverseResult(
    string? Text,
    string? StopReason,
    int? InputTokens,
    int? OutputTokens);
