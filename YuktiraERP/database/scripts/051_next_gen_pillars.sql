-- =============================================================================
-- 051_next_gen_pillars.sql
-- v2.3.0 next-generation architectural pillars:
--   (1) Self-healing reconciliation audit storage (SHA-256 hash chain)
--   (2) Event-sourced financial posting outbox (no row locks on close)
--   (4) Product carbon footprint tracker (Scope 1/2/3 factors + ledger)
--   (4) Real-time mass balance & yield loss results
--
-- New schemas:
--   yuktira_sx.sx_audit             - autonomous self-healing actions with
--       PreviousHash/CurrentHash SHA-256 chain (columns follow
--       YuktiraERP.Infrastructure.Data.Entities.SxAuditEntity).
--   yuktira_fi.financial_events     - append-only event stream outbox for
--       Universal Journal postings (FinancialEventEntity).
--   yuktira_esg.emission_factors    - kg CO2e per unit factors (EmissionFactorEntity).
--   yuktira_esg.emission_logs       - computed emissions ledger (EmissionLogEntity).
--   yuktira_pp.mass_balance_results - dry-substance mass balance snapshots
--       from process order confirmations (MassBalanceResultEntity).
--
-- Idempotent: safe to run more than once. Applied automatically by
-- DataSeeder.ApplyPendingMigrationsAsync (ordered file name 051_*).
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS yuktira_sx;
CREATE SCHEMA IF NOT EXISTS yuktira_fi;
CREATE SCHEMA IF NOT EXISTS yuktira_esg;
CREATE SCHEMA IF NOT EXISTS yuktira_pp;

CREATE TABLE IF NOT EXISTS yuktira_sx.sx_audit (
    "Id"             uuid PRIMARY KEY,
    "TenantId"       uuid NOT NULL,
    "SequenceNumber" bigint NOT NULL,
    "ActionCategory" text NOT NULL DEFAULT '',
    "TargetEntity"   text NOT NULL DEFAULT '',
    "TargetId"       text NOT NULL DEFAULT '',
    "Summary"        text NOT NULL DEFAULT '',
    "DeltaAmount"    numeric(18,4) NOT NULL DEFAULT 0,
    "Currency"       text NOT NULL DEFAULT 'INR',
    "Status"         text NOT NULL DEFAULT 'Applied',
    "UserId"         text NOT NULL DEFAULT 'system',
    "PreviousHash"   text NOT NULL DEFAULT '',
    "CurrentHash"    text NOT NULL DEFAULT '',
    "Details"        text NOT NULL DEFAULT '{}',
    "CreatedAt"      timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt"      timestamp NULL
);
CREATE INDEX IF NOT EXISTS ix_sx_audit_tenant ON yuktira_sx.sx_audit ("TenantId");
CREATE UNIQUE INDEX IF NOT EXISTS ux_sx_audit_seq ON yuktira_sx.sx_audit ("TenantId", "SequenceNumber");

CREATE TABLE IF NOT EXISTS yuktira_fi.financial_events (
    "Id"             uuid PRIMARY KEY,
    "TenantId"       uuid NOT NULL,
    "StreamId"       uuid NOT NULL,
    "StreamType"     text NOT NULL DEFAULT 'UniversalJournal',
    "Sequence"       bigint NOT NULL,
    "EventType"      text NOT NULL DEFAULT '',
    "Payload"        text NOT NULL DEFAULT '{}',
    "CorrelationId"  text NOT NULL DEFAULT '',
    "Status"         text NOT NULL DEFAULT 'Pending',
    "PreviousHash"   text NOT NULL DEFAULT '',
    "Hash"           text NOT NULL DEFAULT '',
    "AppliedAt"      timestamp NULL,
    "Error"          text NOT NULL DEFAULT '',
    "CreatedAt"      timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt"      timestamp NULL
);
CREATE INDEX IF NOT EXISTS ix_financial_events_tenant ON yuktira_fi.financial_events ("TenantId");
CREATE INDEX IF NOT EXISTS ix_financial_events_stream ON yuktira_fi.financial_events ("StreamId", "Sequence");
CREATE INDEX IF NOT EXISTS ix_financial_events_status ON yuktira_fi.financial_events ("Status");

CREATE TABLE IF NOT EXISTS yuktira_esg.emission_factors (
    "Id"             uuid PRIMARY KEY,
    "TenantId"       uuid NOT NULL,
    "Scope"          integer NOT NULL DEFAULT 3,
    "SourceType"     text NOT NULL DEFAULT '',
    "MaterialCode"   text NOT NULL DEFAULT '',
    "Unit"           text NOT NULL DEFAULT 'kg',
    "KgCo2ePerUnit"  numeric(18,6) NOT NULL DEFAULT 0,
    "Region"         text NOT NULL DEFAULT '',
    "IsActive"       boolean NOT NULL DEFAULT TRUE,
    "EffectiveFrom"  timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt"      timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt"      timestamp NULL
);
CREATE INDEX IF NOT EXISTS ix_emission_factors_tenant ON yuktira_esg.emission_factors ("TenantId");
CREATE INDEX IF NOT EXISTS ix_emission_factors_lookup ON yuktira_esg.emission_factors ("Scope", "SourceType", "IsActive");

