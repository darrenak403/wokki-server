using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Shift;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Application.Services.Shift.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Shifts;

public static class ShiftEndpoints
{
    public static IEndpointRouteBuilder MapShiftApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/shifts")
            .MapShiftRoutes()
            .WithTags("Shifts")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapShiftRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListAsync)
            .WithName("ListShifts")
            .WithDescription("Danh sách ca làm theo location/department.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<IReadOnlyList<ShiftDefinitionResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/copy", CopyAsync)
            .WithName("CopyShifts")
            .WithDescription("Sao chép ca làm đang hoạt động từ phòng ban nguồn sang các phòng ban đích (cùng chi nhánh).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<CopyShiftDefinitionsResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateShift")
            .WithDescription("Tạo định nghĩa ca.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ShiftDefinitionResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateShift")
            .WithDescription("Cập nhật định nghĩa ca.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<ShiftDefinitionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteShift")
            .WithDescription("Vô hiệu hóa định nghĩa ca (soft delete).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
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
        [FromQuery] Guid locationId,
        [FromQuery] Guid? departmentId,
        [FromServices] IShiftDefinitionService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<IReadOnlyList<ShiftDefinitionResponse>>();

        if (!await scopeService.CanManageLocationAsync(currentUser.UserId.Value, currentUser.Role, locationId, cancellationToken))
            return Forbidden();

        var response = await service.ListAsync(locationId, departmentId, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateShiftDefinitionRequest request,
        [FromServices] IShiftDefinitionService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<CreateShiftDefinitionRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<ShiftDefinitionResponse>();

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (!await scopeService.CanManageLocationAsync(currentUser.UserId.Value, currentUser.Role, request.LocationId, cancellationToken))
            return Forbidden();

        var response = await service.CreateAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CopyAsync(
        [FromBody] CopyShiftDefinitionsRequest request,
        [FromServices] IShiftDefinitionService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<CopyShiftDefinitionsRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<CopyShiftDefinitionsResponse>();

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (!await scopeService.CanManageLocationAsync(
                currentUser.UserId.Value,
                currentUser.Role,
                request.LocationId,
                cancellationToken))
        {
            return Forbidden();
        }

        var response = await service.CopyAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateShiftDefinitionRequest request,
        [FromServices] IShiftDefinitionService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<UpdateShiftDefinitionRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<ShiftDefinitionResponse>();

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (!await scopeService.CanManageShiftAsync(currentUser.UserId.Value, currentUser.Role, id, cancellationToken))
            return Forbidden();

        var response = await service.UpdateAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> DeleteAsync(
        [FromRoute] Guid id,
        [FromServices] IShiftDefinitionService service,
        [FromServices] ILocationScopeService scopeService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null || currentUser.Role is null)
            return Unauthorized<object>();

        if (!await scopeService.CanManageShiftAsync(currentUser.UserId.Value, currentUser.Role, id, cancellationToken))
            return Forbidden();

        var response = await service.DeleteAsync(id, cancellationToken);
        return response.ToHttpResult();
    }
}
