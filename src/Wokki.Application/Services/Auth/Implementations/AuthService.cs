using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Auth;
using Wokki.Application.Services.Auth.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Auth.Implementations;

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

    public async Task<ApiResponse<UserSimpleResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await unitOfWork.Users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
            return ApiResponse<UserSimpleResponse>.FailureResponse(AppMessages.User.Exists);

        var user = new Domain.Entities.User
        {
            Email = normalizedEmail,
            PasswordHash = request.Password,
            Role = request.Role.Trim()
        };

        await unitOfWork.Users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<UserSimpleResponse>.SuccessResponse(
            new UserSimpleResponse(user.Id, user.Email, user.Role),
            AppMessages.User.Created);
    }

    public async Task<ApiResponse<UserSimpleResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return ApiResponse<UserSimpleResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        if (user.PasswordHash != request.CurrentPassword)
            return ApiResponse<UserSimpleResponse>.FailureResponse(AppMessages.Auth.InvalidCredentials);

        user.PasswordHash = request.NewPassword;
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<UserSimpleResponse>.SuccessResponse(
            new UserSimpleResponse(user.Id, user.Email, user.Role),
            AppMessages.User.Found);
    }

    public async Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        _ = await unitOfWork.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        return ApiResponse<bool>.SuccessResponse(true, AppMessages.Auth.LoginSuccess);
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.Unauthorized);

        user.PasswordHash = request.NewPassword;
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, AppMessages.Auth.RefreshSuccess);
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
