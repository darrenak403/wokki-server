using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Employee;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Employees;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/employees")
            .MapEmployeeRoutes()
            .WithTags("Employees")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapEmployeeRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListAsync)
            .WithName("ListEmployees")
            .WithDescription("Danh sách nhân viên (phân trang, lọc theo department/location, tìm theo tên/email/SĐT).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<PagedResponse<EmployeeResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetEmployeeById")
            .WithDescription("Chi tiết nhân viên.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<EmployeeResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/department-memberships", ListDepartmentMembershipsAsync)
            .WithName("ListEmployeeDepartmentMemberships")
            .WithDescription("Lịch sử thuộc phòng ban của nhân viên (JoinedAt, LeftAt, trạng thái).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<IReadOnlyList<EmployeeDepartmentMembershipResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateEmployee")
            .WithDescription("Tạo nhân viên và tài khoản User liên kết.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<CreateEmployeeResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateEmployee")
            .WithDescription("Cập nhật hồ sơ nhân viên.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<EmployeeResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteEmployee")
            .WithDescription("Chấm dứt nhân viên (soft delete — set TerminatedAt).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
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

    private static async Task<IResult> ListAsync(
        [AsParameters] EmployeeListRequest request,
        [FromServices] IEmployeeService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<PagedResponse<EmployeeResponse>>();

        if (request.LocationId.HasValue &&
            !await scopeService.CanManageLocationAsync(currentUser.UserId.Value, currentUser.Role, request.LocationId.Value, cancellationToken))
            return Forbidden();

        if (request.DepartmentId.HasValue &&
            !await scopeService.CanManageDepartmentAsync(currentUser.UserId.Value, currentUser.Role, request.DepartmentId.Value, cancellationToken))
            return Forbidden();

        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);
        var response = await service.ListAsync(request, managedLocationIds, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        [FromServices] IEmployeeService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<EmployeeResponse>();

        if (!await scopeService.CanManageEmployeeAsync(currentUser.UserId.Value, currentUser.Role, id, cancellationToken))
            return Forbidden();

        var response = await service.GetByIdAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ListDepartmentMembershipsAsync(
        [FromRoute] Guid id,
        [FromServices] IEmployeeService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<IReadOnlyList<EmployeeDepartmentMembershipResponse>>();

        if (!await scopeService.CanManageEmployeeAsync(currentUser.UserId.Value, currentUser.Role, id, cancellationToken))
            return Forbidden();

        var response = await service.ListDepartmentMembershipsAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateEmployeeRequest request,
        [FromServices] IEmployeeService service,
        [FromServices] IValidator<CreateEmployeeRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.CreateAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateEmployeeRequest request,
        [FromServices] IEmployeeService service,
        [FromServices] IValidator<UpdateEmployeeRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.UpdateAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> DeleteAsync(
        [FromRoute] Guid id,
        [FromServices] IEmployeeService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.DeleteAsync(id, cancellationToken);
        return response.ToHttpResult();
    }
}
