# Office server deployment and first-run security

NOTAIRE DESK is deployed as one dedicated on-premises server with one independent PostgreSQL database per notarial office. The Office LAN API must not be exposed to the public internet. Desktop clients must never choose an office/database or send a database connection string.

## Required configuration and database roles

Configure secrets in the Windows service account's protected environment or the institution's approved secret manager. Do not place actual values in Git, appsettings files, command-line arguments, tickets or logs.

| Setting | Purpose |
|---|---|
| `ConnectionStrings__OfficeDatabase` | Local office PostgreSQL connection. Use the migration-owner account only for bootstrap/migrations, then change the API service to the runtime account. |
| `Authentication__Jwt__SigningKey` | Base64 encoding of at least 32 cryptographically random bytes. |
| `Authentication__Jwt__Issuer` | Optional; defaults to `NOTAIRE-DESK.OfficeApi`. |
| `Authentication__Jwt__Audience` | Optional; defaults to `NOTAIRE-DESK.WindowsClient`. |
| `ASPNETCORE_URLS` | HTTPS listener bound to the intended office LAN interface. |

Create separate database roles using the institution's DBA procedure:

- `office_migrator`: temporary schema/migration owner; used for bootstrap and reviewed database migrations.
- `office_runtime`: restricted account used by the API while the office is operating. It must not own the database or schema and must not have DDL/role-management rights.

The bootstrap utility and API read the same environment variable name, but it must contain different connection strings in the two processes. Never run the API with the `office_migrator` connection.

Example connection string shape (replace placeholders locally):
```text
Host=127.0.0.1;Port=5432;Database=office_database;Username=office_runtime;Password=<stored-outside-repository>;SSL Mode=Prefer;Timeout=15;Command Timeout=30;Pooling=true
```

Use a dedicated least-privilege PostgreSQL login, not the PostgreSQL superuser, for API traffic. Keep port 5432 inaccessible from client workstations; only the API host should connect to PostgreSQL. Use encrypted database transport when PostgreSQL is reached over a network.

Generate a JWT signing key on the server (PowerShell):
```powershell
$key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
# Store $key using the approved protected secret mechanism; do not print it to shared logs.
```

The API deliberately fails startup if the database connection string or JWT signing key is missing or invalid. There is no development fallback key or default database password.

## Initial database and administrator

1. Install a supported PostgreSQL server and create one empty database for this office. Create the `office_migrator` and `office_runtime` roles under the database administrator's procedure. Keep PostgreSQL local to the office server.
2. Extract the complete `notaire-desk-office-server-x64` GitHub Actions artifact to a protected folder on the office server. The extracted package contains the API publish output, the bootstrap executable, and the PowerShell scripts under `deployment/Server`.
3. In the local PowerShell session, temporarily set `ConnectionStrings__OfficeDatabase` to the `office_migrator` connection. Set the JWT key in the API service environment separately.
4. From the extracted package root, run:
   ```powershell
   ./deployment/Server/bootstrap-admin.ps1
   ```
5. The bootstrap tool applies the EF migration embedded in the published infrastructure assembly, seeds the built-in role catalog, and creates the first Office Administrator only if the database has no users. The password is entered with console masking and only a password hash is stored. The utility refuses to create another initial administrator once any user exists.
6. As the database administrator, run `deployment/Server/office-runtime-grants.sql` after the migration. It grants the runtime role access to the office schema and ordinary table operations; the database trigger rejects mutation of audit rows.
7. Change the Windows API service's `ConnectionStrings__OfficeDatabase` to the `office_runtime` connection string, then start/restart the API service. Do not leave the migration-owner connection configured in the running API.
8. Configure and validate the HTTPS certificate and firewall rules before allowing desktop clients to connect. Keep the bootstrap executable out of the normal API service startup path.

Do not use `/health/live` as a database readiness check. It only confirms that the web process is alive.

## Authentication and authorization baseline

- Passwords use ASP.NET Core Identity's password hasher at an explicit 310,000-iteration setting. Plaintext credentials are never persisted or audited.
- Passwords must be 14–128 characters. Five failed login attempts trigger a 15-minute lockout; login and refresh endpoints are rate-limited by client IP.
- Access tokens are signed HMAC-SHA256 JWTs with a 15-minute lifetime. Opaque refresh tokens last 14 days; only SHA-256 hashes are stored, and each refresh rotates the session.
- Every protected request verifies that the persisted session and office account remain active. Logout, password reset, role change and account disable revoke sessions.
- Permission claims are derived from the user's role assignments. Sensitive endpoints enforce named permission policies.
- Built-in roles: Office Administrator, Notary, Office Clerk, Contract Editor, Data Entry and Audit Viewer.
- The last active Office Administrator cannot be disabled or stripped of the administrator role through the user API.
- No public user-registration endpoint is exposed.

## Audit log

Authentication successes/failures, user creation, password resets, role changes and account disablement are audited. The EF Core context rejects attempted audit-row updates/deletes. The initial PostgreSQL migration installs database triggers that reject `UPDATE`, `DELETE` and `TRUNCATE` on `office.audit_events`, including direct SQL issued by the normal API database role.

Never place passwords, access/refresh tokens, national identifiers, deed text, signatures or request bodies in audit metadata. For stronger tamper resistance, copy audit events to a separately controlled append-only/offline archive under the office's retention policy. Triggers do not protect against a PostgreSQL superuser or host administrator.

## EF Core migrations

The CI pipeline scaffolds the initial `InitialOfficeDatabase` migration from the checked-in EF model, adds the audit immutability triggers, rebuilds the infrastructure assembly and packages both the C# migration and SQL output as `notaire-desk-office-database-migrations`. Review this migration package and its `office-schema.sql` against a disposable PostgreSQL database before applying it to a real office database.

The bootstrap executable used in the server package contains the reviewed migration assembly and applies migrations only when the operator runs the local bootstrap process with the migration-owner connection. The API does not apply migrations during normal startup.

For later schema changes, generate and review a new EF migration, inspect the SQL, back up the database, and test restoration before applying it. Never run automatic schema migrations from the API process.

## LAN and TLS

Bind the API only to the intended office LAN address and configure an HTTPS certificate trusted by Windows desktop clients. Never disable certificate validation. Restrict inbound firewall access to the API HTTPS port and authorized office subnets. Keep the PostgreSQL port private to the server.

The current server package provides the API and first-admin bootstrap utility. A production Windows service installer, certificate provisioning/renewal, encrypted backup/restore tool and restore-tested recovery procedure remain deployment gates.
