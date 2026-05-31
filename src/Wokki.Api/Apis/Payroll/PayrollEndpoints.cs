using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Payroll;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Application.Services.Payroll.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Payroll;

public static class PayrollEndpoints
{
    public static IEndpointRouteBuilder MapPayrollApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/payroll")
            .MapPayrollRoutes()
            .WithTags("Payroll")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapPayrollRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/summary", GetSummaryAsync)
            .WithName("GetPayrollSummary")
            .WithDescription("Tổng hợp lương theo phòng ban và kỳ lương.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<PayrollSummaryResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/summary/{employeeId:guid}", GetEmployeeSummaryAsync)
            .WithName("GetPayrollEmployeeSummary")
            .WithDescription("Chi tiết lương từng nhân viên trong kỳ.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<PayrollEmployeeDetailResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/summary/export", ExportSummaryAsync)
            .WithName("ExportPayrollSummary")
            .WithDescription("Xuất CSV tổng hợp lương.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/periods/{id:guid}/lock", LockPeriodAsync)
            .WithName("LockPayPeriod")
            .WithDescription("Chốt kỳ lương và tạo snapshot PayrollLine.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<PayrollSummaryResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/my-summary", GetMySummaryAsync)
            .WithName("GetMyPayrollSummary")
            .WithDescription("Tổng lương tháng của nhân viên đang đăng nhập.")
            .RequireAuthorization()
            .Produces<ApiResponse<MyPayrollSummaryResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        group.MapPatch("/periods/{payPeriodId:guid}/employees/{employeeId:guid}/paid", SetLinePaidAsync)
            .WithName("SetPayrollLinePaid")
            .WithDescription("Đánh dấu đã chuyển lương cho nhân viên trong kỳ đã chốt.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<PayrollEmployeeLineResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static IResult Forbidden() =>
        Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Auth.Forbidden), statusCode: 403);

    private static IResult Unauthorized<T>() =>
        Results.Json(ApiResponse<T>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

    private static async Task<IResult> GetSummaryAsync(
        [AsParameters] PayrollPeriodRequest request,
        [FromServices] IPayrollService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<PayrollPeriodRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<PayrollSummaryResponse>();

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (!await scopeService.CanManageDepartmentAsync(currentUser.UserId.Value, currentUser.Role, request.DepartmentId, cancellationToken))
            return Forbidden();

        var response = await service.GetSummaryAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetEmployeeSummaryAsync(
        [FromRoute] Guid employeeId,
        [AsParameters] PayrollPeriodRequest request,
        [FromServices] IPayrollService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<PayrollPeriodRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<PayrollEmployeeDetailResponse>();

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (!await scopeService.CanManageEmployeeAsync(currentUser.UserId.Value, currentUser.Role, employeeId, cancellationToken))
            return Forbidden();

        var response = await service.GetEmployeeDetailAsync(employeeId, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ExportSummaryAsync(
        [FromBody] PayrollPeriodRequest request,
        [FromServices] IPayrollService service,
        [FromServices] IValidator<PayrollPeriodRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.ExportCsvAsync(request, cancellationToken);
        if (!response.Success || response.Data is null)
            return response.ToHttpResult();

        return Results.File(response.Data.Content, response.Data.ContentType, response.Data.FileName);
    }

    private static async Task<IResult> LockPeriodAsync(
        [FromRoute] Guid id,
        [FromServices] IPayrollService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<PayrollSummaryResponse>();

        var response = await service.LockPeriodAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetMySummaryAsync(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromServices] IPayrollService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Unauthorized<MyPayrollSummaryResponse>();

        var response = await service.GetMySummaryAsync(currentUser.UserId.Value, startDate, endDate, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> SetLinePaidAsync(
        [FromRoute] Guid payPeriodId,
        [FromRoute] Guid employeeId,
        [FromBody] SetPayrollLinePaidRequest request,
        [FromServices] IPayrollService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Unauthorized<PayrollEmployeeLineResponse>();

        var response = await service.SetLinePaidAsync(
            payPeriodId,
            employeeId,
            request.Paid,
            currentUser.UserId.Value,
            cancellationToken);
        return response.ToHttpResult();
    }
}
