namespace Wokki.Application.Dtos.LocationManager;

public sealed record LocationManagerResponse(
    Guid Id,
    Guid LocationId,
    string LocationName,
    Guid UserId,
    string UserEmail,
    Guid AssignedById,
    DateTime AssignedAt);
