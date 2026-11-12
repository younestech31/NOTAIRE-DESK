using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace NotaireDesk.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required SymmetricSecurityKey SigningKey { get; init; }

    public static JwtOptions Load(IConfiguration configuration)
    {
        var encodedKey = configuration[$"{SectionName}:SigningKey"];
        if (string.IsNullOrWhiteSpace(encodedKey))
            throw new InvalidOperationException(
                "Missing JWT signing key. Configure Authentication:Jwt:SigningKey using a secret environment variable.");

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(encodedKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("JWT signing key must be Base64-encoded.", exception);
        }

        if (keyBytes.Length < 32)
            throw new InvalidOperationException("JWT signing key must contain at least 256 bits.");

        var issuer = configuration[$"{SectionName}:Issuer"] ?? "NOTAIRE-DESK.OfficeApi";
        var audience = configuration[$"{SectionName}:Audience"] ?? "NOTAIRE-DESK.WindowsClient";
        if (issuer.Length > 200 || audience.Length > 200)
            throw new InvalidOperationException("JWT issuer and audience must not exceed 200 characters.");

        return new JwtOptions
        {
            Issuer = issuer,
            Audience = audience,
            SigningKey = new SymmetricSecurityKey(keyBytes)
        };
    }
}
