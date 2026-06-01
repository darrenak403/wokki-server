using Wokki.Application.Dtos.Auth;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Auth.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> RegisterEmployeeAsync(RegisterEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserSimpleResponse>> ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CompleteForgotPasswordAsync(CompleteForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserSimpleResponse>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
}
