using System.Net;

namespace Wokki.Application.Common;

internal static class IpNetworkMatcher
{
    /// <summary>
    /// True if <paramref name="ip"/> falls inside <paramref name="networkOrIp"/>, which may be a single
    /// IP ("203.0.113.5") or a CIDR range ("203.0.113.0/24"). False on any unparsable input.
    /// </summary>
    public static bool IsInRange(string? ip, string? networkOrIp)
    {
        if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(networkOrIp))
            return false;

        if (!IPAddress.TryParse(ip, out var address))
            return false;

        if (networkOrIp.Contains('/'))
            return IPNetwork.TryParse(networkOrIp, out var network) && network.Contains(address);

        return IPAddress.TryParse(networkOrIp, out var single) && single.Equals(address);
    }
}
