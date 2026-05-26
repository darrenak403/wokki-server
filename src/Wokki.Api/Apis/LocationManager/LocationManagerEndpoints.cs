using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Location;
using Wokki.Application.Dtos.LocationManager;
using Wokki.Application.Services.LocationManager.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.LocationManager;

public static class LocationManagerEndpoints
{
    public static IEndpointRouteBuilder MapLocationManagerApi(this IEndpointRouteBuilder builder)
    {
        var locationsGroup = builder.MapGroup("/api/v1/locations")
            .WithTags("LocationManager");

        locationsGroup.MapPost("/{id:guid}/managers", AssignAsync)
            .WithName("AssignLocationManager")
            .WithDescription("Assign a user as manager of a location.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<LocationManagerResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        locationsGroup.MapDelete("/{id:guid}/managers/{userId:guid}", RemoveAsync)
            .WithName("RemoveLocationManager")
            .WithDescription("Remove a manager from a location.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        locationsGroup.MapGet("/{id:guid}/managers", ListByLocationAsync)
            .WithName("ListLocationManagers")
            .WithDescription("List managers of a location.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<IReadOnlyList<LocationManagerResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        builder.MapGroup("/api/v1/managers")
            .WithTags("LocationManager")
            .MapGet("/me/locations", GetMyLocationsAsync)
            .WithName("GetMyManagedLocations")
            .WithDescription("List locations assigned to the calling manager.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<IReadOnlyList<LocationResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        return builder;
    }

    private static async Task<IResult> AssignAsync(
        [FromRoute] Guid id,
        [FromBody] AssignManagerDto dto,
        [FromServices] ILocationManagerService service,
        [FromServices] IValidator<AssignManagerDto> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (!dto.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<LocationManagerResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        return (await service.AssignAsync(id, dto, currentUser.UserId.Value, ct)).ToHttpResult();
    }

    private static async Task<IResult> RemoveAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid userId,
        [FromServices] ILocationManagerService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<object>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        return (await service.RemoveAsync(id, userId, ct)).ToHttpResult();
    }

    private static async Task<IResult> ListByLocationAsync(
        [FromRoute] Guid id,
        [FromServices] ILocationManagerService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<IReadOnlyList<LocationManagerResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        return (await service.ListByLocationAsync(id, ct)).ToHttpResult();
    }

    private static async Task<IResult> GetMyLocationsAsync(
        [FromServices] ILocationManagerService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<IReadOnlyList<LocationResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        return (await service.GetMyLocationsAsync(currentUser.UserId.Value, ct)).ToHttpResult();
    }
}
