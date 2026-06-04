using System.Text.Json;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Employee;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using UserEntity = Wokki.Domain.Entities.User;

namespace Wokki.Application.Services.Employee.Implementations;

public sealed class EmployeeRoleTransitionService(
    IUnitOfWork unitOfWork,
    IOrganizationScopeService organizationScope,
    IStaffPlacementCoordinator placement,
    IJwtTokenService jwtTokenService,
    ICurrentUserService currentUser,
    IEmployeeService employeeService) : IEmployeeRoleTransitionService
{
    private const string RoleTransitionAuditAction = "employee.role_transition";
    private const string EmployeeEntityType = "Employee";

    public async Task<ApiResponse<EmployeeResponse>> TransitionAsync(
        Guid employeeId,
        EmployeeRoleTransitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, track: true, cancellationToken: cancellationToken);
        if (employee is null || employee.TerminatedAt is not null || !organizationScope.IsSameOrganization(employee.OrganizationId))
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.NotFound);

        var user = await unitOfWork.Users.GetByIdAsync(employee.UserId, track: true, cancellationToken: cancellationToken);
        if (user is null || user.OrganizationId is null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.NotFound);

        if (string.Equals(user.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.OrgAdminNoDepartment);

        if (!currentUser.UserId.HasValue)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        var organizationId = employee.OrganizationId;
        var actorUserId = currentUser.UserId.Value;
        var fromRole = user.Role;

        if (request.TargetRole == RoleConstants.Manager)
            return await PromoteToManagerAsync(employee, user, organizationId, actorUserId, fromRole, request, cancellationToken);

        if (request.TargetRole == RoleConstants.User)
            return await DemoteToUserAsync(employee, user, organizationId, actorUserId, fromRole, request, cancellationToken);

        return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.InvalidRoleTransition);
    }

    private async Task<ApiResponse<EmployeeResponse>> PromoteToManagerAsync(
        EmployeeEntity employee,
        UserEntity user,
        Guid organizationId,
        Guid actorUserId,
        string fromRole,
        EmployeeRoleTransitionRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(fromRole, RoleConstants.User, StringComparison.OrdinalIgnoreCase))
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.InvalidRoleTransition);

        var locationId = request.LocationId;
        if (!locationId.HasValue || locationId.Value == Guid.Empty)
        {
            if (!employee.DepartmentId.HasValue)
                return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentOrLocationRequired);

            var currentDept = await unitOfWork.Departments.GetByIdAsync(employee.DepartmentId.Value, cancellationToken: cancellationToken);
            if (currentDept is null || !currentDept.IsActive || currentDept.OrganizationId != organizationId)
                return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

            locationId = currentDept.LocationId;
        }

        var location = await unitOfWork.Locations.GetByIdAsync(locationId.Value, cancellationToken: cancellationToken);
        if (location is null || !location.IsActive || location.OrganizationId != organizationId)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.ManagerLocationNotFound);

        var existingManager = await unitOfWork.LocationManagers.GetAsync(locationId.Value, user.Id, cancellationToken);
        if (existingManager is not null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.AlreadyManagerAtLocation);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            user.Role = RoleConstants.Manager;
            unitOfWork.Users.Update(user);

            employee.DepartmentId = null;
            employee.Position = RoleConstants.Manager;
            unitOfWork.Employees.Update(employee);

            await placement.ClearDepartmentMembershipsAsync(employee.Id, cancellationToken);
            await placement.EndActiveLocationMembershipAsync(employee.Id, actorUserId, cancellationToken);
            await placement.AssignManagerLocationsAsync(user.Id, organizationId, [locationId.Value], cancellationToken);

            await WriteAuditLogAsync(
                organizationId,
                actorUserId,
                employee.Id,
                fromRole,
                RoleConstants.Manager,
                locationId: locationId.Value,
                departmentId: null,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        jwtTokenService.RevokeRefreshToken(user.Id);
        return await employeeService.GetByIdAsync(employee.Id, cancellationToken);
    }

    private async Task<ApiResponse<EmployeeResponse>> DemoteToUserAsync(
        EmployeeEntity employee,
        UserEntity user,
        Guid organizationId,
        Guid actorUserId,
        string fromRole,
        EmployeeRoleTransitionRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(fromRole, RoleConstants.Manager, StringComparison.OrdinalIgnoreCase))
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.InvalidRoleTransition);

        if (!request.DepartmentId.HasValue || request.DepartmentId.Value == Guid.Empty)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentRequiredForUser);

        var department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken: cancellationToken);
        if (department is null || !department.IsActive || department.OrganizationId != organizationId)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

        if (string.Equals(user.Role, RoleConstants.User, StringComparison.OrdinalIgnoreCase)
            && employee.DepartmentId == department.Id)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.AlreadyUserInDepartment);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await placement.RemoveAllLocationManagersForUserAsync(user.Id, cancellationToken);

            user.Role = RoleConstants.User;
            unitOfWork.Users.Update(user);

            if (request.HourlyRate.HasValue)
                employee.HourlyRate = request.HourlyRate.Value;

            await placement.ApplyEmployeeDepartmentAsync(employee.Id, organizationId, department, cancellationToken);
            await placement.EnsureActiveLocationMembershipAsync(employee.Id, organizationId, department.LocationId, cancellationToken);

            await WriteAuditLogAsync(
                organizationId,
                actorUserId,
                employee.Id,
                fromRole,
                RoleConstants.User,
                locationId: department.LocationId,
                departmentId: department.Id,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        jwtTokenService.RevokeRefreshToken(user.Id);
        return await employeeService.GetByIdAsync(employee.Id, cancellationToken);
    }

    private async Task WriteAuditLogAsync(
        Guid organizationId,
        Guid actorUserId,
        Guid employeeId,
        string fromRole,
        string toRole,
        Guid? locationId,
        Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var beforeJson = JsonSerializer.Serialize(new { role = fromRole });
        var afterJson = JsonSerializer.Serialize(new { role = toRole, locationId, departmentId });

        await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ActorUserId = actorUserId,
                Action = RoleTransitionAuditAction,
                EntityType = EmployeeEntityType,
                EntityId = employeeId,
                BeforeJson = beforeJson,
                AfterJson = afterJson,
                OccurredAt = DateTime.UtcNow,
            },
            cancellationToken);
    }
}
