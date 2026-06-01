using EmployeeEntity = Wokki.Domain.Entities.Employee;

namespace Wokki.Application.Services.Employee.Interfaces;

public interface IEmployeeProvisioner
{
    Task<EmployeeEntity> ProvisionUserEmployeeAsync(
        ProvisionEmployeeCommand command,
        CancellationToken cancellationToken = default);
}
