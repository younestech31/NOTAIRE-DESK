using Microsoft.EntityFrameworkCore;
using NotaireDesk.Domain.Contracts;
using NotaireDesk.Domain.Parties;
using NotaireDesk.Domain.Properties;

namespace NotaireDesk.Infrastructure.Persistence;

/// <summary>
/// One context per dedicated office server/database. The connection string is
/// loaded from trusted local server configuration; clients never submit an office id.
/// </summary>
public sealed class OfficeDbContext(DbContextOptions<OfficeDbContext> options) : DbContext(options)
{
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<PropertyRecord> Properties => Set<PropertyRecord>();
    public DbSet<NotarialContract> Contracts => Set<NotarialContract>();
    public DbSet<ContractGenerationSnapshot> ContractGenerationSnapshots => Set<ContractGenerationSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("office");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OfficeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
