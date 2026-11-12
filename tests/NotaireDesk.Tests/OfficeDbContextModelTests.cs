using Microsoft.EntityFrameworkCore;
using NotaireDesk.Domain.Contracts;
using NotaireDesk.Domain.Parties;
using NotaireDesk.Domain.Properties;
using NotaireDesk.Domain.Security;
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
        AssertTable<OfficeUser>(model, "office_users");
        AssertTable<OfficeRole>(model, "office_roles");
        AssertTable<OfficeUserRole>(model, "office_user_roles");
        AssertTable<OfficeRolePermission>(model, "office_role_permissions");
        AssertTable<UserSession>(model, "user_sessions");
        AssertTable<AuditEvent>(model, "audit_events");
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

    [Fact]
    public void UsernameAndRefreshTokenHashesAreUnique()
    {
        var options = new DbContextOptionsBuilder<OfficeDbContext>()
            .UseNpgsql("Host=localhost;Database=notaire_model_test;Username=test;Password=test")
            .Options;

        using var context = new OfficeDbContext(options);
        var user = context.Model.FindEntityType(typeof(OfficeUser));
        var session = context.Model.FindEntityType(typeof(UserSession));

        Assert.NotNull(user);
        Assert.NotNull(session);
        Assert.Contains(user!.GetIndexes(), index => index.IsUnique &&
            index.Properties.Any(p => p.Name == nameof(OfficeUser.NormalizedUsername)));
        Assert.Contains(session!.GetIndexes(), index => index.IsUnique &&
            index.Properties.Any(p => p.Name == nameof(UserSession.RefreshTokenHash)));
    }

    [Fact]
    public void AuditEventsCannotBeModifiedOrDeletedThroughTheOfficeContext()
    {
        var options = new DbContextOptionsBuilder<OfficeDbContext>()
            .UseNpgsql("Host=localhost;Database=notaire_model_test;Username=test;Password=test")
            .Options;

        using var context = new OfficeDbContext(options);
        var entry = AuditEvent.Create(
            null, null, "auth.login.failed", "Authentication", null,
            AuditOutcome.Failed, DateTimeOffset.UtcNow);
        context.Attach(entry);
        context.Entry(entry).State = EntityState.Modified;

        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

        context.Entry(entry).State = EntityState.Detached;
        context.Attach(entry);
        context.Entry(entry).State = EntityState.Deleted;
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
    }

    private static void AssertTable<TEntity>(Microsoft.EntityFrameworkCore.Metadata.IModel model, string tableName)
    {
        var entity = model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        Assert.Equal("office", entity!.GetSchema());
        Assert.Equal(tableName, entity.GetTableName());
    }
}
