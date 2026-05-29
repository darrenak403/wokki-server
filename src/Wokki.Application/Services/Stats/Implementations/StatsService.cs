using Wokki.Application.Dtos.Organization;
using Wokki.Application.Dtos.Platform;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.Stats.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Stats.Implementations;

public sealed class StatsService(IUnitOfWork unitOfWork, IOrganizationScopeService organizationScope) : IStatsService
{
    public async Task<ApiResponse<PlatformStatsResponse>> GetPlatformStatsAsync(CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PlatformStatsResponse>.FailureResponse(AppMessages.Stats.Forbidden);

        var stats = await unitOfWork.Organizations.GetPlatformStatsAsync(cancellationToken);
        return ApiResponse<PlatformStatsResponse>.SuccessResponse(
            new PlatformStatsResponse(stats.OrganizationCount, stats.UserCount, stats.LocationCount, stats.EmployeeCount),
            AppMessages.Stats.PlatformFound);
    }

    public async Task<ApiResponse<OrgStatsResponse>> GetOrgStatsAsync(CancellationToken cancellationToken = default)
    {
        if (organizationScope.IsPlatformOperator)
            return ApiResponse<OrgStatsResponse>.FailureResponse(AppMessages.Stats.Forbidden);

        var organizationId = organizationScope.RequireOrganizationId();
        var stats = await unitOfWork.Organizations.GetOrgStatsAsync(organizationId, cancellationToken);
        return ApiResponse<OrgStatsResponse>.SuccessResponse(
            new OrgStatsResponse(
                organizationId,
                stats.UserCount,
                stats.LocationCount,
                stats.DepartmentCount,
                stats.EmployeeCount,
                stats.ActiveLocationMembershipCount),
            AppMessages.Stats.OrgFound);
    }
}
