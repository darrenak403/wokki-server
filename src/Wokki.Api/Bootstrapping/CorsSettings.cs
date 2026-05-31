namespace Wokki.Api.Bootstrapping;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string FrontendPolicy = "Frontend";

    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>
    /// When true, reflect any request origin (required for AllowCredentials; cannot use AllowAnyOrigin()).
    /// </summary>
    public bool AllowAnyOrigin { get; init; }
}
