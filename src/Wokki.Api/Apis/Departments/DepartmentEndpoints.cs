using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Department;
using Wokki.Application.Services.Department.Interfaces;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Departments;

public static class DepartmentEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/departments")
            .MapDepartmentRoutes()
            .WithTags("Departments")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapDepartmentRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListAsync)
            .WithName("ListDepartments")
            .WithDescription("Danh sách phòng ban (lọc theo locationId).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<IReadOnlyList<DepartmentResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateAsync)
            .WithName("CreateDepartment")
            .WithDescription("Tạo phòng ban.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<DepartmentResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateDepartment")
            .WithDescription("Cập nhật phòng ban.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<DepartmentResponse>>(StatusCodes.Status200OK)
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
        [FromQuery] Guid? locationId,
        [FromServices] IDepartmentService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<IReadOnlyList<DepartmentResponse>>();

        if (locationId.HasValue &&
            !await scopeService.CanManageLocationAsync(currentUser.UserId.Value, currentUser.Role, locationId.Value, cancellationToken))
            return Forbidden();

        var managedLocationIds = await scopeService.GetManagedLocationIdsAsync(
            currentUser.UserId.Value,
            currentUser.Role,
            cancellationToken);
        var response = await service.ListAsync(locationId, managedLocationIds, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateDepartmentRequest request,
        [FromServices] IDepartmentService service,
        [FromServices] IValidator<CreateDepartmentRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.CreateAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateDepartmentRequest request,
        [FromServices] IDepartmentService service,
        [FromServices] IValidator<UpdateDepartmentRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.UpdateAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }
}
