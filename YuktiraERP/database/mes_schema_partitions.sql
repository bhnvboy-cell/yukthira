-- YukthiraERP MES (MESA-11) PostgreSQL 18 Schema & Partitioning Setup
-- Aligns with Blueprint Section 1.1 & 1.5

CREATE SCHEMA IF NOT EXISTS yuktira_mes;

-- Hot telemetry table: partitioned daily by ts with BRIN index
CREATE TABLE IF NOT EXISTS yuktira_mes.mes_tag_value_raw (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "PlantCode" varchar(32) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "CreatedBy" varchar(128) NOT NULL,
    "RowVersion" bigint NOT NULL,
    "SourceId" varchar(64) NOT NULL,
    "TagId" uuid NOT NULL,
    "Ts" timestamptz NOT NULL,
    "ValueNum" double precision NULL,
    "ValueText" varchar(256) NULL,
    "Quality" varchar(16) NOT NULL,
    "Seq" bigint NOT NULL,
    "MessageUuid" uuid NOT NULL,
    PRIMARY KEY ("Id", "Ts")
) PARTITION BY RANGE ("Ts");

CREATE INDEX IF NOT EXISTS ix_mes_tag_value_raw_ts_brin
    ON yuktira_mes.mes_tag_value_raw USING brin ("Ts");

CREATE UNIQUE INDEX IF NOT EXISTS ix_mes_tag_value_raw_idemp
    ON yuktira_mes.mes_tag_value_raw ("TenantId", "SourceId", "MessageUuid", "Ts");

-- Resource state log: partitioned by StartedAt
CREATE TABLE IF NOT EXISTS yuktira_mes.mes_resource_state_log (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "PlantCode" varchar(32) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "CreatedBy" varchar(128) NOT NULL,
    "RowVersion" bigint NOT NULL,
    "SourceId" varchar(64) NOT NULL,
    "ResourceId" uuid NOT NULL,
    "FromState" varchar(32) NOT NULL,
    "ToState" varchar(32) NOT NULL,
    "ReasonCode" varchar(64) NULL,
    "ChangeSource" varchar(16) NOT NULL,
    "StartedAt" timestamptz NOT NULL,
    "EndedAt" timestamptz NULL,
    "DurationSeconds" double precision NOT NULL,
    PRIMARY KEY ("Id", "StartedAt")
) PARTITION BY RANGE ("StartedAt");

CREATE INDEX IF NOT EXISTS ix_mes_resource_state_log_started_brin
    ON yuktira_mes.mes_resource_state_log USING brin ("StartedAt");
