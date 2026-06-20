using Wokki.Application.Dtos.Organization;
using Wokki.Application.Services.Organization.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Organization.Implementations;

public sealed class OrgUsageAnalyticsService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope) : IOrgUsageAnalyticsService
{
    public async Task<ApiResponse<OrgUsageAnalyticsResponse>> GetAsync(
        OrgUsageAnalyticsRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = organizationScope.RequireOrganizationId();

        var windowDays = request.WindowDays is >= 7 and <= 30 ? request.WindowDays.Value : 7;
        var to = DateTime.UtcNow;
        var from = to.AddDays(-windowDays);

        var countsByEventType = await unitOfWork.PlatformActivityEvents.CountByEventTypeAsync(
            from,
            to,
            organizationId,
            cancellationToken);
        var dailyCounts = await unitOfWork.PlatformActivityEvents.CountDailyByEventTypeAsync(
            from,
            to,
            organizationId,
            cancellationToken);

        var countsByEventTypeResponse = countsByEventType
            .Select(x => new OrgUsageAnalyticsEventTypeCountResponse(x.EventType, x.Count))
            .ToList();

        return ApiResponse<OrgUsageAnalyticsResponse>.SuccessResponse(
            new OrgUsageAnalyticsResponse(
                windowDays,
                from,
                to,
                countsByEventTypeResponse.Sum(x => x.Count),
                countsByEventTypeResponse,
                dailyCounts.Select(x => new OrgUsageAnalyticsDailyCountResponse(x.Date, x.EventType, x.Count)).ToList()),
            AppMessages.Stats.OrgUsageAnalyticsFound);
    }
}
