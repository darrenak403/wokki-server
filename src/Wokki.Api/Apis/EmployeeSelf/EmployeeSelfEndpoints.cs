using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Attendance;
using Wokki.Application.Dtos.Employee;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Dtos.SwapRequest;
using Wokki.Application.Services.Attendance.Interfaces;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Application.Services.SwapRequest.Interfaces;
using Wokki.Application.Validators.Employee;
using Wokki.Application.Validators.Schedule;
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
        builder.MapGet("/api/v1/self/schedule-preferences/draft/{weekStartDate}", GetMyDraftScheduleForPreferencesAsync)
            .WithName("GetMyDraftScheduleForPreferences")
            .WithTags("EmployeeSelf")
            .WithDescription("Lịch Draft của phòng ban nhân viên cho tuần (thứ Hai weekStartDate yyyy-MM-dd).")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<EmployeeDraftScheduleResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        builder.MapGet("/api/v1/self/schedule-preferences/week/{weekStartDate}", GetMyScheduleForPreferencesAsync)
            .WithName("GetMyScheduleForPreferences")
            .WithTags("EmployeeSelf")
            .WithDescription("Lịch Draft hoặc Published của phòng ban nhân viên cho màn đăng ký ca.")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<EmployeeDraftScheduleResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

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

        group.MapGet("/schedule-preferences/{scheduleId:guid}", GetMySchedulePreferencesAsync)
            .WithName("GetMySchedulePreferences")
            .WithDescription("Đăng ký ca mong muốn của nhân viên cho lịch Draft.")
            .RequireAuthorization()
            .Produces<ApiResponse<MySchedulePreferenceResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/schedule-preferences/{scheduleId:guid}", SaveMySchedulePreferencesAsync)
            .WithName("SaveMySchedulePreferences")
            .WithDescription("Lưu nháp đăng ký ca (Draft).")
            .RequireAuthorization()
            .Produces<ApiResponse<MySchedulePreferenceResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/schedule-preferences/{scheduleId:guid}/submit", SubmitMySchedulePreferencesAsync)
            .WithName("SubmitMySchedulePreferences")
            .WithDescription("Gửi đăng ký ca (khóa chỉnh sửa).")
            .RequireAuthorization()
            .Produces<ApiResponse<MySchedulePreferenceResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapGet("/attendance", GetMyAttendanceAsync)
            .WithName("GetMyAttendance")
            .WithDescription("Lịch sử chấm công của nhân viên đang đăng nhập.")
            .RequireAuthorization()
            .Produces<ApiResponse<IReadOnlyList<AttendanceResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapGet("/profile", GetMyProfileAsync)
            .WithName("GetMyProfile")
            .WithDescription("Hồ sơ cá nhân của nhân viên đang đăng nhập.")
            .RequireAuthorization()
            .Produces<ApiResponse<EmployeeResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/profile", UpdateMyProfileAsync)
            .WithName("UpdateMyProfile")
            .WithDescription("Cập nhật hồ sơ cá nhân: họ tên, SĐT, thông tin ngân hàng.")
            .RequireAuthorization()
            .Produces<ApiResponse<EmployeeResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/profile/payment-qr", UploadMyPaymentQrAsync)
            .WithName("UploadMyPaymentQr")
            .WithDescription("Upload ảnh QR chuyển khoản lương (Cloudinary, tối đa 5MB).")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<ApiResponse<PaymentQrUploadResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/leave-requests", CreateMyLeaveRequestAsync)
            .WithName("CreateMyScheduleLeaveRequest")
            .WithDescription("Gửi đơn xin nghỉ ca (chỉ khi lịch tuần còn Draft).")
            .RequireAuthorization()
            .Produces<ApiResponse<ScheduleLeaveRequestResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/leave-requests", ListMyLeaveRequestsAsync)
            .WithName("ListMyScheduleLeaveRequests")
            .WithDescription("Danh sách đơn xin nghỉ của nhân viên.")
            .RequireAuthorization()
            .Produces<ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/leave-requests/{id:guid}", CancelMyLeaveRequestAsync)
            .WithName("CancelMyScheduleLeaveRequest")
            .WithDescription("Huỷ đơn xin nghỉ đang chờ duyệt.")
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

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

    private static async Task<IResult> GetMyDraftScheduleForPreferencesAsync(
        [FromRoute] string weekStartDate,
        [FromServices] ISchedulePreferenceService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!DateOnly.TryParse(weekStartDate, out var parsedWeekStartDate))
            return Results.Json(ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Schedule.WeekNotMonday), statusCode: 400);

        var response = await service.GetDraftScheduleForEmployeeAsync(
            currentUser.UserId.Value,
            parsedWeekStartDate,
            cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetMyScheduleForPreferencesAsync(
        [FromRoute] string weekStartDate,
        [FromServices] ISchedulePreferenceService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!DateOnly.TryParse(weekStartDate, out var parsedWeekStartDate))
            return Results.Json(ApiResponse<EmployeeDraftScheduleResponse?>.FailureResponse(AppMessages.Schedule.WeekNotMonday), statusCode: 400);

        var response = await service.GetScheduleForEmployeePreferencesAsync(
            currentUser.UserId.Value,
            parsedWeekStartDate,
            cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetMySchedulePreferencesAsync(
        [FromRoute] Guid scheduleId,
        [FromServices] ISchedulePreferenceService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.GetMineAsync(currentUser.UserId.Value, scheduleId, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> SaveMySchedulePreferencesAsync(
        [FromRoute] Guid scheduleId,
        [FromBody] SaveSchedulePreferencesRequest request,
        [FromServices] ISchedulePreferenceService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.SaveMineAsync(currentUser.UserId.Value, scheduleId, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> SubmitMySchedulePreferencesAsync(
        [FromRoute] Guid scheduleId,
        [FromServices] ISchedulePreferenceService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<MySchedulePreferenceResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.SubmitMineAsync(currentUser.UserId.Value, scheduleId, cancellationToken);
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

    private static async Task<IResult> GetMyProfileAsync(
        [FromServices] IEmployeeSelfProfileService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.GetMineAsync(currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UpdateMyProfileAsync(
        [FromBody] UpdateMyProfileRequest request,
        [FromServices] IEmployeeSelfProfileService service,
        [FromServices] ICurrentUserService currentUser,
        IValidator<UpdateMyProfileRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.UpdateMineAsync(currentUser.UserId.Value, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UploadMyPaymentQrAsync(
        IFormFile? file,
        [FromServices] IEmployeeSelfProfileService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<PaymentQrUploadResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (file is null || file.Length == 0)
            return Results.Json(ApiResponse<PaymentQrUploadResponse>.FailureResponse(AppMessages.Self.PaymentQrInvalid), statusCode: 400);

        await using var stream = file.OpenReadStream();
        var response = await service.UploadPaymentQrAsync(
            currentUser.UserId.Value,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateMyLeaveRequestAsync(
        [FromBody] CreateScheduleLeaveRequest request,
        [FromServices] IScheduleLeaveRequestService service,
        [FromServices] IValidator<CreateScheduleLeaveRequest> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<ScheduleLeaveRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.CreateMineAsync(currentUser.UserId.Value, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ListMyLeaveRequestsAsync(
        [FromQuery] Guid? scheduleId,
        [FromServices] IScheduleLeaveRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<IReadOnlyList<ScheduleLeaveRequestResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.ListMineAsync(currentUser.UserId.Value, scheduleId, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CancelMyLeaveRequestAsync(
        [FromRoute] Guid id,
        [FromServices] IScheduleLeaveRequestService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var response = await service.CancelMineAsync(currentUser.UserId.Value, id, cancellationToken);
        return response.ToHttpResult();
    }
}
