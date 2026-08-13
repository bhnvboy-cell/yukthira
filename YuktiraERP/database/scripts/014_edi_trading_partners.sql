-- 014_edi_trading_partners.sql
-- EDI trading-partner profiles and acknowledgment log

CREATE TABLE IF NOT EXISTS yuktira_core."EdiTradingPartners" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "PartnerCode" text NOT NULL DEFAULT '',
    "PartnerName" text NOT NULL DEFAULT '',
    "Standard" text NOT NULL DEFAULT 'EDIFACT',
    "Version" text NOT NULL DEFAULT 'D96A',
    "SenderId" text NOT NULL DEFAULT '',
    "ReceiverId" text NOT NULL DEFAULT '',
    "SenderQualifier" text NOT NULL DEFAULT 'ZZ',
    "ReceiverQualifier" text NOT NULL DEFAULT 'ZZ',
    "TestIndicator" text NOT NULL DEFAULT 'T',
    "EndpointUrl" text NOT NULL DEFAULT '',
    "AuthType" text NOT NULL DEFAULT 'None',
    "AuthConfigJson" text NOT NULL DEFAULT '{}',
    "DocumentTypes" text NOT NULL DEFAULT 'PO,INVOICE,GRN',
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."EdiAcknowledgments" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "PartnerId" uuid NOT NULL,
    "PartnerCode" text NOT NULL DEFAULT '',
    "Direction" text NOT NULL DEFAULT 'Outbound',
    "InterchangeId" text NOT NULL DEFAULT '',
    "MessageRef" text NOT NULL DEFAULT '',
    "DocumentType" text NOT NULL DEFAULT '',
    "AckCode" text NOT NULL DEFAULT 'Accepted',
    "Description" text NOT NULL DEFAULT '',
    "RawAck" text NOT NULL DEFAULT '',
    "ReceivedAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE INDEX IF NOT EXISTS IX_EdiTradingPartners_TenantId
    ON yuktira_core."EdiTradingPartners" ("TenantId");
CREATE INDEX IF NOT EXISTS IX_EdiAcknowledgments_TenantId
    ON yuktira_core."EdiAcknowledgments" ("TenantId");
CREATE INDEX IF NOT EXISTS IX_EdiAcknowledgments_PartnerId
    ON yuktira_core."EdiAcknowledgments" ("PartnerId");