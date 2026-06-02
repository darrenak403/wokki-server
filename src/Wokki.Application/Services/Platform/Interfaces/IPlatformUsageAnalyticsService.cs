using Wokki.Application.Dtos.Platform;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Platform.Interfaces;

public interface IPlatformUsageAnalyticsService
{
    Task<ApiResponse<PlatformUsageAnalyticsResponse>> GetAsync(
        PlatformUsageAnalyticsRequest request,
        CancellationToken cancellationToken = default);
}
