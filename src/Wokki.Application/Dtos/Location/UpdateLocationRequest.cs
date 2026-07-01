namespace Wokki.Application.Dtos.Location;

public sealed record UpdateLocationRequest(
    string Name,
    string Address,
    string TimeZone,
    bool IsActive,
    double? Latitude = null,
    double? Longitude = null,
    string? NetworkIpOrCidr = null);
