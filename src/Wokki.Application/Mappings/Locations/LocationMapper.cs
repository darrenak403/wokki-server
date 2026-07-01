using Wokki.Application.Dtos.Location;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.Locations;

public static class LocationMapper
{
    public static LocationResponse ToResponse(this Location location) =>
        new(
            location.Id,
            location.Name,
            location.Address,
            location.TimeZone,
            location.IsActive,
            location.CreatedAt,
            location.Latitude,
            location.Longitude,
            location.NetworkIpOrCidr);

    public static Location ToEntity(this CreateLocationRequest request, Guid organizationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            TimeZone = request.TimeZone.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            NetworkIpOrCidr = string.IsNullOrWhiteSpace(request.NetworkIpOrCidr) ? null : request.NetworkIpOrCidr.Trim()
        };

    public static void ApplyUpdate(this Location location, UpdateLocationRequest request)
    {
        location.Name = request.Name.Trim();
        location.Address = request.Address.Trim();
        location.TimeZone = request.TimeZone.Trim();
        location.IsActive = request.IsActive;
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.NetworkIpOrCidr = string.IsNullOrWhiteSpace(request.NetworkIpOrCidr) ? null : request.NetworkIpOrCidr.Trim();
    }
}
