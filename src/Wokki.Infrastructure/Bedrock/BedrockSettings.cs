namespace Wokki.Infrastructure.Bedrock;

public sealed class BedrockSettings
{
    public const string SectionName = "AWS:Bedrock";

    /// <summary>Bedrock runtime region (falls back to <see cref="AwsSettings.Region"/> when empty).</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>Foundation model for schedule suggest.</summary>
    public string ModelId { get; set; } = "google.gemma-3-4b-it";

    /// <summary>Health ping model (falls back to <see cref="ModelId"/>). Use a smaller model to save quota.</summary>
    public string? HealthCheckModelId { get; set; }

    public int MaxTokens { get; set; } = 4096;

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Health check only — keep at 1 to minimize token usage.</summary>
    public int HealthCheckMaxTokens { get; set; } = 1;

    public float HealthCheckTemperature { get; set; } = 0f;

    public int HealthCheckTimeoutSeconds { get; set; } = 15;
}
