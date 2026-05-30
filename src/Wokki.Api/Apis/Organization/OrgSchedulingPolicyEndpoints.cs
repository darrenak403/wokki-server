using FluentValidation;
using Wokki.Api.Extensions;
using Wokki.Application.Dtos.Scheduling;
using Wokki.Application.Services.OrganizationSchedulingPolicy.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.Organization;

public static class OrgSchedulingPolicyEndpoints
{
    public static RouteGroupBuilder MapOrgSchedulingPolicyRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/scheduling-policy", GetSchedulingPolicyAsync)
            .WithName("GetOrganizationSchedulingPolicy")
            .WithDescription("Luật xếp lịch cấp tổ chức (Admin + Manager read-only).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin, RoleConstants.Manager))
            .Produces<ApiResponse<OrganizationSchedulingPolicyResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPut("/scheduling-policy", UpsertSchedulingPolicyAsync)
            .WithName("UpsertOrganizationSchedulingPolicy")
            .WithDescription("Cập nhật luật xếp lịch cấp tổ chức (Org Admin only).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<OrganizationSchedulingPolicyResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        group.MapPost("/scheduling-policy/wizard-draft", BuildWizardDraftAsync)
            .WithName("BuildOrganizationSchedulingPolicyWizardDraft")
            .WithDescription("Sinh bản nháp luật xếp lịch từ quy mô org (Admin).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<SchedulingPolicyWizardDraftResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);

        return group;
    }

    private static async Task<IResult> GetSchedulingPolicyAsync(
        IOrganizationSchedulingPolicyService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPolicyAsync(cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpsertSchedulingPolicyAsync(
        UpsertOrganizationSchedulingPolicyRequest request,
        IOrganizationSchedulingPolicyService service,
        IValidator<UpsertOrganizationSchedulingPolicyRequest> validator,
        CancellationToken cancellationToken)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var result = await service.UpsertPolicyAsync(request, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> BuildWizardDraftAsync(
        SchedulingPolicyWizardRequest request,
        IOrganizationSchedulingPolicyService service,
        CancellationToken cancellationToken)
    {
        var result = await service.BuildWizardDraftAsync(request, cancellationToken);
        return result.ToHttpResult();
    }
}
