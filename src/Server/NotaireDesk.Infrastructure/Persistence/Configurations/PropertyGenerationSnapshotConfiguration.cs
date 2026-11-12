using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Contracts;
using NotaireDesk.Domain.Properties;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class PropertyGenerationSnapshotConfiguration : IEntityTypeConfiguration<PropertyGenerationSnapshot>
{
    public void Configure(EntityTypeBuilder<PropertyGenerationSnapshot> builder)
    {
        builder.ToTable("contract_property_snapshots");
        builder.Property<Guid>("SnapshotId");
        builder.HasKey("SnapshotId", nameof(PropertyGenerationSnapshot.SourcePropertyId));
        builder.Property(x => x.SourcePropertyId).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.DescriptionAr).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.DescriptionFr).HasMaxLength(2000);
        builder.Property(x => x.Wilaya).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Commune).HasMaxLength(150).IsRequired();
        builder.Property(x => x.AreaSquareMeters).HasPrecision(18, 3);
        builder.Property(x => x.LandRegistryReference).HasMaxLength(150);
    }
}
