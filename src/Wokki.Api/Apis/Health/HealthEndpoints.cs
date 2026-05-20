using Wokki.Common.Extensions;
using Wokki.Common.Utils;

namespace Wokki.Api.Apis.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/health")
            .MapHealthRoutes()
            .WithTags("Health");

        return builder;
    }

    public static RouteGroupBuilder MapHealthRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetHealthAsync)
            .WithName("GetHealth")
            .WithDescription("Kiểm tra service còn sống.")
            .AllowAnonymous()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        return group;
    }

    private static Task<IResult> GetHealthAsync() =>
        Task.FromResult(
            ApiResponse<object>.SuccessResponse(new { status = "healthy" }, AppMessages.Health.Ok).ToHttpResult());
}
