using Microsoft.EntityFrameworkCore;
using NotaireDesk.Domain.Contracts;
using NotaireDesk.Domain.Parties;
using NotaireDesk.Domain.Properties;
using NotaireDesk.Infrastructure.Persistence;
using Xunit;

namespace NotaireDesk.Tests;

public sealed class OfficeDbContextModelTests
{
    [Fact]
    public void OfficeEntitiesAreMappedToTheOfficeSchema()
    {
        var options = new DbContextOptionsBuilder<OfficeDbContext>()
            .UseNpgsql("Host=localhost;Database=notaire_model_test;Username=test;Password=test")
            .Options;

        using var context = new OfficeDbContext(options);
        var model = context.Model;

        AssertTable<Party>(model, "parties");
        AssertTable<PropertyRecord>(model, "properties");
        AssertTable<NotarialContract>(model, "contracts");
        AssertTable<ContractGenerationSnapshot>(model, "contract_generation_snapshots");
        AssertTable<PartyGenerationSnapshot>(model, "contract_party_snapshots");
        AssertTable<PropertyGenerationSnapshot>(model, "contract_property_snapshots");
    }

    [Fact]
    public void ContractReferencesCannotCascadeDeleteMasterPartyOrPropertyRecords()
    {
        var options = new DbContextOptionsBuilder<OfficeDbContext>()
            .UseNpgsql("Host=localhost;Database=notaire_model_test;Username=test;Password=test")
            .Options;

        using var context = new OfficeDbContext(options);
        var partyReference = context.Model.FindEntityType(typeof(ContractPartyReference));
        var propertyReference = context.Model.FindEntityType(typeof(ContractPropertyReference));

        Assert.NotNull(partyReference);
        Assert.NotNull(propertyReference);

        Assert.Contains(partyReference!.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Party) &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);

        Assert.Contains(propertyReference!.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(PropertyRecord) &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);
    }

    private static void AssertTable<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model, string tableName)
    {
        var entity = model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        Assert.Equal("office", entity!.GetSchema());
        Assert.Equal(tableName, entity.GetTableName());
    }
}
