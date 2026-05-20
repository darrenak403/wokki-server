using Wokki.Application.Dtos.Auth;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Auth.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserSimpleResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserSimpleResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
}
