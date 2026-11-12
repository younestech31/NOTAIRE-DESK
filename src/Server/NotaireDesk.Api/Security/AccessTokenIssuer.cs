using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using NotaireDesk.Domain.Security;

namespace NotaireDesk.Api.Security;

public sealed class AccessTokenIssuer(JwtOptions options)
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    public string Create(OfficeUser user, Guid sessionId, IEnumerable<string> roleCodes, IEnumerable<string> permissions)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString("D")),
            new(JwtRegisteredClaimNames.Jti, sessionId.ToString("D")),
            new(ClaimTypes.NameIdentifier, user.Id.ToString("D")),
            new(ClaimTypes.Name, user.Username)
        };

        claims.AddRange(roleCodes.Distinct(StringComparer.Ordinal)
            .Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Distinct(StringComparer.Ordinal)
            .Select(permission => new Claim("permission", permission)));

        var credentials = new SigningCredentials(options.SigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(AccessTokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
