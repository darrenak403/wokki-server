using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.OvertimeRequest;
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
        [FromQuery] Guid? shiftAssignmentId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] IOvertimeRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<PagedResponse<OvertimeRequestResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        return (await service.ListMyAsync(currentUser.UserId.Value, shiftAssignmentId, page, pageSize, ct)).ToHttpResult();
    }

    private static async Task<IResult> ListPendingAsync(
        [FromQuery] Guid? departmentId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] IOvertimeRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<PagedResponse<OvertimeRequestResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        return (await service.ListPendingAsync(currentUser.UserId.Value, isAdmin, departmentId, page, pageSize, ct)).ToHttpResult();
    }

    private static async Task<IResult> ApproveAsync(
        [FromRoute] Guid id,
        [FromBody] ReviewNoteRequest? request,
        [FromServices] IOvertimeRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        return (await service.ApproveAsync(id, currentUser.UserId.Value, isAdmin, request?.Note, ct)).ToHttpResult();
    }

    private static async Task<IResult> RejectAsync(
        [FromRoute] Guid id,
        [FromBody] ReviewNoteRequest? request,
        [FromServices] IOvertimeRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<OvertimeRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        return (await service.RejectAsync(id, currentUser.UserId.Value, isAdmin, request?.Note, ct)).ToHttpResult();
    }
}

public sealed record ReviewNoteRequest(string? Note);
