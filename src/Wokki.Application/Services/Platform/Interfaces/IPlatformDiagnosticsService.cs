using Wokki.Application.Dtos.Platform;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Platform.Interfaces;

public interface IPlatformDiagnosticsService
{
    Task<ApiResponse<PlatformHealthResponse>> GetHealthAsync(CancellationToken cancellationToken = default);
}
