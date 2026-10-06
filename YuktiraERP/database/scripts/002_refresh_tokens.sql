-- ============================================
-- YUKTIRA ERP SUITE - Migration 002
-- Refresh Tokens + Migration Tracking
-- ============================================

SET search_path TO yuktira_core, public;

-- Migration tracking table (auto-created by EF Core on first run)
CREATE TABLE IF NOT EXISTS migrations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL UNIQUE,
    applied_at TIMESTAMPTZ DEFAULT NOW(),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Seed migration 001 as already applied
INSERT INTO migrations (name) VALUES ('001_core_schema') ON CONFLICT (name) DO NOTHING;

-- Refresh tokens table
CREATE TABLE IF NOT EXISTS refresh_tokens (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId" UUID NOT NULL REFERENCES users("Id") ON DELETE CASCADE,
    "Token" VARCHAR(500) NOT NULL UNIQUE,
    "ExpiresAt" TIMESTAMPTZ NOT NULL,
    "IsRevoked" BOOLEAN DEFAULT FALSE,
    "ReplacedByToken" VARCHAR(500) DEFAULT '',
    "DeviceInfo" VARCHAR(500) DEFAULT '',
    "IpAddress" VARCHAR(50) DEFAULT '',
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens("UserId");
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens("Token");
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires ON refresh_tokens("ExpiresAt");

-- Mark this migration as applied
INSERT INTO migrations (name) VALUES ('002_refresh_tokens') ON CONFLICT (name) DO NOTHING;
