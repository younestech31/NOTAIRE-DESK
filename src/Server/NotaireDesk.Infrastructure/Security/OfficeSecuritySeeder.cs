using Microsoft.EntityFrameworkCore;
using NotaireDesk.Domain.Security;
using NotaireDesk.Infrastructure.Persistence;

namespace NotaireDesk.Infrastructure.Security;

public sealed class OfficeSecuritySeeder(OfficeDbContext db)
{
    public async Task SeedRolesAsync(CancellationToken cancellationToken = default)
    {
        var existing = await db.Roles.Include(x => x.Permissions)
            .ToDictionaryAsync(x => x.Code, StringComparer.Ordinal, cancellationToken);

        foreach (var definition in OfficeRoleCatalog.Definitions)
        {
            if (!existing.TryGetValue(definition.Code, out var role))
            {
                role = OfficeRole.Create(
                    definition.Code, definition.NameAr, definition.NameFr,
                    isSystemRole: true, DateTimeOffset.UtcNow);
                db.Roles.Add(role);
            }

            foreach (var permission in definition.Permissions)
                role.AddPermission(permission);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
