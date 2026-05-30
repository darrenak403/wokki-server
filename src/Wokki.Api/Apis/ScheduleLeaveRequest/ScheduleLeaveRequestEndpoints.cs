using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.ScheduleLeaveRequest;

public static class ScheduleLeaveRequestEndpoints
{
    public static IEndpointRouteBuilder MapScheduleLeaveRequestApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/leave-requests")
            .MapScheduleLeaveRequestRoutes()
            .WithTags("ScheduleLeaveRequest");

        return builder;
    }

    public static RouteGroupBuilder MapScheduleLeaveRequestRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListAsync)
            .WithName("ListScheduleLeaveRequests")
            .WithDescription("Danh sách đơn xin nghỉ theo lịch (Admin/Manager).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .WithName("ApproveScheduleLeaveRequest")
            .WithDescription("Duyệt đơn xin nghỉ — cập nhật Unavailable và gỡ phân ca conflict.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<ScheduleLeaveRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/reject", RejectAsync)
            .WithName("RejectScheduleLeaveRequest")
            .WithDescription("Từ chối đơn xin nghỉ.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<ScheduleLeaveRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> ListAsync(
        [FromQuery] Guid? scheduleId,
        [FromQuery] string? status,
        [FromServices] IScheduleLeaveRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);

        var response = await service.ListForReviewAsync(
            currentUser.UserId.Value,
            currentUser.Role,
            scheduleId,
            status,
            managedLocationIds,
            cancellationToken);

        return response.ToHttpResult();
    }

    private static async Task<IResult> ApproveAsync(
        [FromRoute] Guid id,
        [FromBody] ReviewScheduleLeaveRequest? request,
        [FromServices] IScheduleLeaveRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);

        var response = await service.ApproveAsync(
            id,
            currentUser.UserId.Value,
            currentUser.Role,
            managedLocationIds,
            request,
            cancellationToken);

        return response.ToHttpResult();
    }

    private static async Task<IResult> RejectAsync(
        [FromRoute] Guid id,
        [FromBody] ReviewScheduleLeaveRequest? request,
        [FromServices] IScheduleLeaveRequestService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Results.Json(ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);

        var response = await service.RejectAsync(
            id,
            currentUser.UserId.Value,
            currentUser.Role,
            managedLocationIds,
            request,
            cancellationToken);

        return response.ToHttpResult();
    }
}
