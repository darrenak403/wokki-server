using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Common;
using Wokki.Api.Extensions;
using Wokki.Application.Dtos.User;
using Wokki.Application.Services.User.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/users")
            .MapUserRoutes()
            .WithTags("Users")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapUserRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetUserById")
            .WithDescription("Lấy thông tin user theo id.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<UserResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapGet("/", GetPagedAsync)
            .WithName("ListUsers")
            .WithDescription("Danh sách user có phân trang.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<PagedResponse<UserResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateAsync)
            .WithName("CreateUser")
            .WithDescription("Tạo user mới.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        [FromServices] IUserService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetPagedAsync(
        [AsParameters] PaginationRequest pagination,
        [FromQuery] bool? withoutEmployee,
        [FromServices] IUserService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.ListAsync(pagination.Page, pagination.PageSize, withoutEmployee == true, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateUserRequest request,
        [FromServices] IUserService service,
        [FromServices] IValidator<CreateUserRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.CreateAsync(request, cancellationToken);
        return response.ToHttpResult();
    }
}
