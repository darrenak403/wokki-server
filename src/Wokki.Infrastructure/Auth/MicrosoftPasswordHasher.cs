using Microsoft.AspNetCore.Identity;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Auth;

public sealed class MicrosoftPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();

    public string HashPassword(string password)
    {
        var user = new User();
        return Hasher.HashPassword(user, password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        var user = new User();
        try
        {
            var result = Hasher.VerifyHashedPassword(user, passwordHash, password);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return passwordHash == password;
        }
    }
}
