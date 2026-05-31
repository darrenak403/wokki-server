using Wokki.Application.Dtos.Scheduling;
using Wokki.Application.Scheduling;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;
using OrgSchedulingPolicyEntity = Wokki.Domain.Entities.OrganizationSchedulingPolicy;

namespace Wokki.Application.Services.OrganizationSchedulingPolicy.Implementations;

public sealed class OrganizationSchedulingPolicyService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope,
    OrganizationSchedulingPolicyFeasibilityValidator feasibilityValidator) : Interfaces.IOrganizationSchedulingPolicyService
{
    public Task<ApiResponse<SchedulingRuleCatalogResponse>> GetCatalogAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ApiResponse<SchedulingRuleCatalogResponse>.SuccessResponse(
            SchedulingRuleCatalog.ToResponse(),
            AppMessages.Organization.SchedulingCatalogFound));

    public async Task<ApiResponse<OrganizationSchedulingPolicyResponse>> GetPolicyAsync(
        CancellationToken cancellationToken = default)
    {
        var organizationId = organizationScope.RequireOrganizationId();
        var policy = await unitOfWork.OrganizationSchedulingPolicies.GetByOrganizationIdAsync(
            organizationId,
            cancellationToken: cancellationToken);

        return ApiResponse<OrganizationSchedulingPolicyResponse>.SuccessResponse(
            ToResponse(organizationId, policy),
            AppMessages.Organization.SchedulingPolicyFound);
    }

    public async Task<ApiResponse<OrganizationSchedulingPolicyResponse>> UpsertPolicyAsync(
        UpsertOrganizationSchedulingPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = organizationScope.RequireOrganizationId();

        if (!OrganizationSchedulingPolicyRules.TryValidate(request.Rules))
            return ApiResponse<OrganizationSchedulingPolicyResponse>.FailureResponse(
                AppMessages.Organization.SchedulingPolicyInvalid);

        var normalizedRules = OrganizationSchedulingPolicyRules.NormalizeUpsert(request.Rules);
        var feasibilityErrors = await feasibilityValidator.ValidateAsync(
            organizationId,
            normalizedRules,
            cancellationToken);
        if (feasibilityErrors.Count > 0)
        {
            var errors = feasibilityErrors
                .Select(message => new ErrorDetail("rules", message))
                .ToList();
            return ApiResponse<OrganizationSchedulingPolicyResponse>.FailureResponse(
                AppMessages.Organization.SchedulingPolicyInfeasible,
                errors);
        }

        var policy = await unitOfWork.OrganizationSchedulingPolicies.GetByOrganizationIdAsync(
            organizationId,
            track: true,
            cancellationToken);
        if (policy is null)
        {
            policy = new OrgSchedulingPolicyEntity { OrganizationId = organizationId };
            ApplyPolicy(policy, request);
            await unitOfWork.OrganizationSchedulingPolicies.AddAsync(policy, cancellationToken);
        }
        else
        {
            ApplyPolicy(policy, request);
            unitOfWork.OrganizationSchedulingPolicies.Update(policy);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<OrganizationSchedulingPolicyResponse>.SuccessResponse(
            ToResponse(organizationId, policy),
            AppMessages.Organization.SchedulingPolicyUpdated);
    }

    public Task<ApiResponse<SchedulingPolicyWizardDraftResponse>> BuildWizardDraftAsync(
        SchedulingPolicyWizardRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = organizationScope.RequireOrganizationId();
        if (request.AverageEmployees < 1 || request.ShiftsPerDay < 1)
        {
            return Task.FromResult(
                ApiResponse<SchedulingPolicyWizardDraftResponse>.FailureResponse(
                    AppMessages.Organization.SchedulingPolicyInvalid));
        }

        var draft = OrganizationSchedulingPolicyWizard.BuildDraft(request);
        return Task.FromResult(
            ApiResponse<SchedulingPolicyWizardDraftResponse>.SuccessResponse(
                draft,
                AppMessages.Organization.SchedulingPolicyWizardDraftCreated));
    }

    private static void ApplyPolicy(OrgSchedulingPolicyEntity policy, UpsertOrganizationSchedulingPolicyRequest request)
    {
        policy.RulesJson = OrganizationSchedulingPolicyRules.SerializeUpsert(request.Rules);
        policy.SchemaVersion = OrganizationSchedulingPolicyRules.SchemaVersion;
        policy.UpdatedAt = DateTime.UtcNow;
    }

    private static OrganizationSchedulingPolicyResponse ToResponse(
        Guid organizationId,
        OrgSchedulingPolicyEntity? policy) =>
        new(
            organizationId,
            OrganizationSchedulingPolicyRules.SchemaVersion,
            OrganizationSchedulingPolicyRules.GetEffectiveRules(policy),
            policy?.UpdatedAt ?? DateTime.UtcNow);
}
