namespace Wokki.Application.Dtos.Location;

public sealed record CreateLocationRequest(
    string Name,
    string Address,
    string TimeZone = "UTC",
    double? Latitude = null,
    double? Longitude = null,
    string? NetworkIpOrCidr = null);
