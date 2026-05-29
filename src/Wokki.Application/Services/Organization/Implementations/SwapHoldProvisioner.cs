using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Services.Organization.Implementations;

public static class SwapHoldProvisioner
{
    public static async Task<Guid> EnsureForOrganizationAsync(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        Guid organizationId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        var existing = await unitOfWork.Employees.GetSwapHoldByOrganizationAsync(organizationId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var email = $"swap-hold+{organizationId:N}@wokki.system";

        var user = new Domain.Entities.User
        {
            Id = userId,
            Email = email,
            PasswordHash = passwordHasher.HashPassword(Guid.NewGuid().ToString("N")),
            Role = RoleConstants.User,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };

        var employee = new Domain.Entities.Employee
        {
            Id = employeeId,
            OrganizationId = organizationId,
            UserId = userId,
            DepartmentId = departmentId,
            FirstName = "Swap",
            LastName = "Hold",
            Position = "System",
            HourlyRate = 0m,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Users.AddAsync(user, cancellationToken);
        await unitOfWork.Employees.AddAsync(employee, cancellationToken);
        return employeeId;
    }
}
