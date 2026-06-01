using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.OrgJoinRequest;
using Wokki.Application.Mappings.OrgJoinRequest;
using Wokki.Application.Services.Employee;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Application.Services.OrgJoinRequest.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using OrgJoinRequestEntity = Wokki.Domain.Entities.OrgJoinRequest;

namespace Wokki.Application.Services.OrgJoinRequest.Implementations;

public sealed class OrgJoinRequestService(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IOrganizationScopeService organizationScope,
    IEmployeeProvisioner employeeProvisioner) : IOrgJoinRequestService
{
    public async Task<ApiResponse<OrgJoinRequestResponse>> SubmitAsync(
        SubmitOrgJoinRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        if (!OrgLessUserAccess.IsOrgLessUser(currentUser.Role, currentUser.OrganizationId))
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.Forbidden);

        var existingEmployee = await unitOfWork.Employees.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken);
        if (existingEmployee is not null)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.AlreadyMember);

        var pending = await unitOfWork.OrgJoinRequests.GetPendingByUserIdAsync(currentUser.UserId.Value, cancellationToken: cancellationToken);
        if (pending is not null)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.PendingExists);

        if (!await unitOfWork.Organizations.HasActivePackageAsync(request.OrganizationId, cancellationToken))
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.OrgNotAvailable);

        var joinRequest = new OrgJoinRequestEntity
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.UserId.Value,
            OrganizationId = request.OrganizationId,
            Status = OrgJoinRequestStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        await unitOfWork.OrgJoinRequests.AddAsync(joinRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var loaded = await unitOfWork.OrgJoinRequests.GetByIdAsync(joinRequest.Id, cancellationToken: cancellationToken);
        return ApiResponse<OrgJoinRequestResponse>.SuccessResponse(
            loaded!.ToResponse(),
            AppMessages.OrgJoin.Submitted);
    }

    public async Task<ApiResponse<OrgJoinRequestResponse>> GetMyAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        if (!OrgLessUserAccess.IsOrgLessUser(currentUser.Role, currentUser.OrganizationId))
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.Forbidden);

        var request = await unitOfWork.OrgJoinRequests.GetLatestByUserIdAsync(currentUser.UserId.Value, cancellationToken);
        if (request is null)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.NotFound);

        await ExpireIfNeededAsync(request, cancellationToken);

        if (request.Status == OrgJoinRequestStatus.Pending)
        {
            var refreshed = await unitOfWork.OrgJoinRequests.GetByIdAsync(request.Id, cancellationToken: cancellationToken);
            return ApiResponse<OrgJoinRequestResponse>.SuccessResponse(refreshed!.ToResponse(), AppMessages.OrgJoin.Found);
        }

        return ApiResponse<OrgJoinRequestResponse>.SuccessResponse(request.ToResponse(), AppMessages.OrgJoin.Found);
    }

    public async Task<ApiResponse<object>> CancelMyAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
            return ApiResponse<object>.FailureResponse(AppMessages.Auth.Unauthorized);

        if (!OrgLessUserAccess.IsOrgLessUser(currentUser.Role, currentUser.OrganizationId))
            return ApiResponse<object>.FailureResponse(AppMessages.OrgJoin.Forbidden);

        var request = await unitOfWork.OrgJoinRequests.GetPendingByUserIdAsync(
            currentUser.UserId.Value,
            track: true,
            cancellationToken: cancellationToken);
        if (request is null)
            return ApiResponse<object>.FailureResponse(AppMessages.OrgJoin.NotFound);

        request.Status = OrgJoinRequestStatus.Cancelled;
        request.ReviewedAt = DateTime.UtcNow;
        unitOfWork.OrgJoinRequests.Update(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.OrgJoin.Cancelled);
    }

    public async Task<ApiResponse<IReadOnlyList<PendingOrgJoinRequestResponse>>> ListPendingAsync(
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(currentUser.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            return ApiResponse<IReadOnlyList<PendingOrgJoinRequestResponse>>.FailureResponse(AppMessages.Auth.Forbidden);

        var organizationId = organizationScope.RequireOrganizationId();
        var pending = await unitOfWork.OrgJoinRequests.ListPendingByOrganizationAsync(organizationId, cancellationToken);

        foreach (var item in pending)
            await ExpireIfNeededAsync(item, cancellationToken);

        var refreshed = await unitOfWork.OrgJoinRequests.ListPendingByOrganizationAsync(organizationId, cancellationToken);
        var responses = refreshed.Select(x => x.ToPendingResponse()).ToList();

        return ApiResponse<IReadOnlyList<PendingOrgJoinRequestResponse>>.SuccessResponse(
            responses,
            AppMessages.OrgJoin.Listed);
    }

    public async Task<ApiResponse<OrgJoinRequestResponse>> ApproveAsync(
        Guid id,
        ApproveOrgJoinRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        if (!string.Equals(currentUser.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        var organizationId = organizationScope.RequireOrganizationId();
        var joinRequest = await unitOfWork.OrgJoinRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (joinRequest is null || joinRequest.OrganizationId != organizationId)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.NotFound);

        if (joinRequest.Status != OrgJoinRequestStatus.Pending)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.NotPending);

        if (!await unitOfWork.Organizations.HasActivePackageAsync(organizationId, cancellationToken))
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.Organization.PackageNotActivated);

        var user = await unitOfWork.Users.GetByIdAsync(joinRequest.UserId, track: true, cancellationToken: cancellationToken);
        if (user is null)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.User.NotFound);

        if (user.OrganizationId.HasValue && user.OrganizationId != organizationId)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.AlreadyMember);

        var existingEmployee = await unitOfWork.Employees.GetByUserIdAsync(user.Id, cancellationToken);
        if (existingEmployee is not null)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.AlreadyMember);

        var department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId, cancellationToken: cancellationToken);
        if (department is null || !department.IsActive || department.OrganizationId != organizationId)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            user.OrganizationId = organizationId;
            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = request.Phone.Trim();

            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await employeeProvisioner.ProvisionUserEmployeeAsync(
                new ProvisionEmployeeCommand(
                    user.Id,
                    organizationId,
                    request.DepartmentId,
                    user.FirstName,
                    user.LastName,
                    request.Phone ?? user.Phone,
                    request.HourlyRate),
                cancellationToken);

            joinRequest.Status = OrgJoinRequestStatus.Approved;
            joinRequest.ReviewedAt = DateTime.UtcNow;
            joinRequest.ReviewedByUserId = currentUser.UserId.Value;
            unitOfWork.OrgJoinRequests.Update(joinRequest);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var loaded = await unitOfWork.OrgJoinRequests.GetByIdAsync(id, cancellationToken: cancellationToken);
        return ApiResponse<OrgJoinRequestResponse>.SuccessResponse(loaded!.ToResponse(), AppMessages.OrgJoin.Approved);
    }

    public async Task<ApiResponse<OrgJoinRequestResponse>> RejectAsync(
        Guid id,
        RejectOrgJoinRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        if (!string.Equals(currentUser.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        var organizationId = organizationScope.RequireOrganizationId();
        var joinRequest = await unitOfWork.OrgJoinRequests.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (joinRequest is null || joinRequest.OrganizationId != organizationId)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.NotFound);

        if (joinRequest.Status != OrgJoinRequestStatus.Pending)
            return ApiResponse<OrgJoinRequestResponse>.FailureResponse(AppMessages.OrgJoin.NotPending);

        joinRequest.Status = OrgJoinRequestStatus.Rejected;
        joinRequest.ReviewedAt = DateTime.UtcNow;
        joinRequest.ReviewedByUserId = currentUser.UserId.Value;
        joinRequest.RejectNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        unitOfWork.OrgJoinRequests.Update(joinRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var loaded = await unitOfWork.OrgJoinRequests.GetByIdAsync(id, cancellationToken: cancellationToken);
        return ApiResponse<OrgJoinRequestResponse>.SuccessResponse(loaded!.ToResponse(), AppMessages.OrgJoin.Rejected);
    }

    private async Task ExpireIfNeededAsync(OrgJoinRequestEntity request, CancellationToken cancellationToken)
    {
        if (request.Status != OrgJoinRequestStatus.Pending)
            return;

        if (await unitOfWork.Organizations.HasActivePackageAsync(request.OrganizationId, cancellationToken))
            return;

        var tracked = await unitOfWork.OrgJoinRequests.GetByIdAsync(request.Id, track: true, cancellationToken: cancellationToken);
        if (tracked is null || tracked.Status != OrgJoinRequestStatus.Pending)
            return;

        tracked.Status = OrgJoinRequestStatus.Expired;
        tracked.ReviewedAt = DateTime.UtcNow;
        unitOfWork.OrgJoinRequests.Update(tracked);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        request.Status = OrgJoinRequestStatus.Expired;
        request.ReviewedAt = tracked.ReviewedAt;
    }
}
