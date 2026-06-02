using Microsoft.Extensions.Options;
using Wokki.Application.Dtos.Platform;
using Wokki.Application.Services.Platform.Interfaces;

namespace Wokki.Infrastructure.Notifications;

public sealed class EmailDiagnosticService(
    IOptions<SmtpSettings> smtpOptions,
    IPlatformDiagnosticState diagnosticState) : IEmailDiagnosticService
{
    private const string ComponentName = "email";

    public PlatformDiagnosticComponentResponse Check()
    {
        var status = smtpOptions.Value.IsConfigured ? "Configured" : "NotConfigured";
        return new PlatformDiagnosticComponentResponse(
            ComponentName,
            status,
            DateTime.UtcNow,
            diagnosticState.GetLastFailure(ComponentName));
    }
}
