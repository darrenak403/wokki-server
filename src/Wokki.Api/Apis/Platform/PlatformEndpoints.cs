using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Common;
using Wokki.Api.Extensions;
using Wokki.Application.Dtos.Platform;
using Wokki.Application.Services.Platform.Interfaces;
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

        group.MapGet("/users", ListUsersAsync)
            .WithName("ListPlatformUsers")
            .WithDescription("Danh sách user toàn hệ thống cho Wokki admin.")
            .Produces<ApiResponse<PagedResponse<PlatformUserResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/organizations", ListOrganizationsAsync)
            .WithName("ListPlatformOrganizations")
            .WithDescription("Danh sách org và trạng thái gói sử dụng cho Wokki admin.")
            .Produces<ApiResponse<PagedResponse<PlatformOrganizationResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPut("/organizations/{id:guid}/subscription", UpdateOrganizationSubscriptionAsync)
            .WithName("UpdateOrganizationSubscription")
            .WithDescription("Bật/tắt hoặc gia hạn gói sử dụng org.")
            .Produces<ApiResponse<PlatformOrganizationResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> GetPlatformStatsAsync(
        IStatsService statsService,
        CancellationToken cancellationToken)
    {
        var result = await statsService.GetPlatformStatsAsync(cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListUsersAsync(
        [AsParameters] PaginationRequest pagination,
        [FromServices] IPlatformAdminService platformAdminService,
        [FromQuery] Guid? organizationId,
        [FromQuery] string? role,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await platformAdminService.ListUsersAsync(
            pagination.Page,
            pagination.PageSize,
            organizationId,
            role,
            search,
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListOrganizationsAsync(
        [AsParameters] PaginationRequest pagination,
        [FromServices] IPlatformAdminService platformAdminService,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await platformAdminService.ListOrganizationsAsync(
            pagination.Page,
            pagination.PageSize,
            search,
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateOrganizationSubscriptionAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateOrganizationSubscriptionRequest request,
        [FromServices] IPlatformAdminService platformAdminService,
        [FromServices] IValidator<UpdateOrganizationSubscriptionRequest> validator,
        CancellationToken cancellationToken)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var result = await platformAdminService.UpdateOrganizationSubscriptionAsync(id, request, cancellationToken);
        return result.ToHttpResult();
    }
}
