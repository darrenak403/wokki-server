using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.SwapRequest;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Application.Services.SwapRequest.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.SwapRequests;

public static class SwapRequestEndpoints
{
    public static IEndpointRouteBuilder MapSwapRequestApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/swap-requests")
            .MapSwapRequestRoutes()
            .WithTags("SwapRequests")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapSwapRequestRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateAsync)
            .WithName("CreateSwapRequest")
            .WithDescription("Tạo yêu cầu đổi ca (User).")
            .RequireAuthorization()
            .Produces<ApiResponse<SwapRequestResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/", ListAsync)
            .WithName("ListSwapRequests")
            .WithDescription("Danh sách yêu cầu đổi ca (Admin/Manager).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<PagedResponse<SwapRequestResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetSwapRequestById")
            .WithDescription("Chi tiết yêu cầu đổi ca.")
            .RequireAuthorization()
            .Produces<ApiResponse<SwapRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/accept", AcceptAsync)
            .WithName("AcceptSwapRequest")
            .WithDescription("Nhân viên được đề xuất chấp nhận đổi ca.")
            .RequireAuthorization()
            .Produces<ApiResponse<SwapRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/decline", DeclineAsync)
            .WithName("DeclineSwapRequest")
            .WithDescription("Nhân viên được đề xuất từ chối đổi ca.")
            .RequireAuthorization()
            .Produces<ApiResponse<SwapRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("CancelSwapRequest")
            .WithDescription("Người gửi hủy yêu cầu đổi ca.")
            .RequireAuthorization()
            .Produces<ApiResponse<SwapRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/override-approve", OverrideApproveAsync)
            .WithName("OverrideApproveSwapRequest")
            .WithDescription("Manager/Admin duyệt đổi ca.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<SwapRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/override-reject", OverrideRejectAsync)
            .WithName("OverrideRejectSwapRequest")
            .WithDescription("Manager/Admin từ chối đổi ca.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<SwapRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        return group;
    }

    private static IResult Forbidden() =>
        Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Auth.Forbidden), statusCode: 403);

    private static IResult Unauthorized<T>() =>
        Results.Json(ApiResponse<T>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateSwapRequestRequest request,
        [FromServices] ISwapRequestService service,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<CreateSwapRequestRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.CreateAsync(request, currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] SwapRequestListRequest request,
        [FromServices] ISwapRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<PagedResponse<SwapRequestResponse>>();

        if (request.DepartmentId.HasValue &&
            !await scopeService.CanManageDepartmentAsync(currentUser.UserId.Value, currentUser.Role, request.DepartmentId.Value, cancellationToken))
            return Forbidden();

        // No departmentId provided — unscoped list is a known scope gap.
        var response = await service.ListAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        [FromServices] ISwapRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.GetByIdAsync(id, currentUser.UserId.Value, currentUser.Role, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> AcceptAsync(
        [FromRoute] Guid id,
        [FromBody] SwapActionRequest? request,
        [FromServices] ISwapRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.AcceptAsync(id, currentUser.UserId.Value, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> DeclineAsync(
        [FromRoute] Guid id,
        [FromBody] SwapActionRequest? request,
        [FromServices] ISwapRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.DeclineAsync(id, currentUser.UserId.Value, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CancelAsync(
        [FromRoute] Guid id,
        [FromBody] SwapActionRequest? request,
        [FromServices] ISwapRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<SwapRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.CancelAsync(id, currentUser.UserId.Value, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> OverrideApproveAsync(
        [FromRoute] Guid id,
        [FromBody] SwapActionRequest? request,
        [FromServices] ISwapRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<SwapRequestResponse>();

        if (!await scopeService.CanManageSwapRequestAsync(currentUser.UserId.Value, currentUser.Role, id, cancellationToken))
            return Forbidden();

        var response = await service.OverrideApproveAsync(id, currentUser.UserId.Value, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> OverrideRejectAsync(
        [FromRoute] Guid id,
        [FromBody] SwapActionRequest? request,
        [FromServices] ISwapRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<SwapRequestResponse>();

        if (!await scopeService.CanManageSwapRequestAsync(currentUser.UserId.Value, currentUser.Role, id, cancellationToken))
            return Forbidden();

        var response = await service.OverrideRejectAsync(id, currentUser.UserId.Value, request, cancellationToken);
        return response.ToHttpResult();
    }
}
