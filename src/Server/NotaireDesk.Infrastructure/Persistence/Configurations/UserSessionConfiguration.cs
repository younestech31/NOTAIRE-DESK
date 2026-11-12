using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Security;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.RefreshTokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.RefreshTokenHash).IsUnique();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.ReplacedBySessionId);
        builder.Property(x => x.DeviceLabel).HasMaxLength(120);
        builder.Property(x => x.Version).IsConcurrencyToken().IsRequired();
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
        builder.HasOne<OfficeUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
