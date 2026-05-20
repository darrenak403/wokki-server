using Wokki.Application.Features.Users.Dtos;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Features.Users;

public sealed class UserService(IUnitOfWork unitOfWork) : IUserService
{
    public async Task<ApiResponse<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return ApiResponse<UserResponse>.FailureResponse(AppMessages.User.NotFound);

        return ApiResponse<UserResponse>.SuccessResponse(user.ToResponse(), AppMessages.User.Found);
    }

    public async Task<ApiResponse<PagedResponse<UserResponse>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, total) = await unitOfWork.Users.ListAsync(page, pageSize, cancellationToken);
        return ApiResponse<PagedResponse<UserResponse>>.SuccessPagedResponse(
            items.Select(u => u.ToResponse()),
            page,
            pageSize,
            total,
            AppMessages.User.Listed);
    }

    public async Task<ApiResponse<Guid>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return ApiResponse<Guid>.FailureResponse(AppMessages.User.Exists);

        var entity = request.ToEntity();
        await unitOfWork.Users.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.SuccessResponse(entity.Id, AppMessages.User.Created);
    }
}
