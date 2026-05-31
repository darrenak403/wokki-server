using EmployeeEntity = Wokki.Domain.Entities.Employee;
using UserEntity = Wokki.Domain.Entities.User;

namespace Wokki.Application.Services.Employee.Interfaces;

/// <summary>
/// Ensures org Admin/Manager users have an Employee row (chat, self profile, org channel membership).
/// </summary>
public interface IOrgAdminEmployeeProvisioner
{
    Task<EmployeeEntity?> EnsureAsync(UserEntity user, CancellationToken cancellationToken = default);

    Task<EmployeeEntity?> EnsureByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures org Admin / org creator user has no department and correct role before chat member listing.
    /// </summary>
    Task<bool> RepairOrgAdminMemberAsync(
        Guid organizationId,
        Guid userId,
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
