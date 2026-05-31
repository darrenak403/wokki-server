using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.OvertimeRequest;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Application.Services.OvertimeRequest.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.OvertimeRequest;

public static class OvertimeRequestEndpoints
{
    public static IEndpointRouteBuilder MapOvertimeRequestApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/overtime-requests")
            .MapOvertimeRequestRoutes()
            .WithTags("OvertimeRequest");

        return builder;
    }

    public static RouteGroupBuilder MapOvertimeRequestRoutes(this RouteGroupBuilder group)
    {
        // User endpoints
        group.MapPost("/", SubmitAsync)
            .WithName("SubmitOvertimeRequest")
            .WithDescription("Gửi yêu cầu OT sau khi ca kết thúc.")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<OvertimeRequestResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/clock-out", ClockOutOTAsync)
            .WithName("ClockOutOT")
            .WithDescription("Clock-out OT session đang chạy.")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<OvertimeRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/my", ListMyAsync)
            .WithName("ListMyOvertimeRequests")
            .WithDescription("Danh sách OT request của bản thân.")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<PagedResponse<OvertimeRequestResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        // Manager / Admin endpoints
        group.MapGet("/", ListAllAsync)
            .WithName("ListAllOvertimeRequests")
            .WithDescription("Danh sách tất cả OT request theo tháng (Manager/Admin).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<PagedResponse<OvertimeRequestResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/pending", ListPendingAsync)
            .WithName("ListPendingOvertimeRequests")
            .WithDescription("Danh sách OT request chờ duyệt (Manager/Admin).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<PagedResponse<OvertimeRequestResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .WithName("ApproveOvertimeRequest")
            .WithDescription("Duyệt yêu cầu OT.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<OvertimeRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/reject", RejectAsync)
            .WithName("RejectOvertimeRequest")
            .WithDescription("Từ chối yêu cầu OT.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<OvertimeRequestResponse>>(StatusCodes.Status200OK)
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

    private static async Task<IResult> SubmitAsync(
        [FromBody] SubmitOvertimeRequestDto dto,
        [FromServices] IOvertimeRequestService service,
        [FromServices] IValidator<SubmitOvertimeRequestDto> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (!dto.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        return (await service.SubmitAsync(currentUser.UserId.Value, dto, ct)).ToHttpResult();
    }

    private static async Task<IResult> ClockOutOTAsync(
        [FromRoute] Guid id,
        [FromServices] IOvertimeRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        return (await service.ClockOutOTAsync(id, currentUser.UserId.Value, ct)).ToHttpResult();
    }

    private static async Task<IResult> ListMyAsync(
        [FromServices] IOvertimeRequestService service,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] Guid? shiftAssignmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<PagedResponse<OvertimeRequestResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (pageSize < 1 || pageSize > 100)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Validation.InvalidPageSize), statusCode: 400);

        if (page < 1)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Validation.InvalidPage), statusCode: 400);

        return (await service.ListMyAsync(currentUser.UserId.Value, shiftAssignmentId, page, pageSize, ct)).ToHttpResult();
    }

    private static async Task<IResult> ListAllAsync(
        [FromServices] IOvertimeRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] Guid? departmentId,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<PagedResponse<OvertimeRequestResponse>>();

        var effectiveMonth = month ?? DateTimeOffset.UtcNow.Month;
        var effectiveYear = year ?? DateTimeOffset.UtcNow.Year;

        if (effectiveMonth < 1 || effectiveMonth > 12)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Validation.Failed), statusCode: 400);

        if (effectiveYear < 2020 || effectiveYear > 2100)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Validation.Failed), statusCode: 400);

        if (page < 1)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Validation.InvalidPage), statusCode: 400);

        if (pageSize < 1 || pageSize > 100)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Validation.InvalidPageSize), statusCode: 400);

        if (departmentId.HasValue &&
            !await scopeService.CanManageDepartmentAsync(currentUser.UserId.Value, currentUser.Role, departmentId.Value, ct))
            return Forbidden();

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(currentUser.UserId.Value, currentUser.Role, ct);
        return (await service.ListAllAsync(
            currentUser.UserId.Value,
            isAdmin,
            departmentId,
            effectiveMonth,
            effectiveYear,
            page,
            pageSize,
            managedLocationIds,
            ct)).ToHttpResult();
    }

    private static async Task<IResult> ListPendingAsync(
        [FromServices] IOvertimeRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] Guid? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<PagedResponse<OvertimeRequestResponse>>();

        if (pageSize < 1 || pageSize > 100)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Validation.InvalidPageSize), statusCode: 400);

        if (page < 1)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Validation.InvalidPage), statusCode: 400);

        if (departmentId.HasValue &&
            !await scopeService.CanManageDepartmentAsync(currentUser.UserId.Value, currentUser.Role, departmentId.Value, ct))
            return Forbidden();

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(currentUser.UserId.Value, currentUser.Role, ct);
        return (await service.ListPendingAsync(
            currentUser.UserId.Value,
            isAdmin,
            departmentId,
            page,
            pageSize,
            managedLocationIds,
            ct)).ToHttpResult();
    }

    private static async Task<IResult> ApproveAsync(
        [FromRoute] Guid id,
        [FromBody] ReviewNoteRequest? request,
        [FromServices] IOvertimeRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<OvertimeRequestResponse>();

        if (!await scopeService.CanManageOvertimeRequestAsync(currentUser.UserId.Value, currentUser.Role, id, ct))
            return Forbidden();

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        return (await service.ApproveAsync(id, currentUser.UserId.Value, isAdmin, request?.Note, ct)).ToHttpResult();
    }

    private static async Task<IResult> RejectAsync(
        [FromRoute] Guid id,
        [FromBody] ReviewNoteRequest? request,
        [FromServices] IOvertimeRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<OvertimeRequestResponse>();

        if (!await scopeService.CanManageOvertimeRequestAsync(currentUser.UserId.Value, currentUser.Role, id, ct))
            return Forbidden();

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        return (await service.RejectAsync(id, currentUser.UserId.Value, isAdmin, request?.Note, ct)).ToHttpResult();
    }
}

public sealed record ReviewNoteRequest(string? Note);
