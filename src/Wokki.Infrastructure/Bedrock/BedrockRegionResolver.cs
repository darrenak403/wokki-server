namespace Wokki.Infrastructure.Bedrock;

internal static class BedrockRegionResolver
{
    public static string Resolve(BedrockSettings bedrock, AwsSettings aws) =>
        string.IsNullOrWhiteSpace(bedrock.Region) ? aws.Region : bedrock.Region.Trim();
}
