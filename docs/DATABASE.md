# Office database model baseline

**This document describes the initial EF Core model only. No production migrations or database deployment scripts have been approved yet.**

## Isolation boundary

Each installation points to exactly one dedicated office PostgreSQL database. The connection string is resolved from trusted local server configuration on the office server; desktop clients cannot select an office database or pass a tenant identifier. There is no `tenant_id` column because the office boundary is the installation/database itself.

The initial schema uses the PostgreSQL `office` schema.

## Initial entities

| Table | Purpose |
|---|---|
| `parties` | Reusable natural-person and legal-entity names in Arabic and French |
| `properties` | Reusable real-estate/property descriptions and local registry reference |
| `contracts` | Notarial instrument reference, type, status and creator |
| `contract_parties` | Link between a contract, party and legal role |
| `contract_properties` | Link between a contract and property |
| `contract_generation_snapshots` | One immutable generation record per template version/run |
| `contract_party_snapshots` | The party fields used for a generated contract version |
| `contract_property_snapshots` | The property fields used for a generated contract version |

## Historical snapshot rule

A generated version stores copies of the party and property fields used at that moment. Reusable master records may later change, but existing snapshot rows must not be updated by application workflows. A new generation creates a new snapshot with a new generation number; old snapshots remain available for audit and reproduction.

The first domain baseline captures the Arabic/French party names and the initial property description, location, area and land-registry reference. It intentionally does not yet include national identifiers, civil-status evidence, title-deed scans or signatures. These sensitive fields require a reviewed encryption, retention, authorization and access-audit model before they are added.

## Referential integrity

Contract-to-master references use restrictive deletes. A party or property referenced by a contract must not be physically deleted as a side effect of deleting or changing a relationship. Archival and retention policies will be designed before exposing deletion operations.

## Contract workflow baseline

- Draft → UnderReview → Approved → Executed → Archived
- UnderReview may be returned to Draft before edits.
- Approval is an explicit action recording the approving notary user ID and time.
- Server-side authentication and role checks must authorize every transition. Domain state checks are not a substitute for API authorization.
- This workflow baseline does not determine legal validity or replace the notary's professional judgment.

## Still required before production

1. Complete office user, role, project and audit entities.
2. Add reviewed EF Core migrations and a local installation/bootstrap process.
3. Add a PostgreSQL integration-test service and restore-tested backup procedure.
4. Establish encryption-at-rest, document key management, retention, export and deletion controls.
5. Expand generation snapshots only to the specific fields required by reviewed templates.
6. Add concurrency control and immutable audit event storage.
