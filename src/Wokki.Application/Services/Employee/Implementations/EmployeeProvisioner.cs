using Wokki.Application.Services.Chat.Interfaces;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using DepartmentEntity = Wokki.Domain.Entities.Department;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using LocationMembershipEntity = Wokki.Domain.Entities.LocationMembership;

namespace Wokki.Application.Services.Employee.Implementations;

public sealed class EmployeeProvisioner(
    IUnitOfWork unitOfWork,
    IOrgChannelService orgChannelService) : IEmployeeProvisioner
{
    public async Task<EmployeeEntity> ProvisionUserEmployeeAsync(
        ProvisionEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        var department = await unitOfWork.Departments.GetByIdAsync(command.DepartmentId, cancellationToken: cancellationToken);
        if (department is null || !department.IsActive || department.OrganizationId != command.OrganizationId)
            throw new InvalidOperationException(AppMessages.Employee.DepartmentNotFound.Code);

        var user = await unitOfWork.Users.GetByIdAsync(command.UserId, track: true, cancellationToken: cancellationToken);
        if (user is null)
            throw new InvalidOperationException(AppMessages.User.NotFound.Code);

        var employee = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = command.OrganizationId,
            UserId = command.UserId,
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            Phone = command.Phone?.Trim() ?? string.Empty,
            HourlyRate = command.HourlyRate,
            DepartmentId = command.DepartmentId,
            Position = department.Name.Trim(),
            EmployedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Employees.AddAsync(employee, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.EmployeeDepartmentMemberships.ReplaceForEmployeeAsync(
            employee.Id,
            command.OrganizationId,
            [command.DepartmentId],
            command.DepartmentId,
            cancellationToken);

        await EnsureActiveLocationMembershipAsync(
            employee.Id,
            command.OrganizationId,
            department.LocationId,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await orgChannelService.EnsureOrgChannelAsync(command.OrganizationId, user.Id, cancellationToken);
        await orgChannelService.EnsureMemberAsync(command.OrganizationId, employee.Id, cancellationToken);

        return employee;
    }

    private async Task EnsureActiveLocationMembershipAsync(
        Guid employeeId,
        Guid organizationId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var active = await unitOfWork.LocationMemberships.GetActiveByEmployeeAsync(
            employeeId, track: true, cancellationToken: cancellationToken);
        if (active?.LocationId == locationId)
            return;

        if (active is not null)
        {
            active.Status = LocationMembershipStatus.Transferred;
            active.ReviewedAt = DateTime.UtcNow;
            unitOfWork.LocationMemberships.Update(active);
        }

        await unitOfWork.LocationMemberships.AddAsync(new LocationMembershipEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            LocationId = locationId,
            Status = LocationMembershipStatus.Active,
            RequestedAt = DateTime.UtcNow,
            ReviewedAt = DateTime.UtcNow,
        }, cancellationToken);
    }
}
