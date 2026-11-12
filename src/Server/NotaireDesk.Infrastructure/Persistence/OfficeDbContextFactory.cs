using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NotaireDesk.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF tooling. A placeholder is permitted only when
/// scaffolding migrations; applying them requires the real local connection string.
/// </summary>
public sealed class OfficeDbContextFactory : IDesignTimeDbContextFactory<OfficeDbContext>
{
    public OfficeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__OfficeDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = "Host=localhost;Port=5432;Database=notaire_design;Username=notaire_design;Password=migration-design-only";

        var options = new DbContextOptionsBuilder<OfficeDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(OfficeDbContext).Assembly.FullName))
            .Options;

        return new OfficeDbContext(options);
    }
}
