using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace QuanLyTaiKhoanAPI.Helper;

public static class TokenHelper
{
    public static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return WebEncoders.Base64UrlEncode(randomBytes);
    }

    public static string HashToken(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes);
    }
}
