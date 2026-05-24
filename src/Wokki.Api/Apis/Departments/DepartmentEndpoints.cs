using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Dtos.Department;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Services.Department.Interfaces;
using Wokki.Application.Services.Schedule.Interfaces;
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

        group.MapGet("/{id:guid}/job-positions", ListJobPositionsAsync)
            .WithName("ListDepartmentJobPositions")
            .WithDescription("Danh sách vị trí làm việc (số lượng mục tiêu).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<IReadOnlyList<JobPositionResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/job-positions", CreateJobPositionAsync)
            .WithName("CreateDepartmentJobPosition")
            .WithDescription("Tạo vị trí làm việc.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<JobPositionResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/job-positions/{jobPositionId:guid}", UpdateJobPositionAsync)
            .WithName("UpdateDepartmentJobPosition")
            .WithDescription("Cập nhật vị trí làm việc.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<JobPositionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/job-positions/{jobPositionId:guid}", DeleteJobPositionAsync)
            .WithName("DeleteDepartmentJobPosition")
            .WithDescription("Vô hiệu hóa vị trí làm việc.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> ListAsync(
        [FromQuery] Guid? locationId,
        [FromServices] IDepartmentService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.ListAsync(locationId, cancellationToken);
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

    private static async Task<IResult> ListJobPositionsAsync(
        [FromRoute] Guid id,
        [FromServices] IDepartmentSchedulingConfigService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.ListJobPositionsAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateJobPositionAsync(
        [FromRoute] Guid id,
        [FromBody] CreateJobPositionRequest request,
        [FromServices] IDepartmentSchedulingConfigService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.CreateJobPositionAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UpdateJobPositionAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid jobPositionId,
        [FromBody] UpdateJobPositionRequest request,
        [FromServices] IDepartmentSchedulingConfigService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.UpdateJobPositionAsync(id, jobPositionId, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> DeleteJobPositionAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid jobPositionId,
        [FromServices] IDepartmentSchedulingConfigService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.DeleteJobPositionAsync(id, jobPositionId, cancellationToken);
        return response.ToHttpResult();
    }
}
