using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.LocationMembership;
using Wokki.Application.Services.LocationMembership.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.LocationMembership;

public static class LocationMembershipEndpoints
{
    public static IEndpointRouteBuilder MapLocationMembershipApi(this IEndpointRouteBuilder builder)
    {
        var membershipGroup = builder.MapGroup("/api/v1/location-memberships")
            .WithTags("LocationMembership");

        membershipGroup.MapGet("/my", GetMyStatusAsync)
            .WithName("GetMyLocationMembership")
            .WithDescription("Get the current user's active or pending location membership.")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<LocationMembershipResponse?>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        membershipGroup.MapPost("/request", RequestAsync)
            .WithName("RequestLocationMembership")
            .WithDescription("Submit a join request for a location.")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<LocationMembershipResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        membershipGroup.MapGet("/pending", ListAllPendingAsync)
            .WithName("ListAllPendingMemberships")
            .WithDescription("List all pending membership requests. Admin sees all locations; Manager sees only managed locations.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<IReadOnlyList<LocationMembershipResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        membershipGroup.MapPatch("/{id:guid}/review", ReviewAsync)
            .WithName("ReviewLocationMembership")
            .WithDescription("Approve or reject a membership request (Admin or Manager of target location).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<LocationMembershipResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        // FR-09: list memberships mounted on /locations/{id}/memberships
        builder.MapGroup("/api/v1/locations")
            .WithTags("LocationMembership")
            .MapGet("/{id:guid}/memberships", ListByLocationAsync)
            .WithName("ListLocationMemberships")
            .WithDescription("List memberships for a location filtered by status (Admin or Manager of that location).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<IReadOnlyList<LocationMembershipResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        return builder;
    }

    private static async Task<IResult> ListAllPendingAsync(
        [FromServices] ILocationMembershipService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<IReadOnlyList<LocationMembershipResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        return (await service.ListAllPendingAsync(currentUser.UserId.Value, isAdmin, ct)).ToHttpResult();
    }

    private static async Task<IResult> GetMyStatusAsync(
        [FromServices] ILocationMembershipService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<LocationMembershipResponse?>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        return (await service.GetMyStatusAsync(currentUser.UserId.Value, ct)).ToHttpResult();
    }

    private static async Task<IResult> RequestAsync(
        [FromBody] LocationMembershipRequestDto dto,
        [FromServices] ILocationMembershipService service,
        [FromServices] IValidator<LocationMembershipRequestDto> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!dto.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        return (await service.RequestAsync(currentUser.UserId.Value, dto, ct)).ToHttpResult();
    }

    private static async Task<IResult> ReviewAsync(
        [FromRoute] Guid id,
        [FromBody] LocationMembershipReviewDto dto,
        [FromServices] ILocationMembershipService service,
        [FromServices] IValidator<LocationMembershipReviewDto> validator,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        if (!dto.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        return (await service.ReviewAsync(id, currentUser.UserId.Value, isAdmin, dto, ct)).ToHttpResult();
    }

    private static async Task<IResult> ListByLocationAsync(
        [FromRoute] Guid id,
        [FromServices] ILocationMembershipService service,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] string? status,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<IReadOnlyList<LocationMembershipResponse>>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        var isAdmin = currentUser.Role == RoleConstants.Admin;
        return (await service.ListByLocationAsync(id, status, currentUser.UserId.Value, isAdmin, ct)).ToHttpResult();
    }
}
