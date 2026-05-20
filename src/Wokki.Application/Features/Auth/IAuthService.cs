using Wokki.Application.Features.Auth.Dtos;
using Wokki.Common.Utils;

namespace Wokki.Application.Features.Auth;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
}
