using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.Dtos.OrgJoinRequest;
using Wokki.Application.Services.OrgJoinRequest.Interfaces;
using Wokki.Common.Extensions;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;

namespace Wokki.Api.Apis.OrgJoinRequest;

public static class OrgJoinRequestEndpoints
{
    public static IEndpointRouteBuilder MapOrgJoinRequestApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/v1/org-join-requests")
            .MapOrgJoinRequestRoutes()
            .WithTags("OrgJoinRequests")
            .RequireRateLimiting(RateLimitPolicies.Fixed);

        return builder;
    }

    public static RouteGroupBuilder MapOrgJoinRequestRoutes(this RouteGroupBuilder group)
    {
        group.MapPost("/", SubmitAsync)
            .WithName("SubmitOrgJoinRequest")
            .WithDescription("Gửi yêu cầu tham gia tổ chức (user chưa thuộc org).")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<OrgJoinRequestResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict);

        group.MapGet("/me", GetMyAsync)
            .WithName("GetMyOrgJoinRequest")
            .WithDescription("Trạng thái yêu cầu tham gia của user hiện tại.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<OrgJoinRequestResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        group.MapDelete("/me", CancelMyAsync)
            .WithName("CancelMyOrgJoinRequest")
            .WithDescription("Hủy yêu cầu đang chờ duyệt.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.User))
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        group.MapGet("/pending", ListPendingAsync)
            .WithName("ListPendingOrgJoinRequests")
            .WithDescription("Danh sách yêu cầu chờ duyệt — Org Admin.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<IReadOnlyList<PendingOrgJoinRequestResponse>>>(StatusCodes.Status200OK);

        group.MapPatch("/{id:guid}/approve", ApproveAsync)
            .WithName("ApproveOrgJoinRequest")
            .WithDescription("Duyệt yêu cầu — gán phòng ban và tạo Employee.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<OrgJoinRequestResponse>>(StatusCodes.Status200OK);

        group.MapPatch("/{id:guid}/reject", RejectAsync)
            .WithName("RejectOrgJoinRequest")
            .WithDescription("Từ chối yêu cầu tham gia.")
            .RequireAuthorization(p => p.RequireRole(RoleConstants.Admin))
            .Produces<ApiResponse<OrgJoinRequestResponse>>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> SubmitAsync(
        [FromBody] SubmitOrgJoinRequest request,
        [FromServices] IOrgJoinRequestService service,
        [FromServices] IValidator<SubmitOrgJoinRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.SubmitAsync(request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> GetMyAsync(
        [FromServices] IOrgJoinRequestService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.GetMyAsync(cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> CancelMyAsync(
        [FromServices] IOrgJoinRequestService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.CancelMyAsync(cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ListPendingAsync(
        [FromServices] IOrgJoinRequestService service,
        CancellationToken cancellationToken = default)
    {
        var response = await service.ListPendingAsync(cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> ApproveAsync(
        [FromRoute] Guid id,
        [FromBody] ApproveOrgJoinRequest request,
        [FromServices] IOrgJoinRequestService service,
        [FromServices] IValidator<ApproveOrgJoinRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.ApproveAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }

    private static async Task<IResult> RejectAsync(
        [FromRoute] Guid id,
        [FromBody] RejectOrgJoinRequest request,
        [FromServices] IOrgJoinRequestService service,
        [FromServices] IValidator<RejectOrgJoinRequest> validator,
        CancellationToken cancellationToken = default)
    {
        if (!request.ValidateRequest(validator, out var validationResult))
            return validationResult!;

        var response = await service.RejectAsync(id, request, cancellationToken);
        return response.ToHttpResult();
    }
}
