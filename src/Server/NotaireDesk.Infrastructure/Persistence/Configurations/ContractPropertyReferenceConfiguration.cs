using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotaireDesk.Domain.Contracts;
using NotaireDesk.Domain.Properties;

namespace NotaireDesk.Infrastructure.Persistence.Configurations;

public sealed class ContractPropertyReferenceConfiguration : IEntityTypeConfiguration<ContractPropertyReference>
{
    public void Configure(EntityTypeBuilder<ContractPropertyReference> builder)
    {
        builder.ToTable("contract_properties");
        builder.Property<Guid>("ContractId");
        builder.HasKey("ContractId", nameof(ContractPropertyReference.PropertyId));
        builder.Property(x => x.PropertyId).IsRequired();
        builder.HasOne<PropertyRecord>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
    }
}
