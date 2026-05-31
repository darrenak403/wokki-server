using Wokki.Application.Dtos.Scheduling;
using Wokki.Application.Services.OrganizationSchedulingPolicy.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Scheduling;

public static class SchedulingCatalogEndpoints
{
    public static IEndpointRouteBuilder MapSchedulingCatalogApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/scheduling")
            .MapSchedulingCatalogRoutes()
            .WithTags("Scheduling")
            .RequireAuthorization();

        return builder;
    }

    public static RouteGroupBuilder MapSchedulingCatalogRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/rule-catalog", GetRuleCatalogAsync)
            .WithName("GetSchedulingRuleCatalog")
            .WithDescription("Platform catalog of scheduling rule definitions (org-shared).")
            .Produces<ApiResponse<SchedulingRuleCatalogResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> GetRuleCatalogAsync(
        IOrganizationSchedulingPolicyService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetCatalogAsync(cancellationToken);
        return result.ToHttpResult();
    }
}
