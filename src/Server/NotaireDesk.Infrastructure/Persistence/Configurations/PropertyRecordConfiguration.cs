using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Properties;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class PropertyRecordConfiguration : IEntityTypeConfiguration<PropertyRecord>
{
    public void Configure(EntityTypeBuilder<PropertyRecord> builder)
    {
        builder.ToTable("properties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.DescriptionAr).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.DescriptionFr).HasMaxLength(2000);
        builder.Property(x => x.Wilaya).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Commune).HasMaxLength(150).IsRequired();
        builder.Property(x => x.AreaSquareMeters).HasPrecision(18, 3);
        builder.Property(x => x.LandRegistryReference).HasMaxLength(150);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => new { x.Wilaya, x.Commune });
        builder.HasIndex(x => x.LandRegistryReference);
    }
}
