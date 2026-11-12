using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Contracts;
using NotaireDesk.Domain.Parties;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class ContractPartyReferenceConfiguration : IEntityTypeConfiguration<ContractPartyReference>
{
    public void Configure(EntityTypeBuilder<ContractPartyReference> builder)
    {
        builder.ToTable("contract_parties");
        builder.Property<Guid>("ContractId");
        builder.HasKey("ContractId", nameof(ContractPartyReference.PartyId), nameof(ContractPartyReference.Role));
        builder.Property(x => x.PartyId).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
    }
}
