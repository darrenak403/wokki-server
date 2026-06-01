using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Common;
using Wokki.Application.Dtos.Organization;
using Wokki.Application.Services.Organization.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Organization;

public static class OrganizationDirectoryEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationDirectoryApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/organizations")
            .MapOrganizationDirectoryRoutes()
            .WithTags("Organizations")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapOrganizationDirectoryRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/directory", ListDirectoryAsync)
            .WithName("ListOrganizationDirectory")
            .WithDescription("Danh bạ tổ chức có gói active — chỉ user chưa thuộc org.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<PagedResponse<OrganizationDirectoryItemResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        return group;
    }

    private static async Task<IResult> ListDirectoryAsync(
        [AsParameters] PaginationRequest pagination,
        [FromQuery] string? search,
        [FromServices] IOrganizationDirectoryService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.ListAsync(pagination.Page, pagination.PageSize, search, cancellationToken);
        return response.ToHttpResult();
    }
}
