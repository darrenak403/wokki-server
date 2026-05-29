using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Application.Services.Stats.Interfaces;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Organization;

public static class OrgStatsEndpoints
{
    public static IEndpointRouteBuilder MapOrgStatsApi(this IEndpointRouteBuilder builder)
    {
        var orgGroup = builder.MapGroup("/api/v1/org").WithTags("Organization");

        orgGroup.MapOrgStatsRoutes()
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager));

        orgGroup.MapOrgSubscriptionRoutes()
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager, RoleConstants.User));

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

    public static RouteGroupBuilder MapOrgSubscriptionRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/subscription", GetOrgSubscriptionAsync)
            .WithName("GetOrgSubscription")
            .WithDescription("Trạng thái gói sử dụng và số ngày còn lại (mọi role org).")
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

    private static async Task<IResult> GetOrgSubscriptionAsync(
        IStatsService statsService,
        CancellationToken cancellationToken)
    {
        var result = await statsService.GetOrgSubscriptionAsync(cancellationToken);
        return result.ToHttpResult();
    }
}
