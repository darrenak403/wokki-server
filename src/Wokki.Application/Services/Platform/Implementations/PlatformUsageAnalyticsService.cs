using Wokki.Application.Dtos.Platform;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.Platform.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Platform.Implementations;

public sealed class PlatformUsageAnalyticsService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope) : IPlatformUsageAnalyticsService
{
    public async Task<ApiResponse<PlatformUsageAnalyticsResponse>> GetAsync(
        PlatformUsageAnalyticsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PlatformUsageAnalyticsResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        var windowDays = request.WindowDays is >= 1 and <= 30 ? request.WindowDays.Value : 7;
        var to = DateTime.UtcNow;
        var from = to.AddDays(-windowDays);
        var weeklyFrom = to.AddDays(-30);

        var activeOrgCount = await unitOfWork.PlatformActivityEvents.CountActiveOrganizationsAsync(
            from,
            to,
            request.OrganizationId,
            cancellationToken);
        var activeOrganizations = await unitOfWork.PlatformActivityEvents.ListOrgActivityAsync(
            from,
            to,
            request.OrganizationId,
            take: 50,
            cancellationToken);
        var countsByEventType = await unitOfWork.PlatformActivityEvents.CountByEventTypeAsync(
            from,
            to,
            request.OrganizationId,
            cancellationToken);
        var weeklyActive = await unitOfWork.PlatformActivityEvents.CountWeeklyActiveOrganizationsAsync(
            weeklyFrom,
            to,
            request.OrganizationId,
            cancellationToken);
        var topOrganizations = await unitOfWork.PlatformActivityEvents.ListOrgActivityAsync(
            from,
            to,
            request.OrganizationId,
            take: 10,
            cancellationToken);

        return ApiResponse<PlatformUsageAnalyticsResponse>.SuccessResponse(
            new PlatformUsageAnalyticsResponse(
                windowDays,
                from,
                to,
                activeOrgCount,
                activeOrganizations.Select(ToOrgActivityResponse).ToList(),
                countsByEventType.Select(x => new PlatformUsageEventTypeCountResponse(x.EventType, x.Count)).ToList(),
                weeklyActive.Select(x => new PlatformUsageWeeklyActiveResponse(x.WeekStartDate, x.ActiveOrgCount)).ToList(),
                topOrganizations.Select(ToOrgActivityResponse).ToList()),
            AppMessages.Platform.UsageAnalyticsFound);
    }

    private static PlatformUsageOrgActivityResponse ToOrgActivityResponse(
        PlatformUsageOrgActivitySnapshot snapshot) =>
        new(
            snapshot.OrganizationId,
            snapshot.OrganizationName,
            snapshot.LastActivityAt,
            snapshot.ActivityCount);
}
