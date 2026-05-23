namespace Wokki.Infrastructure.Bedrock;

/// <summary>IAM user access keys — bind from User Secrets / env (<c>AWS__*</c>).</summary>
public sealed class AwsSettings
{
    public const string SectionName = "AWS";

    public string Region { get; set; } = "us-east-1";

    public string? AccessKeyId { get; set; }

    public string? SecretAccessKey { get; set; }

    public string? SessionToken { get; set; }
}
