using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Auth;
using Wokki.Application.Services.Auth.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Auth.Implementations;

public sealed class AuthService(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher) : IAuthService
{
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return ApiResponse<LoginResponse>.FailureResponse(AppMessages.Auth.InvalidCredentials);

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = jwtTokenService.GenerateRefreshToken(user);

        return ApiResponse<LoginResponse>.SuccessResponse(
            new LoginResponse(accessToken, refreshToken),
            AppMessages.Auth.LoginSuccess);
    }

    public Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var userId = jwtTokenService.ValidateRefreshToken(request.RefreshToken);
        if (userId is null)
            return Task.FromResult(ApiResponse<LoginResponse>.FailureResponse(AppMessages.Auth.Unauthorized));

        return RefreshForUserAsync(userId.Value, cancellationToken);
    }

    public async Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await unitOfWork.Users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
            return ApiResponse<LoginResponse>.FailureResponse(AppMessages.User.Exists);

        var organization = new Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = request.OrganizationName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user = new Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = RoleConstants.Admin,
            OrganizationId = organization.Id,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.Organizations.AddAsync(organization, cancellationToken);
            await unitOfWork.Users.AddAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = jwtTokenService.GenerateRefreshToken(user);

        return ApiResponse<LoginResponse>.SuccessResponse(
            new LoginResponse(accessToken, refreshToken),
            AppMessages.User.Created);
    }

    public async Task<ApiResponse<UserSimpleResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return ApiResponse<UserSimpleResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return ApiResponse<UserSimpleResponse>.FailureResponse(AppMessages.Auth.InvalidCredentials);

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
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

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
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
            new LoginResponse(accessToken, refreshToken),
            AppMessages.Auth.RefreshSuccess);
    }

    public Task<ApiResponse<object>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _ = userId;
        _ = cancellationToken;
        return Task.FromResult(ApiResponse<object>.SuccessResponse(new { }, AppMessages.Auth.LogoutSuccess));
    }
}
