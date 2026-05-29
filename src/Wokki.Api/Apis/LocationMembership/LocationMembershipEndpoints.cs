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
            .WithDescription("Get the current user's active location membership (provisioned when Org Admin creates the employee).")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Fixed)
            .Produces<ApiResponse<LocationMembershipResponse?>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

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

    private static async Task<IResult> GetMyStatusAsync(
        [FromServices] ILocationMembershipService service,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Results.Json(ApiResponse<LocationMembershipResponse?>.FailureResponse(AppMessages.Auth.Unauthorized), statusCode: 401);

        return (await service.GetMyStatusAsync(currentUser.UserId.Value, ct)).ToHttpResult();
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
