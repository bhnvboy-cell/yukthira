CREATE TABLE IF NOT EXISTS yuktira_core."Currencys" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "Code" text NOT NULL DEFAULT '',
    "Name" text NOT NULL DEFAULT '',
    "Symbol" text NOT NULL DEFAULT '',
    "IsBase" boolean NOT NULL DEFAULT FALSE,
    "DecimalPlaces" integer NOT NULL DEFAULT 2,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."ExchangeRates" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "FromCurrency" text NOT NULL DEFAULT '',
    "ToCurrency" text NOT NULL DEFAULT '',
    "Rate" numeric NOT NULL DEFAULT 0,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "Source" text NOT NULL DEFAULT 'Manual',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);