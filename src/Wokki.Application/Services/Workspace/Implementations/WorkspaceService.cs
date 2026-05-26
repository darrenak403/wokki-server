using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.LocationMembership;
using Wokki.Application.Dtos.Workspace;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Application.Services.Workspace.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using LocationMembershipEntity = Wokki.Domain.Entities.LocationMembership;
using EmployeeDeptMembership = Wokki.Domain.Entities.EmployeeDepartmentMembership;

namespace Wokki.Application.Services.Workspace.Implementations;

public sealed class WorkspaceService(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    ILocationScopeService locationScopeService,
    ILogger<WorkspaceService> logger) : IWorkspaceService
{
    public async Task<ApiResponse<object>> ChangeRoleAsync(
        Guid targetUserId,
        Guid callerId,
        ChangeRoleRequest request,
        CancellationToken ct = default)
    {
        var target = await unitOfWork.Users.GetByIdAsync(targetUserId, ct);
        if (target is null)
            return ApiResponse<object>.FailureResponse(AppMessages.User.NotFound);

        if (target.Role == RoleConstants.Admin)
            return ApiResponse<object>.FailureResponse(AppMessages.Workspace.CannotModifyAdmin);

        target.Role = request.Role;
        unitOfWork.Users.Update(target);
        await unitOfWork.SaveChangesAsync(ct);

        // Revoke refresh token so the user must re-authenticate to receive a JWT with the new role claim.
        jwtTokenService.RevokeRefreshToken(targetUserId);

        return ApiResponse<object>.SuccessResponse(new { targetUserId, newRole = request.Role }, AppMessages.Workspace.RoleChanged);
    }

    public async Task<ApiResponse<LocationMembershipResponse>> TransferLocationAsync(
        TransferLocationRequest request,
        Guid callerId,
        string callerRole,
        CancellationToken ct = default)
    {
        var canManage = await locationScopeService.CanManageEmployeeAsync(callerId, callerRole, request.EmployeeId, ct);
        if (!canManage)
            return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.Workspace.TransferForbidden);

        var employee = await unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken: ct);
        if (employee is null)
            return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.Employee.NotFound);

        if (employee.TerminatedAt is not null)
            return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.Employee.AlreadyTerminated);

        var location = await unitOfWork.Locations.GetByIdAsync(request.ToLocationId, cancellationToken: ct);
        if (location is null || !location.IsActive)
            return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.LocationMembership.LocationNotFound);

        var currentActive = await unitOfWork.LocationMemberships.GetActiveByEmployeeAsync(request.EmployeeId, track: true, cancellationToken: ct);
        if (currentActive is not null)
        {
            if (currentActive.LocationId == request.ToLocationId)
                return ApiResponse<LocationMembershipResponse>.FailureResponse(AppMessages.Workspace.AlreadyAtLocation);

            currentActive.Status = LocationMembershipStatus.Transferred;
            currentActive.ReviewedById = callerId;
            currentActive.ReviewedAt = DateTime.UtcNow;
            unitOfWork.LocationMemberships.Update(currentActive);
        }

        var newMembership = new LocationMembershipEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            LocationId = request.ToLocationId,
            Status = LocationMembershipStatus.Active,
            RequestedAt = DateTime.UtcNow,
            ReviewedById = callerId,
            ReviewedAt = DateTime.UtcNow,
        };
        await unitOfWork.LocationMemberships.AddAsync(newMembership, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await unitOfWork.LocationMemberships.GetByIdAsync(newMembership.Id, cancellationToken: ct);
        return ApiResponse<LocationMembershipResponse>.SuccessResponse(MapMembershipResponse(created!), AppMessages.Workspace.LocationTransferred);
    }

    public async Task<ApiResponse<object>> TransferDepartmentAsync(
        TransferDepartmentRequest request,
        Guid callerId,
        string callerRole,
        CancellationToken ct = default)
    {
        var canManage = await locationScopeService.CanManageEmployeeAsync(callerId, callerRole, request.EmployeeId, ct);
        if (!canManage)
            return ApiResponse<object>.FailureResponse(AppMessages.Workspace.TransferForbidden);

        var employee = await unitOfWork.Employees.GetByIdAsync(request.EmployeeId, track: true, cancellationToken: ct);
        if (employee is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Employee.NotFound);

        if (employee.TerminatedAt is not null)
            return ApiResponse<object>.FailureResponse(AppMessages.Employee.AlreadyTerminated);

        var department = await unitOfWork.Departments.GetByIdAsync(request.ToDepartmentId, cancellationToken: ct);
        if (department is null || !department.IsActive)
            return ApiResponse<object>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

        var currentActive = await unitOfWork.EmployeeDepartmentMemberships.GetActivePrimaryByEmployeeAsync(
            request.EmployeeId, track: true, cancellationToken: ct);

        if (currentActive is not null)
        {
            if (currentActive.DepartmentId == request.ToDepartmentId)
                return ApiResponse<object>.FailureResponse(AppMessages.Workspace.AlreadyInDepartment);

            currentActive.Status = DepartmentMembershipStatus.Transferred;
            currentActive.LeftAt = DateTime.UtcNow;
            currentActive.IsPrimary = false;
            unitOfWork.EmployeeDepartmentMemberships.Update(currentActive);
        }
        else
        {
            logger.LogWarning("Employee {EmployeeId} has no active primary department membership; creating new one directly.", request.EmployeeId);
        }

        var existing = await unitOfWork.EmployeeDepartmentMemberships.GetByEmployeeAndDepartmentAsync(
            request.EmployeeId, request.ToDepartmentId, track: true, cancellationToken: ct);

        if (existing is not null)
        {
            existing.Status = DepartmentMembershipStatus.Active;
            existing.JoinedAt = DateTime.UtcNow;
            existing.LeftAt = null;
            existing.IsPrimary = true;
            unitOfWork.EmployeeDepartmentMemberships.Update(existing);
        }
        else
        {
            var newMembership = new EmployeeDeptMembership
            {
                EmployeeId = request.EmployeeId,
                DepartmentId = request.ToDepartmentId,
                IsPrimary = true,
                Status = DepartmentMembershipStatus.Active,
                JoinedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            await unitOfWork.EmployeeDepartmentMemberships.AddAsync(newMembership, ct);
        }

        // Keep Employee.DepartmentId in sync so assignment guards and scope checks stay correct.
        employee.DepartmentId = request.ToDepartmentId;
        unitOfWork.Employees.Update(employee);

        await unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<object>.SuccessResponse(
            new { employeeId = request.EmployeeId, toDepartmentId = request.ToDepartmentId },
            AppMessages.Workspace.DepartmentTransferred);
    }

    private static LocationMembershipResponse MapMembershipResponse(LocationMembershipEntity m) => new(
        m.Id,
        m.LocationId,
        m.Location?.Name ?? string.Empty,
        m.EmployeeId,
        m.Employee?.FirstName ?? string.Empty,
        m.Employee?.LastName ?? string.Empty,
        m.Status,
        m.RequestedAt,
        m.ReviewedById,
        m.ReviewedAt,
        m.Note);
}
