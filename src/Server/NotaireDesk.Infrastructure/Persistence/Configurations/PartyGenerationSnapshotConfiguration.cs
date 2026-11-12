using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Contracts;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class PartyGenerationSnapshotConfiguration : IEntityTypeConfiguration<PartyGenerationSnapshot>
{
    public void Configure(EntityTypeBuilder<PartyGenerationSnapshot> builder)
    {
        builder.ToTable("contract_party_snapshots");
        builder.Property<Guid>("SnapshotId");
        builder.HasKey("SnapshotId", nameof(PartyGenerationSnapshot.SourcePartyId), nameof(PartyGenerationSnapshot.Role));
        builder.Property(x => x.SourcePartyId).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(300).IsRequired();
        builder.Property(x => x.NameFr).HasMaxLength(300);
    }
}
