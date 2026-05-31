using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Attendance;
using Wokki.Application.Services.Attendance.Interfaces;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Attendance;

public static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/attendance")
            .MapAttendanceRoutes()
            .WithTags("Attendance");

        return builder;
    }

    public static RouteGroupBuilder MapAttendanceRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/clock-in", ClockInAsync)
            .WithName("ClockIn")
            .WithDescription("Ghi nhận clock-in cho ca hôm nay.")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Clock)
            .Produces<ApiResponse<AttendanceResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/clock-out", ClockOutAsync)
            .WithName("ClockOut")
            .WithDescription("Ghi nhận clock-out và tính thời gian làm việc.")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Clock)
            .Produces<ApiResponse<AttendanceResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapGet("/", ListAsync)
            .WithName("ListAttendance")
            .WithDescription("Danh sách chấm công (Admin/Manager).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<PagedResponse<AttendanceResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}/adjust", AdjustAsync)
            .WithName("AdjustAttendance")
            .WithDescription("Điều chỉnh thời gian chấm công (có ghi chú audit).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<AttendanceResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static IResult Forbidden() =>
        Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Auth.Forbidden), statusCode: 403);

    private static IResult Unauthorized<T>() =>
        Results.Json(ApiResponse<T>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

    private static async Task<IResult> ClockInAsync(
        [FromBody] ClockInRequest? request,
        [FromServices] IAttendanceService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.ClockInAsync(currentUser.UserId.Value, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ClockOutAsync(
        [FromServices] IAttendanceService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<AttendanceResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.ClockOutAsync(currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] AttendanceListRequest request,
        [FromServices] IAttendanceService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<PagedResponse<AttendanceResponse>>();

        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);
        var response = await service.ListAsync(request, managedLocationIds, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> AdjustAsync(
        [FromRoute] Guid id,
        [FromBody] AdjustAttendanceRequest request,
        [FromServices] IAttendanceService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] IValidator<AdjustAttendanceRequest> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<AttendanceResponse>();

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (!await scopeService.CanManageAttendanceAsync(currentUser.UserId.Value, currentUser.Role, id, cancellationToken))
            return Forbidden();

        var response = await service.AdjustAsync(id, request, currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }
}
