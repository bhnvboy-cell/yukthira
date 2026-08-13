CREATE TABLE IF NOT EXISTS yuktira_core."Languages" (
    "Id" uuid PRIMARY KEY,
    "Code" text NOT NULL DEFAULT '',
    "Name" text NOT NULL DEFAULT '',
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "IsDefault" boolean NOT NULL DEFAULT FALSE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

CREATE TABLE IF NOT EXISTS yuktira_core."Translations" (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "LanguageCode" text NOT NULL DEFAULT '',
    "Key" text NOT NULL DEFAULT '',
    "Value" text NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL
);

INSERT INTO yuktira_core."Languages" ("Id", "Code", "Name", "IsActive", "IsDefault")
VALUES
    (gen_random_uuid(), 'en', 'English', TRUE, TRUE),
    (gen_random_uuid(), 'hi', 'हिन्दी (Hindi)', TRUE, FALSE),
    (gen_random_uuid(), 'ta', 'தமிழ் (Tamil)', TRUE, FALSE),
    (gen_random_uuid(), 'te', 'తెలుగు (Telugu)', TRUE, FALSE),
    (gen_random_uuid(), 'kn', 'ಕನ್ನಡ (Kannada)', TRUE, FALSE),
    (gen_random_uuid(), 'ml', 'മലയാളം (Malayalam)', TRUE, FALSE),
    (gen_random_uuid(), 'fr', 'Français', TRUE, FALSE),
    (gen_random_uuid(), 'es', 'Español', TRUE, FALSE)
ON CONFLICT DO NOTHING;