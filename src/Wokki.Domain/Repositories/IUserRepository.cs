using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetOldestByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> ListWithoutEmployeeAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PlatformUserSnapshot> Items, int TotalCount)> ListPlatformAsync(
        int page,
        int pageSize,
        Guid? organizationId = null,
        string? role = null,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    void Remove(User user);
}
