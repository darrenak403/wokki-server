namespace Wokki.Api.Bootstrapping;

/// <summary>
/// Trusted proxy chain for `UseForwardedHeaders`. Production topology: GitHub Actions -> Docker Hub ->
/// Dokploy webhook -> Dokploy's internal Traefik, fronting `wokki_api` over the `wokki-network` Docker
/// bridge (see docker/docker-compose.prod.yml). Set KnownNetworks to that bridge's actual subnet on the
/// VPS (inspect the running network — do not guess; Dokploy-managed networks are not always
/// 172.16.0.0/12). Local dev has no reverse proxy, so this is intentionally left empty there.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public string[] KnownProxies { get; init; } = [];

    /// <summary>CIDR strings, e.g. "172.20.0.0/16".</summary>
    public string[] KnownNetworks { get; init; } = [];

    public bool IsConfigured =>
        Array.Exists(KnownProxies, s => !string.IsNullOrWhiteSpace(s))
        || Array.Exists(KnownNetworks, s => !string.IsNullOrWhiteSpace(s));
}
