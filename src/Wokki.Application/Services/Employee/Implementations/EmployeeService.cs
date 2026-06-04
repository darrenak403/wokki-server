using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Employee;
using Wokki.Application.Mappings.Employees;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Application.Services.Chat.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using DepartmentEntity = Wokki.Domain.Entities.Department;
using LocationEntity = Wokki.Domain.Entities.Location;
namespace Wokki.Application.Services.Employee.Implementations;

public sealed class EmployeeService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IOrganizationScopeService organizationScope,
    IOrgChannelService orgChannelService,
    IOrgAdminEmployeeProvisioner orgAdminEmployeeProvisioner,
    IStaffPlacementCoordinator placement) : IEmployeeService
{
    public async Task<ApiResponse<EmployeeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (employee is null || !organizationScope.IsSameOrganization(employee.OrganizationId))
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.NotFound);

        var response = await BuildResponseAsync(employee, cancellationToken);
        return ApiResponse<EmployeeResponse>.SuccessResponse(response, AppMessages.Employee.Found);
    }

    public async Task<ApiResponse<IReadOnlyList<EmployeeDepartmentMembershipResponse>>> ListDepartmentMembershipsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (employee is null || !organizationScope.IsSameOrganization(employee.OrganizationId))
            return ApiResponse<IReadOnlyList<EmployeeDepartmentMembershipResponse>>.FailureResponse(AppMessages.Employee.NotFound);

        var memberships = await unitOfWork.EmployeeDepartmentMemberships.ListByEmployeeAsync(id, cancellationToken);
        var responses = new List<EmployeeDepartmentMembershipResponse>(memberships.Count);

        foreach (var membership in memberships)
        {
            var department = await unitOfWork.Departments.GetByIdAsync(membership.DepartmentId, cancellationToken: cancellationToken);
            LocationEntity? location = null;
            if (department is not null)
                location = await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: cancellationToken);

            responses.Add(new EmployeeDepartmentMembershipResponse(
                membership.DepartmentId,
                department?.Name,
                location?.Id,
                location?.Name,
                membership.Status,
                membership.IsPrimary,
                membership.JoinedAt,
                membership.LeftAt));
        }

        return ApiResponse<IReadOnlyList<EmployeeDepartmentMembershipResponse>>.SuccessResponse(
            responses,
            AppMessages.Employee.DepartmentMembershipsListed);
    }

    public async Task<ApiResponse<PagedResponse<EmployeeResponse>>> ListAsync(
        EmployeeListRequest request,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (items, total) = await unitOfWork.Employees.ListAsync(
            page,
            pageSize,
            organizationScope.GetCurrentOrganizationId(),
            request.DepartmentId,
            request.LocationId,
            request.IncludeTerminated,
            locationIds,
            request.Search,
            cancellationToken);

        var responses = new List<EmployeeResponse>(items.Count);
        foreach (var employee in items)
            responses.Add(await BuildResponseAsync(employee, cancellationToken));

        return ApiResponse<PagedResponse<EmployeeResponse>>.SuccessPagedResponse(
            responses,
            page,
            pageSize,
            total,
            AppMessages.Employee.Listed);
    }

    public async Task<ApiResponse<CreateEmployeeResponse>> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var organizationId = organizationScope.RequireOrganizationId();
        var existingUser = await unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            if (existingUser.OrganizationId != organizationId)
                return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.UserAlreadyLinked);

            var linkedEmployee = await unitOfWork.Employees.GetByUserIdAsync(existingUser.Id, cancellationToken);
            if (linkedEmployee is not null)
                return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.UserAlreadyLinked);

            if (string.Equals(existingUser.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            {
                var provisioned = await orgAdminEmployeeProvisioner.EnsureAsync(existingUser, cancellationToken);
                if (provisioned is null)
                    return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.NotFound);

                var adminCreateData = new CreateEmployeeResponse(
                    provisioned.Id,
                    existingUser.Id,
                    existingUser.Email,
                    string.Empty);
                return ApiResponse<CreateEmployeeResponse>.SuccessResponse(adminCreateData, AppMessages.Employee.Created);
            }
        }

        var requiresDepartment = request.Role == RoleConstants.User;
        if (requiresDepartment && !request.DepartmentId.HasValue)
            return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentRequiredForUser);

        var requiresManagerLocations = request.Role == RoleConstants.Manager;
        var managerLocationIds = NormalizeLocationIds(request.LocationIds);
        if (requiresManagerLocations && managerLocationIds.Count == 0)
            return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.ManagerLocationsRequired);

        if (requiresManagerLocations && !await AllLocationsActiveAsync(managerLocationIds, organizationId, cancellationToken))
            return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.ManagerLocationNotFound);

        DepartmentEntity? department = null;
        IReadOnlyList<Guid> departmentIds = [];
        if (request.DepartmentId.HasValue)
        {
            department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId.Value, cancellationToken: cancellationToken);
            if (department is null || !department.IsActive || department.OrganizationId != organizationId)
                return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

            departmentIds = NormalizeDepartmentIds(request.DepartmentId.Value, request.DepartmentIds);
            if (!await AllDepartmentsActiveAsync(departmentIds, organizationId, cancellationToken))
                return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(request.Password)
            ? PasswordGenerator.Generate()
            : request.Password;

        var user = existingUser ?? new Wokki.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = email,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(temporaryPassword);
        if (!string.Equals(existingUser?.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            user.Role = request.Role;
        user.MustChangePassword = string.IsNullOrWhiteSpace(request.Password);

        var employee = request.ToEntity(user.Id, organizationId);
        if (department is not null)
            EmployeeMapper.SyncPositionFromDepartment(employee, department);
        else
            employee.Position = request.Role;

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (existingUser is null)
                await unitOfWork.Users.AddAsync(user, cancellationToken);
            else
                unitOfWork.Users.Update(user);

            await unitOfWork.Employees.AddAsync(employee, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            if (department is not null && request.DepartmentId.HasValue)
            {
                await unitOfWork.EmployeeDepartmentMemberships.ReplaceForEmployeeAsync(
                    employee.Id,
                    organizationId,
                    departmentIds,
                    request.DepartmentId.Value,
                    cancellationToken);
                await placement.EnsureActiveLocationMembershipAsync(
                    employee.Id,
                    organizationId,
                    department.LocationId,
                    cancellationToken);
            }

            if (requiresManagerLocations)
                await placement.AssignManagerLocationsAsync(user.Id, organizationId, managerLocationIds, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await orgChannelService.EnsureOrgChannelAsync(organizationId, user.Id, cancellationToken);
        await orgChannelService.EnsureMemberAsync(organizationId, employee.Id, cancellationToken);

        var data = new CreateEmployeeResponse(employee.Id, user.Id, user.Email, temporaryPassword);
        return ApiResponse<CreateEmployeeResponse>.SuccessResponse(data, AppMessages.Employee.Created);
    }

    public async Task<ApiResponse<EmployeeResponse>> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (employee is null || employee.TerminatedAt is not null || !organizationScope.IsSameOrganization(employee.OrganizationId))
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.NotFound);

        var linkedUser = await unitOfWork.Users.GetByIdAsync(employee.UserId, cancellationToken: cancellationToken);
        if (linkedUser is not null
            && string.Equals(linkedUser.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.OrgAdminNoDepartment);

        var department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId, cancellationToken: cancellationToken);
        if (department is null || !department.IsActive || department.OrganizationId != employee.OrganizationId)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

        var departmentIds = NormalizeDepartmentIds(request.DepartmentId, request.DepartmentIds);
        if (!await AllDepartmentsActiveAsync(departmentIds, employee.OrganizationId, cancellationToken))
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

        employee.ApplyUpdate(request);
        EmployeeMapper.SyncPositionFromDepartment(employee, department);
        unitOfWork.Employees.Update(employee);
        await unitOfWork.EmployeeDepartmentMemberships.ReplaceForEmployeeAsync(
            employee.Id,
            employee.OrganizationId,
            departmentIds,
            request.DepartmentId,
            cancellationToken);
        await placement.EnsureActiveLocationMembershipAsync(
            employee.Id,
            employee.OrganizationId,
            department.LocationId,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await BuildResponseAsync(employee, cancellationToken);
        return ApiResponse<EmployeeResponse>.SuccessResponse(response, AppMessages.Employee.Updated);
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (employee is null || !organizationScope.IsSameOrganization(employee.OrganizationId))
            return ApiResponse<object>.FailureResponse(AppMessages.Employee.NotFound);

        if (employee.TerminatedAt is not null)
            return ApiResponse<object>.FailureResponse(AppMessages.Employee.AlreadyTerminated);

        employee.TerminatedAt = DateTime.UtcNow;
        unitOfWork.Employees.Update(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await orgChannelService.RemoveMemberAsync(employee.OrganizationId, employee.Id, cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.Employee.Deleted);
    }

    private async Task<EmployeeResponse> BuildResponseAsync(EmployeeEntity employee, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(employee.UserId, cancellationToken: cancellationToken)
                   ?? throw new InvalidOperationException($"User {employee.UserId} not found for employee {employee.Id}.");

        DepartmentEntity? department = null;
        if (!string.Equals(user.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase)
            && employee.DepartmentId.HasValue)
            department = await unitOfWork.Departments.GetByIdAsync(employee.DepartmentId.Value, cancellationToken: cancellationToken);
        LocationEntity? location = null;
        if (department is not null)
            location = await unitOfWork.Locations.GetByIdAsync(department.LocationId, cancellationToken: cancellationToken);

        return employee.ToResponse(user, department, location);
    }

    private static IReadOnlyList<Guid> NormalizeDepartmentIds(Guid primaryDepartmentId, IReadOnlyList<Guid>? departmentIds) =>
        (departmentIds ?? [])
        .Append(primaryDepartmentId)
        .Where(id => id != Guid.Empty)
        .Distinct()
        .ToList();

    private async Task<bool> AllDepartmentsActiveAsync(
        IReadOnlyList<Guid> departmentIds,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        foreach (var id in departmentIds)
        {
            var department = await unitOfWork.Departments.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (department is null || !department.IsActive || department.OrganizationId != organizationId)
                return false;
        }

        return true;
    }

    private static IReadOnlyList<Guid> NormalizeLocationIds(IReadOnlyList<Guid>? locationIds) =>
        (locationIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

    private async Task<bool> AllLocationsActiveAsync(
        IReadOnlyList<Guid> locationIds,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        foreach (var id in locationIds)
        {
            var location = await unitOfWork.Locations.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (location is null || !location.IsActive || location.OrganizationId != organizationId)
                return false;
        }

        return true;
    }

}
