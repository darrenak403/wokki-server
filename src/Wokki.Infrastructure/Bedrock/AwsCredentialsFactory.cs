using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace Wokki.Infrastructure.Bedrock;

internal static class AwsCredentialsFactory
{
    public static AWSCredentials Resolve(IConfiguration configuration, AwsSettings aws)
    {
        var accessKey = FirstNonEmpty(
            aws.AccessKeyId,
            configuration["AWS:AccessKeyId"],
            configuration["AWS_ACCESS_KEY_ID"]);

        var secretKey = FirstNonEmpty(
            aws.SecretAccessKey,
            configuration["AWS:SecretAccessKey"],
            configuration["AWS_SECRET_ACCESS_KEY"]);

        var sessionToken = FirstNonEmpty(
            aws.SessionToken,
            configuration["AWS:SessionToken"],
            configuration["AWS_SESSION_TOKEN"]);

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "AWS IAM credentials are not configured. Set AWS:AccessKeyId and AWS:SecretAccessKey " +
                "(User Secrets) or AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY (environment).");
        }

        return string.IsNullOrWhiteSpace(sessionToken)
            ? new BasicAWSCredentials(accessKey, secretKey)
            : new SessionAWSCredentials(accessKey, secretKey, sessionToken);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
