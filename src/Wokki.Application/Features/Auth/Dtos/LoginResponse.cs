namespace Wokki.Application.Features.Auth.Dtos;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string Role);
