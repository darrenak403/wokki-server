using Wokki.Application.Dtos.Organization;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Organization.Interfaces;

public interface IOrganizationDirectoryService
{
    Task<ApiResponse<PagedResponse<OrganizationDirectoryItemResponse>>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);
}
