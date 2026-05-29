using System.Security.Cryptography;

namespace Wokki.Application.Services.Auth;

public static class AuthOtpHelper
{
    public const int MaxVerifyAttempts = 5;
    public const int MaxSendAttempts = 5;
    public static readonly TimeSpan Expiry = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan VerifiedWindow = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan SendLockout = TimeSpan.FromMinutes(30);

    public static string GenerateNumericCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }
}
