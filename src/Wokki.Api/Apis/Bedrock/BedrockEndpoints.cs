using Wokki.Api.Bootstrapping;
using Wokki.Application.Services.Bedrock.Interfaces;
using Wokki.Application.Dtos.Bedrock;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;

namespace Wokki.Api.Apis.Bedrock;

public static class BedrockEndpoints
{
    public static IEndpointRouteBuilder MapBedrockApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/bedrock")
            .MapBedrockRoutes()
            .WithTags("Bedrock")
            .WithDescription("AWS Bedrock connectivity (health check).")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapBedrockRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/health", GetHealthAsync)
            .WithName("GetBedrockHealth")
            .WithDescription(
                "Ping Bedrock via Converse (prompt: ping). Verifies credentials, IAM, model access, and runtime.")
            .AllowAnonymous()
            .Produces<ApiResponse<BedrockHealthResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<BedrockHealthResponse>>(StatusCodes.Status429TooManyRequests)
            .Produces<ApiResponse<BedrockHealthResponse>>(StatusCodes.Status503ServiceUnavailable);

        return group;
    }

    private static async Task<IResult> GetHealthAsync(
        IBedrockService bedrockService,
        CancellationToken cancellationToken = default) =>
        (await bedrockService.GetHealthAsync(cancellationToken)).ToHttpResult();
}
