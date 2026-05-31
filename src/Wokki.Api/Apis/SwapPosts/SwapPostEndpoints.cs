using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.SwapPost;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Application.Services.SwapPost.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Enums;

namespace Wokki.Api.Apis.SwapPosts;

public static class SwapPostEndpoints
{
    public static IEndpointRouteBuilder MapSwapPostApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/swap-posts")
            .MapSwapPostRoutes()
            .WithTags("SwapPosts")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapSwapPostRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/feed", ListFeedAsync)
            .WithName("ListSwapPostFeed")
            .WithDescription("Bảng tin đổi ca (Pending) trong phòng ban — lịch Draft.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<PagedResponse<SwapPostResponse>>>(StatusCodes.Status200OK);

        group.MapGet("/mine", ListMineAsync)
            .WithName("ListMySwapPosts")
            .WithDescription("Bài đổi ca của nhân viên đang đăng nhập.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<PagedResponse<SwapPostResponse>>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateAsync)
            .WithName("CreateSwapPost")
            .WithDescription("Đăng bài nhường ca hoặc đổi chéo trên lịch Draft.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<SwapPostResponse>>(StatusCodes.Status201Created);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetSwapPostById")
            .WithDescription("Chi tiết bài đổi ca.")
            .RequireAuthorization()
            .Produces<ApiResponse<SwapPostResponse>>(StatusCodes.Status200OK);

        group.MapPost("/{id:guid}/accept", AcceptAsync)
            .WithName("AcceptSwapPost")
            .WithDescription("Nhận ca / đổi chéo — FCFS, áp dụng ngay trên lịch Draft.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<SwapPostResponse>>(StatusCodes.Status200OK);

        group.MapPost("/{id:guid}/accept/preview", PreviewAcceptAsync)
            .WithName("PreviewAcceptSwapPost")
            .WithDescription("Kiểm tra policy trước khi accept.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<SwapPostAcceptPreviewResponse>>(StatusCodes.Status200OK);

        group.MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("CancelSwapPost")
            .WithDescription("Hủy bài đang Pending (author).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<SwapPostResponse>>(StatusCodes.Status200OK);

        group.MapGet("/admin/feed", ListAdminFeedAsync)
            .WithName("ListSwapPostAdminFeed")
            .WithDescription("Bảng tin đổi ca (Pending) — Admin/Manager xem theo chi nhánh/phòng ban.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<PagedResponse<SwapPostResponse>>>(StatusCodes.Status200OK);

        group.MapGet("/audit", ListAuditAsync)
            .WithName("ListSwapPostAudit")
            .WithDescription("Nhật ký đổi ca đã hoàn thành (Admin/Manager).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<PagedResponse<SwapPostAuditResponse>>>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> ListFeedAsync(
        [FromQuery] Guid scheduleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ISwapPostService service = null!,
        [FromServices] ICurrentUserService currentUser = null!,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.ListFeedAsync(
            scheduleId,
            currentUser.UserId.Value,
            currentUser.Role,
            page,
            pageSize,
            cancellationToken);

        return response.ToHttpResult();
    }

    private static async Task<IResult> ListMineAsync(
        [FromQuery] Guid? scheduleId,
        [FromQuery] SwapPostStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ISwapPostService service = null!,
        [FromServices] ICurrentUserService currentUser = null!,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.ListMineAsync(
            currentUser.UserId.Value,
            scheduleId,
            status,
            page,
            pageSize,
            cancellationToken);

        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateSwapPostRequest request,
        [FromServices] ISwapPostService service,
        [FromServices] IValidator<CreateSwapPostRequest> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult;

        var response = await service.CreateAsync(request, currentUser.UserId.Value, currentUser.Role, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        [FromServices] ISwapPostService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.GetByIdAsync(id, currentUser.UserId.Value, currentUser.Role, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> AcceptAsync(
        [FromRoute] Guid id,
        [FromBody] AcceptSwapPostRequest? request,
        [FromServices] ISwapPostService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.AcceptAsync(
            id,
            request ?? new AcceptSwapPostRequest(null),
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);

        return response.ToHttpResult();
    }

    private static async Task<IResult> PreviewAcceptAsync(
        [FromRoute] Guid id,
        [FromBody] AcceptSwapPostRequest? request,
        [FromServices] ISwapPostService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<SwapPostAcceptPreviewResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.PreviewAcceptAsync(
            id,
            request ?? new AcceptSwapPostRequest(null),
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);

        return response.ToHttpResult();
    }

    private static async Task<IResult> CancelAsync(
        [FromRoute] Guid id,
        [FromServices] ISwapPostService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<SwapPostResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.CancelAsync(id, currentUser.UserId.Value, currentUser.Role, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ListAdminFeedAsync(
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateOnly? weekStartDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ISwapPostService service = null!,
        [FromServices] ILocationScopeService scopeService = null!,
        [FromServices] ICurrentUserService currentUser = null!,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<PagedResponse<SwapPostResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);

        var response = await service.ListAdminFeedAsync(
            locationId,
            departmentId,
            weekStartDate,
            currentUser.UserId.Value,
            currentUser.Role,
            managedLocationIds,
            page,
            pageSize,
            cancellationToken);

        return response.ToHttpResult();
    }

    private static async Task<IResult> ListAuditAsync(
        [FromQuery] Guid? scheduleId,
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateOnly? weekStartDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ISwapPostService service = null!,
        [FromServices] ILocationScopeService scopeService = null!,
        [FromServices] ICurrentUserService currentUser = null!,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<PagedResponse<SwapPostAuditResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);

        var response = await service.ListAuditAsync(
            scheduleId,
            locationId,
            departmentId,
            weekStartDate,
            currentUser.UserId.Value,
            currentUser.Role,
            managedLocationIds,
            page,
            pageSize,
            cancellationToken);

        return response.ToHttpResult();
    }
}
