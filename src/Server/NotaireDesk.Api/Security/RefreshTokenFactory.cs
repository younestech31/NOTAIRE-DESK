using System.Security.Cryptography;
using System.Text;

namespace NotaireDesk.Api.Security;

public static class RefreshTokenFactory
{
    public static string Create()
    {
        var random = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(random).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(digest);
    }
}
