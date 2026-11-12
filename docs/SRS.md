# Software requirements baseline

## Product
NOTAIRE DESK is a Windows desktop application and local office platform for multiple independent Algerian notarial offices. It combines a Word-like editor with notarial workflow and structured data management.

## Users
Notary, office manager, contract editor, data-entry clerk and clerk. One installation administrator manages system setup. Business roles remain separately permissioned. Authorization is enforced server-side and scoped to the office and assigned project/file.

## Functional scope
1. A4 editor with ribbon, formatting, tables, numbering, headers/footers, footnotes, images, signatures, find/replace, undo/redo, autosave and print.
2. Open/save DOCX, export PDF, import legacy DOC while preserving source and converting a working copy.
3. Party/property registers with history and access controls.
4. Versioned templates with variables, conditional clauses and controlled publication.
5. Contract generation from approved templates and structured records.
6. Immutable generation-time snapshots of party/property data.
7. Contract conversion workflows with human review.
8. Bulk generation from descriptive spreadsheets with validation and exception report.
9. Validation findings grouped into errors, warnings and notes.
10. Audit trail, local backup/restore and office user management.

## Non-functional
- Arabic RTL and French LTR, including mixed-language documents.
- LAN operation, dedicated local server and independent PostgreSQL database per office.
- No cloud storage or cloud processing of contract data.
- Least privilege, encrypted LAN transport, secure secret storage and auditable administration.
- Backup/restore, crash recovery and document versioning.
- Performance measured on representative office hardware.
- DOCX compatibility claims require repeatable tests with real-world representative synthetic fixtures.

## Exclusions
No automated legal opinion, autonomous notarial approval or public cloud tenancy for office documents. Initial shell is not production-ready.
