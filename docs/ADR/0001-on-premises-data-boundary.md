# ADR 0001: On-premises data boundary

- Status: Accepted
- Date: 2026-11-12

Each office operates an independent dedicated server, PostgreSQL database and local document store. Desktop clients communicate with that server over the office LAN through ASP.NET Core. Contract data is never persisted to cloud storage.

Consequences: office installation and backup are managed locally; no shared multi-office database or cloud tenant layer; web preview is isolated and synthetic-only; remote support requires a separately approved auditable mechanism and must not silently copy documents off-site.
