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

        builder.HasMany<ContractPartyReference>(nameof(NotarialContract.Parties))
            .WithOne()
            .HasForeignKey("ContractId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(nameof(NotarialContract.Parties))
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany<ContractPropertyReference>(nameof(NotarialContract.Properties))
            .WithOne()
            .HasForeignKey("ContractId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(nameof(NotarialContract.Properties))
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany<ContractGenerationSnapshot>(nameof(NotarialContract.Generations))
            .WithOne()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(nameof(NotarialContract.Generations))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
