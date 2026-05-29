using Wokki.Application.Dtos.Organization;
using Wokki.Application.Dtos.Platform;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Stats.Interfaces;

public interface IStatsService
{
    Task<ApiResponse<PlatformStatsResponse>> GetPlatformStatsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<OrgStatsResponse>> GetOrgStatsAsync(CancellationToken cancellationToken = default);
}
