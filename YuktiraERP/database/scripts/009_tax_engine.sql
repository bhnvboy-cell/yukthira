CREATE TABLE IF NOT EXISTS yuktira_core."TaxCodes" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "Code" text NOT NULL DEFAULT '',
    "Name" text NOT NULL DEFAULT '',
    "Rate" numeric NOT NULL DEFAULT 0,
    "TaxType" text NOT NULL DEFAULT 'GST',
    "TaxAccountCode" text NOT NULL DEFAULT '2300',
    "IsCompound" boolean NOT NULL DEFAULT FALSE,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."TaxTransactions" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "DocumentNumber" text NOT NULL DEFAULT '',
    "DocumentType" text NOT NULL DEFAULT '',
    "PartyName" text NOT NULL DEFAULT '',
    "TaxCode" text NOT NULL DEFAULT '',
    "TaxName" text NOT NULL DEFAULT '',
    "Rate" numeric NOT NULL DEFAULT 0,
    "NetAmount" numeric NOT NULL DEFAULT 0,
    "TaxAmount" numeric NOT NULL DEFAULT 0,
    "GrossAmount" numeric NOT NULL DEFAULT 0,
    "Date" timestamp with time zone NOT NULL,
    "Status" text NOT NULL DEFAULT 'Posted',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);
