-- Integration Queue Extensions
-- Adds tables for outbound integration queue and dead letter handling

CREATE SCHEMA IF NOT EXISTS yuktira_integration;

-- Integration Queue table
CREATE TABLE IF NOT EXISTS yuktira_integration.integration_queue (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    message_type VARCHAR(100) NOT NULL DEFAULT '',
    payload TEXT NOT NULL DEFAULT '{}',
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    retry_count INT NOT NULL DEFAULT 0,
    max_retries INT NOT NULL DEFAULT 3,
    last_error TEXT NOT NULL DEFAULT '',
    next_retry_at TIMESTAMP WITH TIME ZONE,
    direction VARCHAR(20) NOT NULL DEFAULT 'Outbound',
    target_system VARCHAR(500) NOT NULL DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Integration Dead Letter table
CREATE TABLE IF NOT EXISTS yuktira_integration.integration_dead_letter (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    original_queue_id UUID NOT NULL,
    message_type VARCHAR(100) NOT NULL DEFAULT '',
    payload TEXT NOT NULL DEFAULT '{}',
    error_message TEXT NOT NULL DEFAULT '',
    retry_attempts INT NOT NULL DEFAULT 0,
    failed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_integration_queue_tenant ON yuktira_integration.integration_queue(tenant_id);
CREATE INDEX IF NOT EXISTS idx_integration_queue_status ON yuktira_integration.integration_queue(tenant_id, status);
CREATE INDEX IF NOT EXISTS idx_integration_queue_retry ON yuktira_integration.integration_queue(tenant_id, next_retry_at);
CREATE INDEX IF NOT EXISTS idx_integration_dead_letter_tenant ON yuktira_integration.integration_dead_letter(tenant_id);
