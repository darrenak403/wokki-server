using Wokki.Application.Dtos.Bedrock;
using Wokki.Common.Utils;

namespace Wokki.Application.Services.Bedrock.Interfaces;

public interface IBedrockService
{
    Task<ApiResponse<BedrockHealthResponse>> GetHealthAsync(CancellationToken cancellationToken = default);

    Task<BedrockConverseResult> ConverseAsync(
        string userPrompt,
        BedrockConverseOptions options,
        CancellationToken cancellationToken = default);
}
