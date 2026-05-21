using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IEmployeeAvailabilityRepository
{
    Task<IReadOnlyList<EmployeeAvailability>> ListByEmployeeIdsAsync(
        IEnumerable<Guid> employeeIds,
        CancellationToken cancellationToken = default);
}
