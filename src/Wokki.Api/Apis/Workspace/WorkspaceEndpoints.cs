using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Workspace;
using Wokki.Application.Services.Workspace.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Workspace;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceApi(this IEndpointRouteBuilder builder)
    {
        var usersGroup = builder.MapGroup("/api/v1/users")
            .WithTags("Workspace");

        usersGroup.MapPatch("/{id:guid}/role", ChangeRoleAsync)
            .WithName("ChangeUserRole")
            .WithDescription("Deprecated — use POST /api/v1/employees/{employeeId}/role-transition. Returns 400.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        var membershipGroup = builder.MapGroup("/api/v1/location-memberships")
            .WithTags("Workspace");

        membershipGroup.MapPost("/transfer", TransferLocationAsync)
            .WithName("TransferEmployeeLocation")
            .WithDescription("Transfer an employee to a different location (Admin or Manager with scope).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        var deptGroup = builder.MapGroup("/api/v1/department-memberships")
            .WithTags("Workspace");

        deptGroup.MapPost("/transfer", TransferDepartmentAsync)
            .WithName("TransferEmployeeDepartment")
            .WithDescription("Transfer an employee to a different department (Admin or Manager with scope).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        return builder;
    }

    private static async Task<IResult> ChangeRoleAsync(
        [FromRoute] Guid id,
        [FromBody] ChangeRoleRequest request,
        [FromServices] IWorkspaceService service,
        [FromServices] IValidator<ChangeRoleRequest> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        return (await service.ChangeRoleAsync(id, currentUser.UserId.Value, request, ct)).ToHttpResult();
    }

    private static async Task<IResult> TransferLocationAsync(
        [FromBody] TransferLocationRequest request,
        [FromServices] IWorkspaceService service,
        [FromServices] IValidator<TransferLocationRequest> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        return (await service.TransferLocationAsync(request, currentUser.UserId.Value, currentUser.Role ?? RoleConstants.User, ct)).ToHttpResult();
    }

    private static async Task<IResult> TransferDepartmentAsync(
        [FromBody] TransferDepartmentRequest request,
        [FromServices] IWorkspaceService service,
        [FromServices] IValidator<TransferDepartmentRequest> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        return (await service.TransferDepartmentAsync(request, currentUser.UserId.Value, currentUser.Role ?? RoleConstants.User, ct)).ToHttpResult();
    }
}
