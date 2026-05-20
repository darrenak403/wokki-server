using Wokki.Application.Dtos.Shift;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Shift.Interfaces;

public interface IShiftDefinitionService
{
    Task<ApiResponse<IReadOnlyList<ShiftDefinitionResponse>>> ListAsync(
        Guid locationId,
        Guid? departmentId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<ShiftDefinitionResponse>> CreateAsync(
        CreateShiftDefinitionRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<ShiftDefinitionResponse>> UpdateAsync(
        Guid id,
        UpdateShiftDefinitionRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
