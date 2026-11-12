using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Contracts;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class NotarialContractConfiguration : IEntityTypeConfiguration<NotarialContract>
{
    public void Configure(EntityTypeBuilder<NotarialContract> builder)
    {
        builder.ToTable("contracts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Reference).HasMaxLength(80).IsRequired();
        builder.Property(x => x.InstrumentType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.Reference).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasMany<ContractPartyReference>("_parties")
            .WithOne()
            .HasForeignKey("ContractId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation("_parties").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany<ContractPropertyReference>("_properties")
            .WithOne()
            .HasForeignKey("ContractId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation("_properties").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany<ContractGenerationSnapshot>("_generations")
            .WithOne()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation("_generations").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
