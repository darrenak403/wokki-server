using Wokki.Application.Dtos.Department;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Department.Interfaces;

public interface IDepartmentService
{
    Task<ApiResponse<IReadOnlyList<DepartmentResponse>>> ListAsync(Guid? locationId, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartmentResponse>> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartmentResponse>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
}
