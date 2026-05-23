namespace Wokki.Application.Dtos.Bedrock;

public sealed class BedrockHealthResponse
{
    public bool IsConnected { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string? Message { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CheckedAtUtc { get; set; }
}
