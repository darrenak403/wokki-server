using Wokki.Application.Dtos.Organization;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Organization.Interfaces;

public interface IOrgUsageAnalyticsService
{
    Task<ApiResponse<OrgUsageAnalyticsResponse>> GetAsync(
        OrgUsageAnalyticsRequest request,
        CancellationToken cancellationToken = default);
}
