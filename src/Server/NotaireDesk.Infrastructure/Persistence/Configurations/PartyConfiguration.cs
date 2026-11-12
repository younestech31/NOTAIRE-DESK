using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Parties;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.ToTable("parties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisplayNameAr).HasMaxLength(300).IsRequired();
        builder.Property(x => x.DisplayNameFr).HasMaxLength(300);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.DisplayNameAr);
        builder.HasIndex(x => x.DisplayNameFr);
    }
}
