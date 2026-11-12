using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotaireDesk.Domain.Security;
using NotaireDesk.Infrastructure.Persistence;
using NotaireDesk.Infrastructure.Security;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__OfficeDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Set ConnectionStrings__OfficeDatabase in the server's protected environment before bootstrapping.");
    return 2;
}

if (connectionString.Contains("migration-design-only", StringComparison.Ordinal))
{
    Console.Error.WriteLine("The migration design-time placeholder cannot be used for administrator bootstrap.");
    return 2;
}

await using var db = new OfficeDbContextFactory().CreateDbContext([]);
await db.Database.MigrateAsync();

var seeder = new OfficeSecuritySeeder(db);
await seeder.SeedRolesAsync();

if (await db.Users.AnyAsync())
{
    Console.Error.WriteLine("An office user already exists. Initial administrator bootstrap is permitted only on an empty office database.");
    return 3;
}

Console.WriteLine("NOTAIRE DESK — First office administrator");
Console.Write("Username (3–80 characters, no spaces): ");
var username = Console.ReadLine()?.Trim();
Console.Write("Arabic display name: ");
var nameAr = Console.ReadLine()?.Trim();
Console.Write("French display name (optional): ");
var nameFr = Console.ReadLine()?.Trim();

if (string.IsNullOrWhiteSpace(username) || username.Length is < 3 or > 80 ||
    username.Any(char.IsWhiteSpace) || string.IsNullOrWhiteSpace(nameAr))
{
    Console.Error.WriteLine("The username or Arabic display name is invalid.");
    return 4;
}

Console.WriteLine("Password must contain 14–128 characters.");
var password = ReadMaskedPassword("Password: ");
var confirmation = ReadMaskedPassword("Confirm password: ");
if (!string.Equals(password, confirmation, StringComparison.Ordinal))
{
    Console.Error.WriteLine("The passwords do not match.");
    return 5;
}

if (password.Length < 14 || password.Length > 128 || password.Contains('\0'))
{
    Console.Error.WriteLine("Password does not meet the required length.");
    return 6;
}

var adminRole = await db.Roles.SingleAsync(x => x.Code == SystemRoleCodes.OfficeAdministrator);
var now = DateTimeOffset.UtcNow;
var user = OfficeUser.Create(username, nameAr, nameFr, "placeholder", now);
var hasher = new PasswordHasher<OfficeUser>(
    Options.Create(new PasswordHasherOptions { IterationCount = 310_000 }));
user.ReplacePasswordHash(hasher.HashPassword(user, password), now);
user.ReplaceRoles([adminRole.Id]);

db.Users.Add(user);
db.AuditEvents.Add(AuditEvent.Create(
    user.Id,
    user.Username,
    "bootstrap.office_administrator.created",
    "OfficeUser",
    user.Id.ToString("D"),
    AuditOutcome.Succeeded,
    now,
    safeMetadata: "created_by_local_bootstrap_utility"));

await db.SaveChangesAsync();
Console.WriteLine("Initial office administrator created. Remove the bootstrap utility from the server's normal operating path.");
return 0;

static string ReadMaskedPassword(string prompt)
{
    Console.Write(prompt);
    var value = new System.Text.StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return value.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (value.Length > 0)
            {
                value.Length--;
                Console.Write("\b \b");
            }
            continue;
        }

        if (!char.IsControl(key.KeyChar) && value.Length < 128)
        {
            value.Append(key.KeyChar);
            Console.Write('*');
        }
    }
}
