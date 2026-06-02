using Wokki.Application.Dtos.Platform;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Platform.Interfaces;

public interface IPlatformAdminService
{
    Task<ApiResponse<PagedResponse<PlatformUserResponse>>> ListUsersAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        string? role = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<PlatformOrganizationResponse>>> ListOrganizationsAsync(
        PlatformOrganizationListRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<PlatformSubscriptionLedgerEntryResponse>>> ListSubscriptionLedgerAsync(
        PlatformSubscriptionLedgerListRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<PlatformSupportSearchResponse>>> SearchSupportAsync(
        PlatformSupportSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PlatformOrganizationSupportContextResponse>> GetSupportOrganizationContextAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PlatformOrganizationResponse>> UpdateOrganizationSubscriptionAsync(
        Guid organizationId,
        UpdateOrganizationSubscriptionRequest request,
        CancellationToken cancellationToken = default);
}
