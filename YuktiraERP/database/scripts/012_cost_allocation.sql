CREATE TABLE IF NOT EXISTS yuktira_core."CostAllocationRules" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "Name" text NOT NULL DEFAULT '',
    "CostElementCode" text NOT NULL DEFAULT '',
    "AllocationType" text NOT NULL DEFAULT 'Proportional',
    "Basis" text NOT NULL DEFAULT 'Headcount',
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."CostAllocationRuns" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "Period" text NOT NULL DEFAULT '',
    "TotalAllocated" numeric NOT NULL DEFAULT 0,
    "Status" text NOT NULL DEFAULT 'Completed',
    "RunAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."CostAllocationDetails" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "RunId" uuid NOT NULL,
    "CostCenterCode" text NOT NULL DEFAULT '',
    "CostCenterName" text NOT NULL DEFAULT '',
    "CostElementCode" text NOT NULL DEFAULT '',
    "Amount" numeric NOT NULL DEFAULT 0,
    "SharePercent" numeric NOT NULL DEFAULT 0,
    "Basis" text NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);