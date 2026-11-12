using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Contracts;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class ContractGenerationSnapshotConfiguration : IEntityTypeConfiguration<ContractGenerationSnapshot>
{
    public void Configure(EntityTypeBuilder<ContractGenerationSnapshot> builder)
    {
        builder.ToTable("contract_generation_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContractId).IsRequired();
        builder.Property(x => x.TemplateId).IsRequired();
        builder.Property(x => x.TemplateVersion).IsRequired();
        builder.Property(x => x.GenerationNumber).IsRequired();
        builder.Property(x => x.GeneratedBy).IsRequired();
        builder.Property(x => x.GeneratedAt).IsRequired();
        builder.HasIndex(x => new { x.ContractId, x.GenerationNumber }).IsUnique();

        builder.HasMany<PartyGenerationSnapshot>(nameof(ContractGenerationSnapshot.Parties))
            .WithOne()
            .HasForeignKey("SnapshotId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(nameof(ContractGenerationSnapshot.Parties))
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany<PropertyGenerationSnapshot>(nameof(ContractGenerationSnapshot.Properties))
            .WithOne()
            .HasForeignKey("SnapshotId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(nameof(ContractGenerationSnapshot.Properties))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
