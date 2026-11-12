# Architecture baseline

## Office deployment boundary
Each notarial office has a separate dedicated on-premises server, PostgreSQL database, local document store and LAN clients. The server must not multiplex unrelated offices through a shared database. Office identity and database connection come from trusted server-side installation configuration, never a client-controlled tenant identifier.

## Components
- Windows desktop: WPF / C# / .NET 10.
- Office LAN API: ASP.NET Core.
- Domain/Application/Infrastructure layers independent of WPF and document-engine implementation.
- PostgreSQL: isolated database per office.
- Files: local server storage with access controls, backup and restore.
- Web preview: synthetic walkthrough only; no connection to office API or real data.

## Security invariants
1. No contract, identity, signature or property document stored in cloud services.
2. No public internet exposure of office API by default.
3. Authentication and role authorization enforced by API.
4. Each generated instrument stores immutable snapshots of the party/property data used.
5. Audit events record actor, time, action and affected record.
6. Backups are encrypted, local/offline-capable and restore-tested.
7. No secrets or production data in Git.

## Document-engine decision gate
Do not build a DOCX layout engine from scratch. Evaluate self-hosted ONLYOFFICE Docs against commercially licensed alternatives. Test complex Arabic RTL / French LTR documents, mixed-language paragraphs, tables, headers/footers, footnotes, page breaks, pagination, images, signatures, round-trip fidelity and PDF output. Confirm commercial licensing in writing. DOC import preserves the original and converts a working copy; fidelity is not assumed.

Validation findings are errors, warnings or notes. The system must not issue legal determinations or approve/sign instruments on behalf of the notary.
