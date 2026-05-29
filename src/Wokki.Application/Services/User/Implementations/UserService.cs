using Wokki.Application.Dtos.User;
using Wokki.Application.Mappings.Users;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.User.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.User.Implementations;

public sealed class UserService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope) : IUserService
{
    public async Task<ApiResponse<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null || user.OrganizationId is null || !organizationScope.IsSameOrganization(user.OrganizationId.Value))
            return ApiResponse<UserResponse>.FailureResponse(AppMessages.User.NotFound);

        return ApiResponse<UserResponse>.SuccessResponse(user.ToResponse(), AppMessages.User.Found);
    }

    public async Task<ApiResponse<PagedResponse<UserResponse>>> ListAsync(int page, int pageSize, bool withoutEmployee = false, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var organizationId = organizationScope.GetCurrentOrganizationId();
        var (items, total) = withoutEmployee
            ? await unitOfWork.Users.ListWithoutEmployeeAsync(page, pageSize, organizationId, cancellationToken)
            : await unitOfWork.Users.ListAsync(page, pageSize, organizationId, cancellationToken);

        return ApiResponse<PagedResponse<UserResponse>>.SuccessPagedResponse(
            items.Select(u => u.ToResponse()),
            page,
            pageSize,
            total,
            AppMessages.User.Listed);
    }

    public async Task<ApiResponse<Guid>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _ = request;
        _ = cancellationToken;
        return await Task.FromResult(ApiResponse<Guid>.FailureResponse(AppMessages.User.EmployeeProfileRequired));
    }
}
