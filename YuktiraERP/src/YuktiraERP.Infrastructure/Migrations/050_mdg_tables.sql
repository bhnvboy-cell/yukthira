CREATE SCHEMA IF NOT EXISTS yuktira_mdg;

CREATE TABLE IF NOT EXISTS yuktira_mdg.change_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    request_number VARCHAR(30) NOT NULL,
    entity_name VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100),
    staging_payload JSONB NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Draft',
    requested_by VARCHAR(100) NOT NULL,
    approved_by VARCHAR(100),
    submitted_at TIMESTAMP,
    approved_at TIMESTAMP,
    activated_at TIMESTAMP,
    rejection_reason TEXT,
    workflow_instance_id UUID,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_mdg_tenant ON yuktira_mdg.change_requests(tenant_id);
CREATE INDEX IF NOT EXISTS idx_mdg_status ON yuktira_mdg.change_requests(status);
CREATE UNIQUE INDEX IF NOT EXISTS idx_mdg_request_number ON yuktira_mdg.change_requests(request_number, tenant_id);

CREATE TABLE IF NOT EXISTS yuktira_mdg.audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    change_request_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,
    actor VARCHAR(100) NOT NULL,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    old_value TEXT,
    new_value TEXT,
    hash_sha256 VARCHAR(64) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_mdg_audit_tenant ON yuktira_mdg.audit_logs(tenant_id);
CREATE INDEX IF NOT EXISTS idx_mdg_audit_cr ON yuktira_mdg.audit_logs(change_request_id);
