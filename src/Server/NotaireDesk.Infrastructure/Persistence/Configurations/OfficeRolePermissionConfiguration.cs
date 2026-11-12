using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Security;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class OfficeRolePermissionConfiguration : IEntityTypeConfiguration<OfficeRolePermission>
{
    public void Configure(EntityTypeBuilder<OfficeRolePermission> builder)
    {
        builder.ToTable("office_role_permissions");
        builder.HasKey(x => new { x.RoleId, x.Permission });
        builder.Property(x => x.Permission).HasMaxLength(80).IsRequired();
    }
}
