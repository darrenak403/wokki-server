namespace Wokki.Application.Dtos.Location;

public sealed record CreateLocationRequest(string Name, string Address, string TimeZone = "UTC");
