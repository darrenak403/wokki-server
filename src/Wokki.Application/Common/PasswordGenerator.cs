using System.Security.Cryptography;

namespace Wokki.Application.Common;

internal static class PasswordGenerator
{
    private const string Chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$";

    public static string Generate(int length = 12)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Chars[bytes[i] % Chars.Length];
        return new string(chars);
    }
}
