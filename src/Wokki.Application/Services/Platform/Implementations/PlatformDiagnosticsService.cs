using Wokki.Application.Dtos.Platform;
using Wokki.Application.Services.Bedrock.Interfaces;
using Wokki.Application.Services.OrganizationScope.Interfaces;
using Wokki.Application.Services.Platform.Interfaces;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Platform.Implementations;

public sealed class PlatformDiagnosticsService(
    IOrganizationScopeService organizationScope,
    IBedrockService bedrockService,
    IEmailDiagnosticService emailDiagnosticService,
    IPlatformDiagnosticState diagnosticState) : IPlatformDiagnosticsService
{
    private const string ApiComponent = "api";
    private const string BedrockComponent = "bedrock";

    public async Task<ApiResponse<PlatformHealthResponse>> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        if (!organizationScope.IsPlatformOperator)
            return ApiResponse<PlatformHealthResponse>.FailureResponse(AppMessages.Auth.Forbidden);

        var checkedAt = DateTime.UtcNow;
        var components = new List<PlatformDiagnosticComponentResponse>
        {
            new(
                ApiComponent,
                "Healthy",
                checkedAt,
                diagnosticState.GetLastFailure(ApiComponent))
        };

        components.Add(await CheckBedrockAsync(cancellationToken));
        components.Add(emailDiagnosticService.Check());

        var overall = components.Any(IsDegraded)
            ? "Degraded"
            : "Healthy";

        return ApiResponse<PlatformHealthResponse>.SuccessResponse(
            new PlatformHealthResponse(overall, checkedAt, components),
            AppMessages.Platform.HealthFound);
    }

    private async Task<PlatformDiagnosticComponentResponse> CheckBedrockAsync(CancellationToken cancellationToken)
    {
        var checkedAt = DateTime.UtcNow;
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await bedrockService.GetHealthAsync(timeoutCts.Token);
            if (!response.Success)
            {
                diagnosticState.RecordFailure(
                    BedrockComponent,
                    response.Data?.ErrorCode ?? response.Message.Code,
                    response.Data?.ErrorMessage ?? response.Message.Text,
                    DateTime.UtcNow);
            }

            return new PlatformDiagnosticComponentResponse(
                BedrockComponent,
                response.Data?.Status ?? (response.Success ? "Connected" : "Disconnected"),
                DateTime.UtcNow,
                diagnosticState.GetLastFailure(BedrockComponent));
        }
        catch (Exception ex)
        {
            diagnosticState.RecordFailure(
                BedrockComponent,
                ex.GetType().Name,
                ex.Message,
                DateTime.UtcNow);

            return new PlatformDiagnosticComponentResponse(
                BedrockComponent,
                "Disconnected",
                checkedAt,
                diagnosticState.GetLastFailure(BedrockComponent));
        }
    }

    private static bool IsDegraded(PlatformDiagnosticComponentResponse component)
    {
        if (string.Equals(component.Name, BedrockComponent, StringComparison.OrdinalIgnoreCase))
            return !string.Equals(component.Status, "Connected", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(component.Name, ApiComponent, StringComparison.OrdinalIgnoreCase))
            return !string.Equals(component.Status, "Healthy", StringComparison.OrdinalIgnoreCase);

        return string.Equals(component.Status, "Failed", StringComparison.OrdinalIgnoreCase)
               || string.Equals(component.Status, "Disconnected", StringComparison.OrdinalIgnoreCase);
    }
}
