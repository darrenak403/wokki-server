using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Application.Services.Stats.Interfaces;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Platform;

public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/platform")
            .MapPlatformRoutes()
            .WithTags("Platform")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.PlatformOperator));

        return builder;
    }

    public static RouteGroupBuilder MapPlatformRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/stats", GetPlatformStatsAsync)
            .WithName("GetPlatformStats")
            .WithDescription("Thống kê toàn platform (chỉ PlatformOperator).")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        return group;
    }

    private static async Task<IResult> GetPlatformStatsAsync(
        IStatsService statsService,
        CancellationToken cancellationToken)
    {
        var result = await statsService.GetPlatformStatsAsync(cancellationToken);
        return result.ToHttpResult();
    }
}
