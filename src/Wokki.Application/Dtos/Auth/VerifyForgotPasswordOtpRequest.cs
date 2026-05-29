namespace Wokki.Application.Dtos.Auth;

public sealed record VerifyForgotPasswordOtpRequest(string Email, string OtpCode);
