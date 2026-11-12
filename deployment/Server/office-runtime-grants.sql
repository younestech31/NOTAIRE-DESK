-- Run this as a PostgreSQL database administrator after the office database
-- is initialized by the local migration/bootstrap account.
--
-- Create these roles separately using the institution's credential procedure.
-- Do not put passwords in this file or commit connection strings.
--   office_migrator: owns schema and applies reviewed migrations.
--   office_runtime: used only by the Office LAN API at normal runtime.
--
-- Replace office_database with the actual database name if this script is
-- executed in a maintenance session connected to a different database.

GRANT CONNECT ON DATABASE office_database TO office_runtime;
GRANT USAGE ON SCHEMA office TO office_runtime;

-- The API can read and modify business data, create sessions and append audit
-- records. Database triggers on audit_events reject UPDATE, DELETE and TRUNCATE.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA office TO office_runtime;
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA office TO office_runtime;

-- Future tables created by the migration owner will inherit the same baseline.
ALTER DEFAULT PRIVILEGES FOR ROLE office_migrator IN SCHEMA office
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO office_runtime;
ALTER DEFAULT PRIVILEGES FOR ROLE office_migrator IN SCHEMA office
    GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO office_runtime;

-- Explicitly document the required audit access. The trigger remains the
-- enforcement boundary for append-only rows even though the runtime role has
-- broader table grants for ordinary business tables.
GRANT SELECT, INSERT ON office.audit_events TO office_runtime;
