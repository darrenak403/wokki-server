using Wokki.Application.Dtos.Location;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.Locations;

public static class LocationMapper
{
    public static LocationResponse ToResponse(this Location location) =>
        new(location.Id, location.Name, location.Address, location.TimeZone, location.IsActive, location.CreatedAt);

    public static Location ToEntity(this CreateLocationRequest request) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            TimeZone = request.TimeZone.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public static void ApplyUpdate(this Location location, UpdateLocationRequest request)
    {
        location.Name = request.Name.Trim();
        location.Address = request.Address.Trim();
        location.TimeZone = request.TimeZone.Trim();
        location.IsActive = request.IsActive;
    }
}
