using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Security;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class OfficeRoleConfiguration : IEntityTypeConfiguration<OfficeRole>
{
    public void Configure(EntityTypeBuilder<OfficeRole> builder)
    {
        builder.ToTable("office_roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameFr).HasMaxLength(150);
        builder.Property(x => x.IsSystemRole).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasMany<OfficeRolePermission>(nameof(OfficeRole.Permissions))
            .WithOne()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(nameof(OfficeRole.Permissions))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
