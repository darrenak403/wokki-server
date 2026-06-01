using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Auth;
using Wokki.Application.Services.Auth.Interfaces;
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
        group.MapPost("/login", LoginUserAsync)
            .WithName("Login")
            .WithDescription("Đăng nhập người dùng (Không cần xác thực)")
            .AllowAnonymous()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status402PaymentRequired)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/register", RegisterUserAsync)
            .WithName("Register")
            .WithDescription("Tự đăng ký tổ chức: email + password + organizationName → Org Admin + JWT.")
            .AllowAnonymous()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/register-employee", RegisterEmployeeAsync)
            .WithName("RegisterEmployee")
            .WithDescription("Tự đăng ký nhân viên: email + password + họ tên → User không thuộc org + JWT.")
            .AllowAnonymous()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapPost("/refresh-token", RefreshTokenAsync)
            .WithName("RefreshToken")
            .WithDescription("Làm mới access token bằng refresh token (không cần access token còn hạn).")
            .AllowAnonymous()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status402PaymentRequired)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/reset-password", ResetPasswordAsync)
            .WithName("ResetPassword")
            .WithDescription("Đổi mật khẩu khi đã đăng nhập: mật khẩu cũ + mới + xác nhận.")
            .RequireAuthorization()
            .Produces<ApiResponse<UserSimpleResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .WithName("ForgotPassword")
            .WithDescription("Gửi mã OTP về email để đặt lại mật khẩu.")
            .AllowAnonymous()
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status429TooManyRequests);

        group.MapPost("/forgot-password/verify-otp", VerifyForgotPasswordOtpAsync)
            .WithName("VerifyForgotPasswordOtp")
            .WithDescription("Xác minh mã OTP sau bước forgot-password.")
            .AllowAnonymous()
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapPost("/forgot-password/complete", CompleteForgotPasswordAsync)
            .WithName("CompleteForgotPassword")
            .WithDescription("Đặt mật khẩu mới sau khi OTP đã được xác minh.")
            .AllowAnonymous()
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        group.MapPost("/logout", LogoutUserAsync)
            .WithName("Logout")
            .WithDescription("Đăng xuất người dùng (Cần xác thực)")
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithDescription("Thông tin người dùng đang đăng nhập.")
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> LoginUserAsync(
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

    private static async Task<IResult> RegisterUserAsync(
        [FromBody] RegisterRequest request,
        [FromServices] IAuthService authService,
        [FromServices] IValidator<RegisterRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await authService.RegisterAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> RegisterEmployeeAsync(
        [FromBody] RegisterEmployeeRequest request,
        [FromServices] IAuthService authService,
        [FromServices] IValidator<RegisterEmployeeRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await authService.RegisterEmployeeAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> RefreshTokenAsync(
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

    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        [FromServices] IAuthService authService,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IValidator<ResetPasswordRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        if (currentUser.UserId is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Auth.NotLoggedIn).ToHttpResult();

        var response = await authService.ResetPasswordAsync(currentUser.UserId.Value, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] IAuthService authService,
        [FromServices] IValidator<ForgotPasswordRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await authService.ForgotPasswordAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> VerifyForgotPasswordOtpAsync(
        [FromBody] VerifyForgotPasswordOtpRequest request,
        [FromServices] IAuthService authService,
        [FromServices] IValidator<VerifyForgotPasswordOtpRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await authService.VerifyForgotPasswordOtpAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CompleteForgotPasswordAsync(
        [FromBody] CompleteForgotPasswordRequest request,
        [FromServices] IAuthService authService,
        [FromServices] IValidator<CompleteForgotPasswordRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await authService.CompleteForgotPasswordAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> LogoutUserAsync(
        [FromServices] IAuthService authService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Auth.NotLoggedIn).ToHttpResult();

        var response = await authService.LogoutAsync(currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        [FromServices] IAuthService authService,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Auth.NotLoggedIn).ToHttpResult();

        var response = await authService.GetMeAsync(currentUser.UserId.Value, cancellationToken);
        return response.ToHttpResult();
    }
}
