CREATE TABLE IF NOT EXISTS yuktira_core."StockMovements" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "DocumentNumber" text NOT NULL DEFAULT '',
    "MaterialName" text NOT NULL DEFAULT '',
    "MovementType" text NOT NULL DEFAULT '',
    "Quantity" numeric NOT NULL DEFAULT 0,
    "StockBefore" numeric NOT NULL DEFAULT 0,
    "StockAfter" numeric NOT NULL DEFAULT 0,
    "Reference" text NOT NULL DEFAULT '',
    "Status" text NOT NULL DEFAULT 'Posted',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."FiscalPeriods" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "Period" text NOT NULL DEFAULT '',
    "FiscalYear" text NOT NULL DEFAULT '',
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "Status" text NOT NULL DEFAULT 'Open',
    "ClosedAt" timestamp with time zone NULL,
    "ClosedBy" text NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."BankReconciliations" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "AccountCode" text NOT NULL DEFAULT '',
    "AccountName" text NOT NULL DEFAULT '',
    "StatementDate" timestamp with time zone NOT NULL,
    "StatementBalance" numeric NOT NULL DEFAULT 0,
    "LedgerBalance" numeric NOT NULL DEFAULT 0,
    "Difference" numeric NOT NULL DEFAULT 0,
    "Status" text NOT NULL DEFAULT 'Draft',
    "Notes" text NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."Payments" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "PaymentNumber" text NOT NULL DEFAULT '',
    "Date" timestamp with time zone NOT NULL,
    "PartyName" text NOT NULL DEFAULT '',
    "Type" text NOT NULL DEFAULT 'Payment',
    "Reference" text NOT NULL DEFAULT '',
    "Amount" numeric NOT NULL DEFAULT 0,
    "Method" text NOT NULL DEFAULT 'Bank Transfer',
    "Status" text NOT NULL DEFAULT 'Posted',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."DepreciationSchedules" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "AssetId" uuid NOT NULL,
    "AssetCode" text NOT NULL DEFAULT '',
    "AssetName" text NOT NULL DEFAULT '',
    "Period" text NOT NULL DEFAULT '',
    "DepreciationAmount" numeric NOT NULL DEFAULT 0,
    "AccumulatedDepreciation" numeric NOT NULL DEFAULT 0,
    "BookValue" numeric NOT NULL DEFAULT 0,
    "Status" text NOT NULL DEFAULT 'Posted',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."ApprovalSteps" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "ApprovalRequestId" uuid NOT NULL,
    "Level" integer NOT NULL DEFAULT 0,
    "ApproverName" text NOT NULL DEFAULT '',
    "Status" text NOT NULL DEFAULT 'Pending',
    "Comments" text NOT NULL DEFAULT '',
    "ActionedAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);