# NOTAIRE DESK

Windows desktop workspace for Algerian notarial offices. Arabic RTL / French LTR. Each office has its own dedicated on-premises server, PostgreSQL database and local document storage.

> Infrastructure and proof-of-concept only. Not for real client files or production use.

## Stack
- WPF / C# / .NET 10 Windows client
- ASP.NET Core LAN API
- Independent PostgreSQL database per office
- Local-only contract and document storage
- Isolated synthetic-data web preview

See [Architecture](docs/ARCHITECTURE.md), [Requirements](docs/SRS.md), [Development](docs/DEVELOPMENT.md), and the [Document Engine POC](docs/DOCUMENT-ENGINE-POC.md).

## Build
Requires .NET 10 SDK; WPF execution requires Windows.
```powershell
dotnet restore NOTAIRE-DESK.sln
dotnet build NOTAIRE-DESK.sln -c Release
dotnet test NOTAIRE-DESK.sln -c Release
dotnet run --project src/Server/NotaireDesk.Api
```

CI builds Windows x64 and web preview artifacts on each development push and pull request. Vercel PR deployment requires VERCEL_TOKEN, VERCEL_ORG_ID and VERCEL_PROJECT_ID secrets plus VERCEL_PREVIEW_ENABLED=true.

Never commit contracts, identities, signatures, credentials, database dumps or production backups.
