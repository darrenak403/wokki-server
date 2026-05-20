using Wokki.Application.Dtos.User;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.User.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<UserResponse>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<Guid>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
}