CREATE TABLE IF NOT EXISTS yuktira_esg.emission_logs (
    "Id"             uuid PRIMARY KEY,
    "TenantId"       uuid NOT NULL,
    "Scope"          integer NOT NULL DEFAULT 3,
    "SourceType"     text NOT NULL DEFAULT '',
    "ReferenceType"  text NOT NULL DEFAULT '',
    "ReferenceId"    text NOT NULL DEFAULT '',
    "MaterialCode"   text NOT NULL DEFAULT '',
    "Quantity"       numeric(18,4) NOT NULL DEFAULT 0,
    "Unit"           text NOT NULL DEFAULT 'kg',
    "KgCo2e"         numeric(18,6) NOT NULL DEFAULT 0,
    "KgCo2ePerUnit"  numeric(18,6) NOT NULL DEFAULT 0,
    "Period"         text NOT NULL DEFAULT '',
    "ComputedAt"     timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt"      timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt"      timestamp NULL
);
CREATE INDEX IF NOT EXISTS ix_emission_logs_tenant ON yuktira_esg.emission_logs ("TenantId");
CREATE INDEX IF NOT EXISTS ix_emission_logs_period ON yuktira_esg.emission_logs ("Period", "Scope");

CREATE TABLE IF NOT EXISTS yuktira_pp.mass_balance_results (
    "Id"                  uuid PRIMARY KEY,
    "TenantId"            uuid NOT NULL,
    "ProductionOrderId"   uuid NOT NULL,
    "OrderNumber"         text NOT NULL DEFAULT '',
    "TotalInputKg"        numeric(18,4) NOT NULL DEFAULT 0,
    "TotalOutputKg"       numeric(18,4) NOT NULL DEFAULT 0,
    "TotalCoProductKg"    numeric(18,4) NOT NULL DEFAULT 0,
    "YieldLossKg"         numeric(18,4) NOT NULL DEFAULT 0,
    "YieldLossPct"        numeric(9,4) NOT NULL DEFAULT 0,
    "DrySubstanceInputKg" numeric(18,4) NOT NULL DEFAULT 0,
    "DrySubstanceOutputKg" numeric(18,4) NOT NULL DEFAULT 0,
    "DrySubstanceLossKg"  numeric(18,4) NOT NULL DEFAULT 0,
    "ThresholdPct"        numeric(9,4) NOT NULL DEFAULT 2,
    "Status"              text NOT NULL DEFAULT 'WithinTolerance',
    "BreakdownJson"       text NOT NULL DEFAULT '{}',
    "CalculatedAt"        timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt"           timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt"           timestamp NULL
);
CREATE INDEX IF NOT EXISTS ix_mass_balance_tenant ON yuktira_pp.mass_balance_results ("TenantId");
CREATE INDEX IF NOT EXISTS ix_mass_balance_order ON yuktira_pp.mass_balance_results ("ProductionOrderId");

-- Seed default emission factors (kg CO2e per unit; neutral, region-agnostic)
INSERT INTO yuktira_esg.emission_factors
    ("Id", "TenantId", "Scope", "SourceType", "MaterialCode", "Unit", "KgCo2ePerUnit", "Region", "IsActive", "EffectiveFrom")
SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 1, 'Process', '', 'kg', 0.850000, 'DEFAULT', TRUE, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM yuktira_esg.emission_factors WHERE "SourceType" = 'Process' AND "Region" = 'DEFAULT');

INSERT INTO yuktira_esg.emission_factors
    ("Id", "TenantId", "Scope", "SourceType", "MaterialCode", "Unit", "KgCo2ePerUnit", "Region", "IsActive", "EffectiveFrom")
SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 2, 'Energy', '', 'kWh', 0.420000, 'DEFAULT', TRUE, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM yuktira_esg.emission_factors WHERE "SourceType" = 'Energy' AND "Region" = 'DEFAULT');

INSERT INTO yuktira_esg.emission_factors
    ("Id", "TenantId", "Scope", "SourceType", "MaterialCode", "Unit", "KgCo2ePerUnit", "Region", "IsActive", "EffectiveFrom")
SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 3, 'Freight', '', 'ton-km', 0.062000, 'DEFAULT', TRUE, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM yuktira_esg.emission_factors WHERE "SourceType" = 'Freight' AND "Region" = 'DEFAULT');

INSERT INTO yuktira_esg.emission_factors
    ("Id", "TenantId", "Scope", "SourceType", "MaterialCode", "Unit", "KgCo2ePerUnit", "Region", "IsActive", "EffectiveFrom")
SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 3, 'Material', '', 'kg', 1.200000, 'DEFAULT', TRUE, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM yuktira_esg.emission_factors WHERE "SourceType" = 'Material' AND "Region" = 'DEFAULT');
