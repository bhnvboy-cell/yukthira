-- ============================================
-- YUKTIRA ERP SUITE - Migration 007
-- Security hardening
-- 1) Bind refresh tokens to their tenant
-- ============================================

SET search_path TO yuktira_core;

DO $$
BEGIN
    IF to_regclass('yuktira_core."RefreshTokens"') IS NOT NULL THEN
        ALTER TABLE yuktira_core."RefreshTokens" ADD COLUMN IF NOT EXISTS "TenantId" UUID;
    ELSIF to_regclass('yuktira_core.refresh_tokens') IS NOT NULL THEN
        ALTER TABLE yuktira_core.refresh_tokens ADD COLUMN IF NOT EXISTS tenant_id UUID;
    END IF;
END $$;

-- Record the migration only if the (lowercase, script-managed) tracking table exists.
DO $$
BEGIN
    IF to_regclass('yuktira_core.migrations') IS NOT NULL THEN
        INSERT INTO yuktira_core.migrations (name) VALUES ('007_security_hardening')
        ON CONFLICT (name) DO NOTHING;
    END IF;
END $$;
