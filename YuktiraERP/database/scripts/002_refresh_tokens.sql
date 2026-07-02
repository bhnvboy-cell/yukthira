-- ============================================
-- YUKTIRA ERP SUITE - Migration 002
-- Refresh Tokens + Migration Tracking
-- ============================================

SET search_path TO yuktira_core;

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
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(500) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    is_revoked BOOLEAN DEFAULT FALSE,
    replaced_by_token VARCHAR(500) DEFAULT '',
    device_info VARCHAR(500) DEFAULT '',
    ip_address VARCHAR(50) DEFAULT '',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX idx_refresh_tokens_expires ON refresh_tokens(expires_at);

-- Mark this migration as applied
INSERT INTO migrations (name) VALUES ('002_refresh_tokens') ON CONFLICT (name) DO NOTHING;
