# Office server deployment and first-run security

This application is designed for one dedicated on-premises server and one independent PostgreSQL database per notarial office. The API must not be exposed to the public internet. Desktop clients must never send an office/tenant selector or a database connection string.

## Required server configuration

Configure these values in the Windows service account's protected environment or the institution's approved secret manager. Do not put the actual values in Git, appsettings files, command-line arguments, issue comments or logs.

| Setting | Required value |
|---|---|
| ConnectionStrings__OfficeDatabase | Connection string for the local office PostgreSQL database |
| Authentication__Jwt__SigningKey | Base64 encoding of at least 32 cryptographically random bytes |
| Authentication__Jwt__Issuer | Optional; defaults to NOTAIRE-DESK.OfficeApi |
| Authentication__Jwt__Audience | Optional; defaults to NOTAIRE-DESK.WindowsClient |
| ASPNETCORE_URLS | HTTPS listener bound to the intended office LAN interface |

Example connection-string shape (replace every placeholder locally):
```text
Host=127.0.0.1;Port=5432;Database=office_database;Username=notaire_app;Password=<stored-outside-repository>;SSL Mode=Prefer;Timeout=15;Command Timeout=30;Pooling=true
```

Use a dedicated least-privilege PostgreSQL login. Do not use the PostgreSQL superuser for application traffic. Keep PostgreSQL port 5432 inaccessible from client workstations; only the API host should connect to the database. Use encrypted database transport when PostgreSQL is reached over a network.

Generate a JWT signing key on the server (PowerShell):
```powershell
$key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
# Store $key using the approved protected secret mechanism. Never print it to shared logs.
```

The API deliberately fails startup when the database connection string or JWT key is absent or invalid. There is no development fallback key and no default database password.

## Initial database and administrator

1. Install a supported PostgreSQL server on the office server and create a separate database and restricted application login for this office.
2. Set ConnectionStrings__OfficeDatabase in the protected environment for the local administrator account.
3. Open a local PowerShell console on the office server and run:
   ```powershell
   ./deployment/Server/bootstrap-admin.ps1
   ```
4. The bootstrap utility applies checked-in EF Core migrations, seeds the built-in role catalog, and creates the first Office Administrator only when the database contains no users. The password is entered with console masking and only a password hash is stored.
5. Once the first administrator exists, the bootstrap utility refuses to create another initial administrator. Keep this utility out of the normal API service startup path.
6. Configure the server HTTPS certificate and firewall rules before allowing desktop clients to connect.

Do not use the /health/live endpoint as a database-readiness check. It only indicates that the web process is alive.

## Authentication and authorization baseline

- Passwords are hashed with ASP.NET Core Identity's password hasher at an explicit 310,000-iteration setting. Plaintext credentials are never persisted or audited.
- Passwords must be 14–128 characters. Lockout is triggered after five failed attempts for 15 minutes; login attempts are rate-limited by client IP.
- Access tokens are signed HMAC-SHA256 JWTs with a 15-minute lifetime. Opaque refresh tokens last 14 days; only SHA-256 hashes are stored and each refresh rotates the session.
- Every protected request verifies that the persisted session and office account remain active. Logout, password reset, role change and account disable revoke sessions.
- Permission claims are generated from the user's current role assignments. The API applies named permission policies to sensitive endpoints.
- The first role catalog is seeded locally: Office Administrator, Notary, Office Clerk, Contract Editor, Data Entry and Audit Viewer.
- The last active Office Administrator cannot be disabled or stripped of the administrator role through the user API.
- No public user-registration endpoint is exposed.

## Audit log

The API records authentication successes/failures, user creation, password resets, role changes and account disablement. Audit records are append-only through the EF Core context. Never place passwords, access/refresh tokens, national identifiers, deed text, signatures or request bodies in audit metadata. The database migration adds a PostgreSQL trigger so the application database role cannot update or delete audit rows through ordinary SQL statements.

For stronger tamper resistance, copy audit events to a separately controlled append-only/offline archive under the office's approved retention policy. Database triggers do not protect against a PostgreSQL superuser or server administrator.

## EF Core migrations

The EF migration project is NotaireDesk.Infrastructure; the design-time context factory uses a placeholder only for scaffolding. Applying migrations requires the real protected connection string.

After migrations are checked into the repository:
```powershell
dotnet tool restore
dotnet ef migrations list --project src/Server/NotaireDesk.Infrastructure --startup-project src/Server/NotaireDesk.Api --context OfficeDbContext
dotnet ef database update --project src/Server/NotaireDesk.Infrastructure --startup-project src/Server/NotaireDesk.Api --context OfficeDbContext
```

Review every migration and its generated SQL before applying it to an office database. Back up the database and test restoration before schema upgrades. Never run automatic migrations from the public API startup process.

## LAN and TLS

Bind the API only to the intended office LAN address and configure an HTTPS certificate trusted by the Windows desktop clients. Do not disable certificate validation in the desktop application. Restrict inbound firewall access to the API's HTTPS port and authorized office subnets. Keep the PostgreSQL port private to the server. This repository does not yet provide a production installer, Windows service registration, certificate provisioning or a backup/restore utility; those remain separate deployment gates.
