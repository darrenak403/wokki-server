using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Application.Services.Stats.Interfaces;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Organization;

public static class OrgStatsEndpoints
{
    public static IEndpointRouteBuilder MapOrgStatsApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/org")
            .MapOrgStatsRoutes()
            .WithTags("Organization")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager));

        return builder;
    }

    public static RouteGroupBuilder MapOrgStatsRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/stats", GetOrgStatsAsync)
            .WithName("GetOrgStats")
            .WithDescription("Thống kê tổ chức hiện tại (Admin + Manager).")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        return group;
    }

    private static async Task<IResult> GetOrgStatsAsync(
        IStatsService statsService,
        CancellationToken cancellationToken)
    {
        var result = await statsService.GetOrgStatsAsync(cancellationToken);
        return result.ToHttpResult();
    }
}
