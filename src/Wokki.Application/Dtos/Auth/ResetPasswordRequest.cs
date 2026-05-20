namespace Wokki.Application.Dtos.Auth;

public sealed record ResetPasswordRequest(string Email, string NewPassword);
