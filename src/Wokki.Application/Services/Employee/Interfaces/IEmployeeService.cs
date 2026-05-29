using Wokki.Application.Dtos.Employee;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Employee.Interfaces;

public interface IEmployeeService
{
    Task<ApiResponse<EmployeeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<EmployeeDepartmentMembershipResponse>>> ListDepartmentMembershipsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<EmployeeResponse>>> ListAsync(
        EmployeeListRequest request,
        IReadOnlySet<Guid>? locationIds = null,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<CreateEmployeeResponse>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmployeeResponse>> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
