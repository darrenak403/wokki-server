using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wokki.Application.Dtos.Bedrock;
using Wokki.Application.Services.Bedrock.Interfaces;
using Wokki.Common.Utils;

namespace Wokki.Infrastructure.Bedrock;

public sealed class BedrockService(
    IAmazonBedrockRuntime bedrockRuntime,
    IOptions<BedrockSettings> bedrockOptions,
    IOptions<AwsSettings> awsOptions,
    ILogger<BedrockService> logger) : IBedrockService
{
    public async Task<ApiResponse<BedrockHealthResponse>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var bedrock = bedrockOptions.Value;
        var region = BedrockRegionResolver.Resolve(bedrock, awsOptions.Value);
        var modelId = ResolveHealthModelId(bedrock);

        try
        {
            var response = await ConverseInternalAsync(
                modelId,
                "ok",
                bedrock.HealthCheckMaxTokens,
                bedrock.HealthCheckTemperature,
                bedrock.HealthCheckTimeoutSeconds,
                cancellationToken);

            var payload = new BedrockHealthResponse
            {
                IsConnected = true,
                Status = "Connected",
                ModelId = modelId,
                Region = region,
                Message = string.IsNullOrWhiteSpace(response.Text)
                    ? "AWS Bedrock connection is healthy."
                    : $"AWS Bedrock connection is healthy. Model responded: {response.Text}",
                CheckedAtUtc = DateTime.UtcNow
            };

            return ApiResponse<BedrockHealthResponse>.SuccessResponse(payload, AppMessages.Bedrock.Connected);
        }
        catch (AmazonBedrockRuntimeException ex) when (
            string.Equals(ex.ErrorCode, "ThrottlingException", StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Bedrock health throttled. ModelId: {ModelId}, Region: {Region}",
                modelId,
                region);

            return HealthFailure(
                modelId,
                region,
                "Throttled",
                ex.Message,
                AppMessages.Bedrock.Throttled);
        }
        catch (AmazonBedrockRuntimeException ex)
        {
            logger.LogError(
                ex,
                "AWS Bedrock health check failed. ModelId: {ModelId}, Region: {Region}",
                modelId,
                region);

            return HealthFailure(modelId, region, ex.ErrorCode, ex.Message, AppMessages.Bedrock.Disconnected);
        }
        catch (AmazonClientException ex)
        {
            logger.LogError(ex, "AWS credentials or client configuration error.");
            return HealthFailure(modelId, region, ex.GetType().Name, ex.Message, AppMessages.Bedrock.Disconnected);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthFailure(
                modelId,
                region,
                "Timeout",
                $"Bedrock health check timed out after {bedrock.HealthCheckTimeoutSeconds}s.",
                AppMessages.Bedrock.Disconnected);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while checking AWS Bedrock connection.");
            return HealthFailure(modelId, region, ex.GetType().Name, ex.Message, AppMessages.Bedrock.Disconnected);
        }
    }

    private static string ResolveHealthModelId(BedrockSettings bedrock) =>
        string.IsNullOrWhiteSpace(bedrock.HealthCheckModelId)
            ? bedrock.ModelId.Trim()
            : bedrock.HealthCheckModelId.Trim();

    public async Task<BedrockConverseResult> ConverseAsync(
        string userPrompt,
        BedrockConverseOptions options,
        CancellationToken cancellationToken = default)
    {
        var bedrock = bedrockOptions.Value;
        var modelId = bedrock.ModelId.Trim();

        return await ConverseInternalAsync(
            modelId,
            userPrompt,
            options.MaxTokens,
            options.Temperature,
            options.TimeoutSeconds,
            cancellationToken);
    }

    private async Task<BedrockConverseResult> ConverseInternalAsync(
        string modelId,
        string userPrompt,
        int maxTokens,
        float temperature,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var response = await bedrockRuntime.ConverseAsync(new ConverseRequest
        {
            ModelId = modelId,
            Messages =
            [
                new Message
                {
                    Role = ConversationRole.User,
                    Content = [new ContentBlock { Text = userPrompt }]
                }
            ],
            InferenceConfig = new InferenceConfiguration
            {
                MaxTokens = maxTokens,
                Temperature = temperature
            }
        }, timeoutCts.Token);

        return new BedrockConverseResult(
            response.Output?.Message?.Content?.FirstOrDefault()?.Text,
            response.StopReason,
            response.Usage?.InputTokens,
            response.Usage?.OutputTokens);
    }

    private static ApiResponse<BedrockHealthResponse> HealthFailure(
        string modelId,
        string region,
        string? errorCode,
        string? errorMessage,
        AppMessage message) =>
        ApiResponse<BedrockHealthResponse>.FailureResponse(
            new BedrockHealthResponse
            {
                IsConnected = false,
                Status = string.Equals(errorCode, "Throttled", StringComparison.Ordinal) ? "Throttled" : "Disconnected",
                ModelId = modelId,
                Region = region,
                Message = message.Text,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CheckedAtUtc = DateTime.UtcNow
            },
            message);
}
