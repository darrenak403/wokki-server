namespace Wokki.Application.Dtos.Location;

public sealed record LocationResponse(
    Guid Id,
    string Name,
    string Address,
    string TimeZone,
    bool IsActive,
    DateTime CreatedAt,
    double? Latitude = null,
    double? Longitude = null,
    string? NetworkIpOrCidr = null);
