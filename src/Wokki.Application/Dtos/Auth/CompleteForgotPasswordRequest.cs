namespace Wokki.Application.Dtos.Auth;

public sealed record CompleteForgotPasswordRequest(
    string Email,
    string NewPassword,
    string ConfirmNewPassword);
