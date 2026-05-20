using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Features.Auth;
using Wokki.Application.Features.Auth.Dtos;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;

namespace Wokki.Api.Apis.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/auth")
            .MapAuthRoutes()
            .WithTags("Auth")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapAuthRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithDescription("Đăng nhập bằng email và mật khẩu, trả về JWT.")
            .AllowAnonymous()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("Refresh")
            .WithDescription("Đổi access token bằng refresh token.")
            .AllowAnonymous()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithDescription("Đăng xuất.")
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithDescription("Thông tin người dùng đang đăng nhập.")
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] IAuthService authService,
        [FromServices] IValidator<LoginRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await authService.LoginAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        [FromServices] IAuthService authService,
        [FromServices] IValidator<RefreshTokenRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await authService.RefreshAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> LogoutAsync(
        [FromServices] IAuthService authService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Auth.NotLoggedIn).ToHttpResult();

        var response = await authService.LogoutAsync(currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static Task<IResult> GetCurrentUserAsync([FromServices] ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated)
            return Task.FromResult(ApiResponse<object>.FailureResponse(AppMessages.Auth.NotLoggedIn).ToHttpResult());

        return Task.FromResult(
            ApiResponse<object>.SuccessResponse(
                new { id = currentUser.UserId, email = currentUser.Email, role = currentUser.Role },
                AppMessages.Auth.Me).ToHttpResult());
    }
}
