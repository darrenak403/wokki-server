namespace Wokki.Api.Bootstrapping;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string FrontendPolicy = "Frontend";

    public string[] AllowedOrigins { get; init; } = [];
}
