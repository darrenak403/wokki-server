using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Attendance;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Dtos.SwapRequest;
using Wokki.Application.Services.Attendance.Interfaces;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Application.Services.SwapRequest.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;

namespace Wokki.Api.Apis.EmployeeSelf;

/// <summary>
/// Self-service APIs for the logged-in employee (requires linked Employee profile).
/// </summary>
public static class EmployeeSelfEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeSelfApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/self")
            .MapEmployeeSelfRoutes()
            .WithTags("EmployeeSelf")
            .WithDescription("Nghiệp vụ self-service của nhân viên đang đăng nhập (lịch ca, đổi ca, chấm công).")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapEmployeeSelfRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/swap-requests", GetMySwapRequestsAsync)
            .WithName("GetMySwapRequests")
            .WithDescription("Yêu cầu đổi ca gửi/nhận của nhân viên đang đăng nhập.")
            .RequireAuthorization()
            .Produces<ApiResponse<IReadOnlyList<SwapRequestResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapGet("/schedule", GetMyScheduleAsync)
            .WithName("GetMySchedule")
            .WithDescription("Lịch ca của nhân viên đang đăng nhập (4 tuần tới).")
            .RequireAuthorization()
            .Produces<ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapGet("/attendance", GetMyAttendanceAsync)
            .WithName("GetMyAttendance")
            .WithDescription("Lịch sử chấm công của nhân viên đang đăng nhập.")
            .RequireAuthorization()
            .Produces<ApiResponse<IReadOnlyList<AttendanceResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> GetMySwapRequestsAsync(
        [FromServices] ISwapRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<IReadOnlyList<SwapRequestResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.ListMineAsync(currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetMyScheduleAsync(
        [FromServices] IScheduleService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<IReadOnlyList<ShiftAssignmentResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.GetMyScheduleAsync(currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetMyAttendanceAsync(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromServices] IAttendanceService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<IReadOnlyList<AttendanceResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.ListMineAsync(currentUser.UserId.Value, fromDate, toDate, cancellationToken);
        return response.ToHttpResult();
    }
}
