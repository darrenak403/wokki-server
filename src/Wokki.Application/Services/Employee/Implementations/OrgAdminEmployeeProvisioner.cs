using System.Globalization;
using Wokki.Application.Services.Chat.Interfaces;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using UserEntity = Wokki.Domain.Entities.User;

namespace Wokki.Application.Services.Employee.Implementations;

public sealed class OrgAdminEmployeeProvisioner(
    IUnitOfWork unitOfWork,
    IOrgChannelService orgChannelService) : IOrgAdminEmployeeProvisioner
{
    public async Task<EmployeeEntity?> EnsureByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user is null)
            return null;

        return await EnsureAsync(user, cancellationToken);
    }

    public async Task<bool> RepairOrgAdminMemberAsync(
        Guid organizationId,
        Guid userId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId, track: true, cancellationToken: cancellationToken);
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, track: true, cancellationToken: cancellationToken);
        if (user is null || employee is null || employee.OrganizationId != organizationId)
            return false;

        if (!await ShouldTreatAsOrgAdminAsync(user, organizationId, cancellationToken))
            return false;

        var dirty = false;
        if (!string.Equals(user.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
        {
            user.Role = RoleConstants.Admin;
            dirty = true;
        }

        if (employee.DepartmentId.HasValue)
        {
            employee.DepartmentId = null;
            dirty = true;
        }

        if (!string.Equals(employee.Position, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
        {
            employee.Position = RoleConstants.Admin;
            dirty = true;
        }

        if (dirty)
        {
            unitOfWork.Users.Update(user);
            unitOfWork.Employees.Update(employee);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await orgChannelService.EnsureOrgChannelAsync(organizationId, user.Id, cancellationToken);
        await orgChannelService.EnsureMemberAsync(organizationId, employee.Id, cancellationToken);
        return true;
    }

    public async Task<EmployeeEntity?> EnsureAsync(
        UserEntity user,
        CancellationToken cancellationToken = default)
    {
        if (user.OrganizationId is null || !ShouldAutoProvision(user.Role))
            return null;

        var existing = await unitOfWork.Employees.GetByUserIdAsync(user.Id, cancellationToken);
        if (existing is not null)
        {
            if (IsOrgWideRole(user.Role) && existing.DepartmentId.HasValue)
            {
                existing.DepartmentId = null;
                unitOfWork.Employees.Update(existing);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (IsOrgWideRole(user.Role)
                && !string.Equals(existing.Position, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            {
                existing.Position = RoleConstants.Admin;
                unitOfWork.Employees.Update(existing);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await orgChannelService.EnsureOrgChannelAsync(user.OrganizationId.Value, user.Id, cancellationToken);
            await orgChannelService.EnsureMemberAsync(user.OrganizationId.Value, existing.Id, cancellationToken);
            return existing;
        }

        var (firstName, lastName) = DeriveNamesFromEmail(user.Email);
        var departmentId = IsOrgWideRole(user.Role)
            ? null
            : await ResolveDefaultDepartmentIdAsync(user.OrganizationId.Value, cancellationToken);

        var employee = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId.Value,
            UserId = user.Id,
            FirstName = firstName,
            LastName = lastName,
            Phone = string.Empty,
            Position = user.Role,
            HourlyRate = 0,
            DepartmentId = departmentId,
            EmployedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Employees.AddAsync(employee, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await orgChannelService.EnsureOrgChannelAsync(user.OrganizationId.Value, user.Id, cancellationToken);
        await orgChannelService.EnsureMemberAsync(user.OrganizationId.Value, employee.Id, cancellationToken);

        return employee;
    }

    private async Task<bool> ShouldTreatAsOrgAdminAsync(
        UserEntity user,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(user.Role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
            return true;

        var creator = await unitOfWork.Users.GetOldestByOrganizationIdAsync(organizationId, cancellationToken);
        return creator is not null && creator.Id == user.Id;
    }

    private static bool ShouldAutoProvision(string role) =>
        string.Equals(role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, RoleConstants.Manager, StringComparison.OrdinalIgnoreCase);

    private static bool IsOrgWideRole(string role) =>
        string.Equals(role, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase);

    private async Task<Guid?> ResolveDefaultDepartmentIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var departments = await unitOfWork.Departments.ListAsync(
            organizationId: organizationId,
            cancellationToken: cancellationToken);

        return departments.Count > 0 ? departments[0].Id : null;
    }

    internal static (string FirstName, string LastName) DeriveNamesFromEmail(string email)
    {
        var local = email.Split('@')[0].Trim();
        if (string.IsNullOrWhiteSpace(local))
            return ("Admin", string.Empty);

        var parts = local
            .Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length >= 2)
            return (CapitalizeWord(parts[0]), CapitalizeWord(parts[1]));

        return (CapitalizeWord(local), string.Empty);
    }

    private static string CapitalizeWord(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }
}
