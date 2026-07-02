-- System Configuration & Number Ranges
-- Adds configuration tables for app-wide settings and auto-numbering

CREATE SCHEMA IF NOT EXISTS yuktira_admin;

CREATE TABLE IF NOT EXISTS yuktira_admin.system_config (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    category VARCHAR(100) NOT NULL DEFAULT 'General',
    key VARCHAR(200) NOT NULL,
    value TEXT NOT NULL DEFAULT '',
    description TEXT,
    is_encrypted BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(tenant_id, key)
);

CREATE TABLE IF NOT EXISTS yuktira_admin.number_range_definition (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    module VARCHAR(50) NOT NULL,
    code VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    prefix VARCHAR(20) NOT NULL DEFAULT '',
    suffix VARCHAR(20) NOT NULL DEFAULT '',
    next_number BIGINT NOT NULL DEFAULT 1,
    step INTEGER NOT NULL DEFAULT 1,
    min_length INTEGER NOT NULL DEFAULT 5,
    max_length INTEGER NOT NULL DEFAULT 20,
    reset_frequency VARCHAR(20) NOT NULL DEFAULT 'Never',
    last_reset_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(tenant_id, module, code)
);

CREATE INDEX IF NOT EXISTS idx_system_config_tenant ON yuktira_admin.system_config(tenant_id);
CREATE INDEX IF NOT EXISTS idx_system_config_category ON yuktira_admin.system_config(tenant_id, category);
CREATE INDEX IF NOT EXISTS idx_number_range_tenant ON yuktira_admin.number_range_definition(tenant_id);
CREATE INDEX IF NOT EXISTS idx_number_range_module ON yuktira_admin.number_range_definition(tenant_id, module);

-- Seed default number ranges
INSERT INTO yuktira_admin.number_range_definition (tenant_id, module, code, name, prefix, next_number, min_length) VALUES
    ('00000000-0000-0000-0000-000000000001', 'MM', 'MAT', 'Material Code', 'MAT-', 1001, 6),
    ('00000000-0000-0000-0000-000000000001', 'SD', 'CUST', 'Customer Code', 'CUST-', 1001, 6),
    ('00000000-0000-0000-0000-000000000001', 'SD', 'SO', 'Sales Order', 'SO-', 50001, 6),
    ('00000000-0000-0000-0000-000000000001', 'SD', 'INV', 'Invoice', 'INV-', 10001, 6),
    ('00000000-0000-0000-0000-000000000001', 'FI', 'VOUCHER', 'Voucher', 'V-', 10001, 6),
    ('00000000-0000-0000-0000-000000000001', 'FI', 'JOURNAL', 'Journal Entry', 'JL-', 10001, 6),
    ('00000000-0000-0000-0000-000000000001', 'PO', 'PO', 'Purchase Order', 'PO-', 10001, 6),
    ('00000000-0000-0000-0000-000000000001', 'HR', 'EMP', 'Employee Code', 'EMP-', 1001, 6)
ON CONFLICT (tenant_id, module, code) DO NOTHING;

-- Seed default system configs
INSERT INTO yuktira_admin.system_config (tenant_id, category, key, value, description) VALUES
    ('00000000-0000-0000-0000-000000000001', 'General', 'app.name', 'YuktiraERP', 'Application display name'),
    ('00000000-0000-0000-0000-000000000001', 'General', 'app.timezone', 'UTC', 'Default application timezone'),
    ('00000000-0000-0000-0000-000000000001', 'General', 'app.date_format', 'yyyy-MM-dd', 'Default date format'),
    ('00000000-0000-0000-0000-000000000001', 'Auth', 'password.min_length', '8', 'Minimum password length'),
    ('00000000-0000-0000-0000-000000000001', 'Auth', 'password.require_uppercase', 'true', 'Require uppercase in password'),
    ('00000000-0000-0000-0000-000000000001', 'Auth', 'password.require_digit', 'true', 'Require digit in password'),
    ('00000000-0000-0000-0000-000000000001', 'Auth', 'password.require_special', 'true', 'Require special char in password'),
    ('00000000-0000-0000-0000-000000000001', 'Auth', 'login.max_attempts', '5', 'Max failed attempts before lockout'),
    ('00000000-0000-0000-0000-000000000001', 'Auth', 'login.lockout_minutes', '30', 'Lockout duration in minutes'),
    ('00000000-0000-0000-0000-000000000001', 'Auth', 'session.timeout_minutes', '480', 'Session timeout in minutes'),
    ('00000000-0000-0000-0000-000000000001', 'Audit', 'audit.enabled', 'true', 'Enable audit logging'),
    ('00000000-0000-0000-0000-000000000001', 'Audit', 'audit.retention_days', '365', 'Days to retain audit logs'),
    ('00000000-0000-0000-0000-000000000001', 'Notifications', 'email.enabled', 'true', 'Enable email notifications'),
    ('00000000-0000-0000-0000-000000000001', 'Notifications', 'sms.enabled', 'false', 'Enable SMS notifications'),
    ('00000000-0000-0000-0000-000000000001', 'Notifications', 'in_app.enabled', 'true', 'Enable in-app notifications'),
    ('00000000-0000-0000-0000-000000000001', 'Backup', 'backup.enabled', 'true', 'Enable automated backups'),
    ('00000000-0000-0000-0000-000000000001', 'Backup', 'backup.retention_days', '30', 'Days to retain backups'),
    ('00000000-0000-0000-0000-000000000001', 'Backup', 'backup.schedule_cron', '0 2 * * *', 'Backup schedule (daily at 2AM)')
ON CONFLICT (tenant_id, key) DO NOTHING;
