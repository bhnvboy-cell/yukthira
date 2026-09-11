-- ══════════════════════════════════════════════════════
-- V2.0 Roadmap Tables: Event Sourcing, EDI, ML Engine
-- YuktiraERP v2.0.0
-- ══════════════════════════════════════════════════════

-- Event Store (CQRS)
CREATE SCHEMA IF NOT EXISTS yuktira_sys;

CREATE TABLE IF NOT EXISTS yuktira_sys.domain_events (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "AggregateId" UUID NOT NULL,
    "AggregateType" INTEGER NOT NULL,
    "EventType" VARCHAR(255) NOT NULL,
    "EventData" TEXT NOT NULL,
    "Version" INTEGER NOT NULL,
    "Timestamp" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "TenantId" UUID NOT NULL,
    "UserId" VARCHAR(100),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_domain_events_aggregate ON yuktira_sys.domain_events("AggregateId");
CREATE INDEX IF NOT EXISTS idx_domain_events_tenant ON yuktira_sys.domain_events("TenantId");
CREATE INDEX IF NOT EXISTS idx_domain_events_type ON yuktira_sys.domain_events("EventType");
CREATE INDEX IF NOT EXISTS idx_domain_events_timestamp ON yuktira_sys.domain_events("Timestamp");
CREATE UNIQUE INDEX IF NOT EXISTS idx_domain_events_unique_version ON yuktira_sys.domain_events("AggregateId", "TenantId", "Version");

-- Read Model Snapshots
CREATE TABLE IF NOT EXISTS yuktira_sys.read_model_snapshots (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ModelName" VARCHAR(255) NOT NULL,
    "TenantId" UUID NOT NULL,
    "ModelData" TEXT NOT NULL,
    "Version" INTEGER NOT NULL,
    "Timestamp" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_read_model_snapshots_unique ON yuktira_sys.read_model_snapshots("ModelName", "TenantId");

-- EDI Transmissions
CREATE TABLE IF NOT EXISTS yuktira_sys.edi_transmissions (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MessageType" INTEGER NOT NULL,
    "Protocol" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "SenderId" VARCHAR(255) NOT NULL,
    "ReceiverId" VARCHAR(255) NOT NULL,
    "RawPayload" TEXT,
    "AcknowledgmentId" VARCHAR(500),
    "ErrorMessage" TEXT,
    "TenantId" UUID NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_edi_transmissions_tenant ON yuktira_sys.edi_transmissions("TenantId");
CREATE INDEX IF NOT EXISTS idx_edi_transmissions_status ON yuktira_sys.edi_transmissions("Status");

-- MDN Receipts
CREATE TABLE IF NOT EXISTS yuktira_sys.mdn_receipts (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "OriginalMessageId" VARCHAR(500) NOT NULL,
    "Status" INTEGER NOT NULL,
    "MicValue" VARCHAR(500),
    "MdnPayload" TEXT,
    "ErrorMessage" TEXT,
    "TenantId" UUID NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_mdn_receipts_message ON yuktira_sys.mdn_receipts("OriginalMessageId");
