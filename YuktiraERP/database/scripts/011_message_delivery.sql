CREATE TABLE IF NOT EXISTS yuktira_core."MessageDeliverys" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "Channel" text NOT NULL DEFAULT '',
    "ToAddress" text NOT NULL DEFAULT '',
    "Subject" text NOT NULL DEFAULT '',
    "Body" text NOT NULL DEFAULT '',
    "Status" text NOT NULL DEFAULT 'Sent',
    "ErrorMessage" text NOT NULL DEFAULT '',
    "Provider" text NOT NULL DEFAULT '',
    "SentAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);