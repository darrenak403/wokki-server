using Wokki.Application.Dtos.Scheduling;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.OrganizationSchedulingPolicy.Interfaces;

public interface IOrganizationSchedulingPolicyService
{
    Task<ApiResponse<SchedulingRuleCatalogResponse>> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task<ApiResponse<OrganizationSchedulingPolicyResponse>> GetPolicyAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<OrganizationSchedulingPolicyResponse>> UpsertPolicyAsync(
        UpsertOrganizationSchedulingPolicyRequest request,
        CancellationToken cancellationToken = default);
}
