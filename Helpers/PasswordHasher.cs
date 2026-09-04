using System;
using System.Security.Cryptography;
using System.Text;

namespace KarzounERP.Helpers;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyPassword(string password, string? hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword)) return false;
        return HashPassword(password) == hashedPassword;
    }
}
