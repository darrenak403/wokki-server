using Wokki.Application.Dtos.Employee;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Employee.Interfaces;

public interface IEmployeeRoleTransitionService
{
    Task<ApiResponse<EmployeeResponse>> TransitionAsync(
        Guid employeeId,
        EmployeeRoleTransitionRequest request,
        CancellationToken cancellationToken = default);
}
