using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Security;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class OfficeUserConfiguration : IEntityTypeConfiguration<OfficeUser>
{
    public void Configure(EntityTypeBuilder<OfficeUser> builder)
    {
        builder.ToTable("office_users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Username).HasMaxLength(80).IsRequired();
        builder.Property(x => x.NormalizedUsername).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.NormalizedUsername).IsUnique();
        builder.Property(x => x.DisplayNameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayNameFr).HasMaxLength(200);
        builder.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.FailedLoginAttempts).IsRequired();
        builder.Property(x => x.LockoutEndUtc);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasMany<OfficeUserRole>(nameof(OfficeUser.Roles))
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(nameof(OfficeUser.Roles))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
