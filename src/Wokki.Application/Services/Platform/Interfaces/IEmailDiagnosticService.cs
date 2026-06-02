using Wokki.Application.Dtos.Platform;

namespace Wokki.Application.Services.Platform.Interfaces;

public interface IEmailDiagnosticService
{
    PlatformDiagnosticComponentResponse Check();
}
