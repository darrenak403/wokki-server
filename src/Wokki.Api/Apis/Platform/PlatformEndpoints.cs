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

        group.MapGet("/health", GetPlatformHealthAsync)
            .WithName("GetPlatformHealth")
            .WithDescription("Chẩn đoán API và dependency cho Wokki admin.")
            .Produces<ApiResponse<PlatformHealthResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/usage-analytics", GetUsageAnalyticsAsync)
            .WithName("GetPlatformUsageAnalytics")
            .WithDescription("Tín hiệu sử dụng org theo cửa sổ thời gian.")
            .Produces<ApiResponse<PlatformUsageAnalyticsResponse>>(StatusCodes.Status200OK)
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

        group.MapGet("/subscription-ledger", ListSubscriptionLedgerAsync)
            .WithName("ListPlatformSubscriptionLedger")
            .WithDescription("Lịch sử thay đổi gói sử dụng toàn platform.")
            .Produces<ApiResponse<PagedResponse<PlatformSubscriptionLedgerEntryResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/organizations/{id:guid}/subscription-ledger", ListOrganizationSubscriptionLedgerAsync)
            .WithName("ListOrganizationSubscriptionLedger")
            .WithDescription("Lịch sử thay đổi gói sử dụng của một org.")
            .Produces<ApiResponse<PagedResponse<PlatformSubscriptionLedgerEntryResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/support/search", SearchSupportAsync)
            .WithName("SearchPlatformSupport")
            .WithDescription("Tìm kiếm hỗ trợ theo org id, tên org hoặc email user.")
            .Produces<ApiResponse<PagedResponse<PlatformSupportSearchResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/support/organizations/{id:guid}/context", GetSupportOrganizationContextAsync)
            .WithName("GetPlatformSupportOrganizationContext")
            .WithDescription("Ngữ cảnh hỗ trợ của một org cho Wokki admin.")
            .Produces<ApiResponse<PlatformOrganizationSupportContextResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

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

    private static async Task<IResult> GetPlatformHealthAsync(
        [FromServices] IPlatformDiagnosticsService diagnosticsService,
        CancellationToken cancellationToken)
    {
        var result = await diagnosticsService.GetHealthAsync(cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUsageAnalyticsAsync(
        [AsParameters] PlatformUsageAnalyticsRequest request,
        [FromServices] IPlatformUsageAnalyticsService usageAnalyticsService,
        CancellationToken cancellationToken)
    {
        var result = await usageAnalyticsService.GetAsync(request, cancellationToken);
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
        [AsParameters] PlatformOrganizationListRequest request,
        [FromServices] IPlatformAdminService platformAdminService,
        CancellationToken cancellationToken)
    {
        var result = await platformAdminService.ListOrganizationsAsync(
            request,
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListSubscriptionLedgerAsync(
        [AsParameters] PlatformSubscriptionLedgerListRequest request,
        [FromServices] IPlatformAdminService platformAdminService,
        CancellationToken cancellationToken)
    {
        var result = await platformAdminService.ListSubscriptionLedgerAsync(request, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListOrganizationSubscriptionLedgerAsync(
        [FromRoute] Guid id,
        [AsParameters] PlatformSubscriptionLedgerListRequest request,
        [FromServices] IPlatformAdminService platformAdminService,
        CancellationToken cancellationToken)
    {
        var scopedRequest = new PlatformSubscriptionLedgerListRequest
        {
            Page = request.Page,
            PageSize = request.PageSize,
            OrganizationId = id,
            Action = request.Action,
            From = request.From,
            To = request.To
        };

        var result = await platformAdminService.ListSubscriptionLedgerAsync(scopedRequest, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> SearchSupportAsync(
        [AsParameters] PlatformSupportSearchRequest request,
        [FromServices] IPlatformAdminService platformAdminService,
        CancellationToken cancellationToken)
    {
        var result = await platformAdminService.SearchSupportAsync(request, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetSupportOrganizationContextAsync(
        [FromRoute] Guid id,
        [FromServices] IPlatformAdminService platformAdminService,
        CancellationToken cancellationToken)
    {
        var result = await platformAdminService.GetSupportOrganizationContextAsync(id, cancellationToken);
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
