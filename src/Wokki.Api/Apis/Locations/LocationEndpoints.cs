using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Dtos.Location;
using Wokki.Application.Services.Location.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Locations;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/locations")
            .MapLocationRoutes()
            .WithTags("Locations")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapLocationRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListAsync)
            .WithName("ListLocations")
            .WithDescription("Danh sách địa điểm.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<IReadOnlyList<LocationResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapGet("/available", ListAvailableAsync)
            .WithName("ListAvailableLocations")
            .WithDescription("Danh sách chi nhánh đang hoạt động (dành cho người dùng đăng ký tham gia).")
            .RequireAuthorization()
            .Produces<ApiResponse<IReadOnlyList<LocationResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreateAsync)
            .WithName("CreateLocation")
            .WithDescription("Tạo địa điểm mới.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<LocationResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateLocation")
            .WithDescription("Cập nhật địa điểm.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<LocationResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}/scheduling-policy", GetSchedulingPolicyAsync)
            .WithName("GetLocationSchedulingPolicy")
            .WithDescription("Luật phân ca tổng của chi nhánh.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<LocationSchedulingPolicyResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/scheduling-policy", UpsertSchedulingPolicyAsync)
            .WithName("UpsertLocationSchedulingPolicy")
            .WithDescription("Cập nhật luật phân ca tổng của chi nhánh.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<LocationSchedulingPolicyResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] ILocationService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.ListAsync(cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ListAvailableAsync(
        [FromServices] ILocationService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.ListActiveAsync(cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateLocationRequest request,
        [FromServices] ILocationService service,
        [FromServices] IValidator<CreateLocationRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.CreateAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateLocationRequest request,
        [FromServices] ILocationService service,
        [FromServices] IValidator<UpdateLocationRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.UpdateAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetSchedulingPolicyAsync(
        [FromRoute] Guid id,
        [FromServices] ILocationService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.GetSchedulingPolicyAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> UpsertSchedulingPolicyAsync(
        [FromRoute] Guid id,
        [FromBody] UpsertLocationSchedulingPolicyRequest request,
        [FromServices] ILocationService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.UpsertSchedulingPolicyAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }
}
