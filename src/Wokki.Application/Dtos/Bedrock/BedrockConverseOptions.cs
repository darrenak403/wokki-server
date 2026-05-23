namespace Wokki.Application.Dtos.Bedrock;

public sealed record BedrockConverseOptions(
    int MaxTokens,
    float Temperature,
    int TimeoutSeconds);
