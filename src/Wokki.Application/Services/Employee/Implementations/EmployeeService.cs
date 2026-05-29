using Wokki.Application.Common;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Employee;
using Wokki.Application.Mappings.Employees;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Repositories;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using LocationEntity = Wokki.Domain.Entities.Location;

namespace Wokki.Application.Services.Employee.Implementations;

public sealed class EmployeeService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher) : IEmployeeService
{
    public async Task<ApiResponse<EmployeeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (employee is null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.NotFound);

        var response = await BuildResponseAsync(employee, cancellationToken);
        return ApiResponse<EmployeeResponse>.SuccessResponse(response, AppMessages.Employee.Found);
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
            request.DepartmentId,
            request.LocationId,
            request.IncludeTerminated,
            locationIds,
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
        var existingUser = await unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
            return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.UserAlreadyLinked);

        var department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId, cancellationToken: cancellationToken);
        if (department is null || !department.IsActive)
            return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

        var departmentIds = NormalizeDepartmentIds(request.DepartmentId, request.DepartmentIds);
        if (!await AllDepartmentsActiveAsync(departmentIds, cancellationToken))
            return ApiResponse<CreateEmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

        var temporaryPassword = string.IsNullOrWhiteSpace(request.Password)
            ? PasswordGenerator.Generate()
            : request.Password;

        var user = new Wokki.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHasher.HashPassword(temporaryPassword),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        var employee = request.ToEntity(user.Id);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.Users.AddAsync(user, cancellationToken);
            await unitOfWork.Employees.AddAsync(employee, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.EmployeeDepartmentMemberships.ReplaceForEmployeeAsync(
                employee.Id,
                departmentIds,
                request.DepartmentId,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var data = new CreateEmployeeResponse(employee.Id, user.Id, user.Email, temporaryPassword);
        return ApiResponse<CreateEmployeeResponse>.SuccessResponse(data, AppMessages.Employee.Created);
    }

    public async Task<ApiResponse<EmployeeResponse>> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (employee is null || employee.TerminatedAt is not null)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.NotFound);

        var department = await unitOfWork.Departments.GetByIdAsync(request.DepartmentId, cancellationToken: cancellationToken);
        if (department is null || !department.IsActive)
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

        var departmentIds = NormalizeDepartmentIds(request.DepartmentId, request.DepartmentIds);
        if (!await AllDepartmentsActiveAsync(departmentIds, cancellationToken))
            return ApiResponse<EmployeeResponse>.FailureResponse(AppMessages.Employee.DepartmentNotFound);

        employee.ApplyUpdate(request);
        unitOfWork.Employees.Update(employee);
        await unitOfWork.EmployeeDepartmentMemberships.ReplaceForEmployeeAsync(
            employee.Id,
            departmentIds,
            request.DepartmentId,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await BuildResponseAsync(employee, cancellationToken);
        return ApiResponse<EmployeeResponse>.SuccessResponse(response, AppMessages.Employee.Updated);
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(id, track: true, cancellationToken: cancellationToken);
        if (employee is null)
            return ApiResponse<object>.FailureResponse(AppMessages.Employee.NotFound);

        if (employee.TerminatedAt is not null)
            return ApiResponse<object>.FailureResponse(AppMessages.Employee.AlreadyTerminated);

        employee.TerminatedAt = DateTime.UtcNow;
        unitOfWork.Employees.Update(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { }, AppMessages.Employee.Deleted);
    }

    private async Task<EmployeeResponse> BuildResponseAsync(EmployeeEntity employee, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(employee.UserId, cancellationToken)
                   ?? throw new InvalidOperationException($"User {employee.UserId} not found for employee {employee.Id}.");

        var department = await unitOfWork.Departments.GetByIdAsync(employee.DepartmentId, cancellationToken: cancellationToken);
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
        CancellationToken cancellationToken)
    {
        foreach (var id in departmentIds)
        {
            var department = await unitOfWork.Departments.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (department is null || !department.IsActive)
                return false;
        }

        return true;
    }
}
