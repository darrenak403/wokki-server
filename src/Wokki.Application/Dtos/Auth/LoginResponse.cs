namespace Wokki.Application.Dtos.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    bool MustChangePassword = false);
