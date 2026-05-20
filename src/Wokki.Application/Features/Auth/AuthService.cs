using Wokki.Application.Common.Interfaces;
using Wokki.Application.Features.Auth.Dtos;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Features.Auth;

public sealed class AuthService(IUnitOfWork unitOfWork, IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || user.PasswordHash != request.Password)
            return ApiResponse<LoginResponse>.FailureResponse(AppMessages.Auth.InvalidCredentials);

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = jwtTokenService.GenerateRefreshToken(user);

        return ApiResponse<LoginResponse>.SuccessResponse(
            new LoginResponse(accessToken, refreshToken, user.Id, user.Email, user.Role),
            AppMessages.Auth.LoginSuccess);
    }

    public Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var userId = jwtTokenService.ValidateRefreshToken(request.RefreshToken);
        if (userId is null)
            return Task.FromResult(ApiResponse<LoginResponse>.FailureResponse(AppMessages.Auth.Unauthorized));

        return RefreshForUserAsync(userId.Value, cancellationToken);
    }

    private async Task<ApiResponse<LoginResponse>> RefreshForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return ApiResponse<LoginResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = jwtTokenService.GenerateRefreshToken(user);

        return ApiResponse<LoginResponse>.SuccessResponse(
            new LoginResponse(accessToken, refreshToken, user.Id, user.Email, user.Role),
            AppMessages.Auth.RefreshSuccess);
    }

    public Task<ApiResponse<object>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _ = userId;
        _ = cancellationToken;
        return Task.FromResult(ApiResponse<object>.SuccessResponse(new { }, AppMessages.Auth.LogoutSuccess));
    }
}
