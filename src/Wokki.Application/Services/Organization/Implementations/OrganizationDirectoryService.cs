using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Organization;
using Wokki.Application.Services.Organization.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Organization.Implementations;

public sealed class OrganizationDirectoryService(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IOrganizationDirectoryService
{
    public async Task<ApiResponse<PagedResponse<OrganizationDirectoryItemResponse>>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (!OrgLessUserAccess.IsOrgLessUser(currentUser.Role, currentUser.OrganizationId))
            return ApiResponse<PagedResponse<OrganizationDirectoryItemResponse>>.FailureResponse(AppMessages.OrgJoin.Forbidden);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (items, total) = await unitOfWork.Organizations.ListDirectoryAsync(page, pageSize, search, cancellationToken);
        var responses = items.Select(i => new OrganizationDirectoryItemResponse(i.Id, i.Name)).ToList();

        return ApiResponse<PagedResponse<OrganizationDirectoryItemResponse>>.SuccessPagedResponse(
            responses,
            page,
            pageSize,
            total,
            AppMessages.OrgJoin.DirectoryListed);
    }
}
