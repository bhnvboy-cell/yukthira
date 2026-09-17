-- Security Import & Role Management Tables
-- Migration 053: master_roles, composite_roles, derived_roles, role_tcode_assignments, security_import_batches, user_role_assignments
-- Schema: yuktira_sys

CREATE SCHEMA IF NOT EXISTS yuktira_sys;

CREATE TABLE IF NOT EXISTS yuktira_sys.master_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    role_id VARCHAR(50) NOT NULL,
    role_name VARCHAR(200) NOT NULL DEFAULT '',
    module VARCHAR(50) NOT NULL DEFAULT '',
    sub_process VARCHAR(100) NOT NULL DEFAULT '',
    catalog VARCHAR(100) NOT NULL DEFAULT '',
    space VARCHAR(100) NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT '',
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_master_roles_tenant ON yuktira_sys.master_roles(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_master_roles_roleid_tenant ON yuktira_sys.master_roles(role_id, tenant_id);
CREATE INDEX IF NOT EXISTS idx_master_roles_module ON yuktira_sys.master_roles(module);

CREATE TABLE IF NOT EXISTS yuktira_sys.composite_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    composite_role_id VARCHAR(50) NOT NULL,
    composite_role_name VARCHAR(200) NOT NULL DEFAULT '',
    module VARCHAR(50) NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT '',
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_composite_roles_tenant ON yuktira_sys.composite_roles(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_composite_roles_crid_tenant ON yuktira_sys.composite_roles(composite_role_id, tenant_id);
CREATE INDEX IF NOT EXISTS idx_composite_roles_module ON yuktira_sys.composite_roles(module);

CREATE TABLE IF NOT EXISTS yuktira_sys.derived_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    derived_role_id VARCHAR(50) NOT NULL,
    derived_role_name VARCHAR(200) NOT NULL DEFAULT '',
    composite_role_id VARCHAR(50) NOT NULL DEFAULT '',
    master_role_id VARCHAR(50) NOT NULL DEFAULT '',
    module VARCHAR(50) NOT NULL DEFAULT '',
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_derived_roles_tenant ON yuktira_sys.derived_roles(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_derived_roles_drid_tenant ON yuktira_sys.derived_roles(derived_role_id, tenant_id);
CREATE INDEX IF NOT EXISTS idx_derived_roles_composite ON yuktira_sys.derived_roles(composite_role_id);
CREATE INDEX IF NOT EXISTS idx_derived_roles_master ON yuktira_sys.derived_roles(master_role_id);

CREATE TABLE IF NOT EXISTS yuktira_sys.role_tcode_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    role_id VARCHAR(50) NOT NULL DEFAULT '',
    role_type VARCHAR(20) NOT NULL DEFAULT '',
    transaction_code VARCHAR(50) NOT NULL DEFAULT '',
    app_id VARCHAR(50) NOT NULL DEFAULT '',
    app_description VARCHAR(500) NOT NULL DEFAULT '',
    has_access BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_role_tcode_tenant ON yuktira_sys.role_tcode_assignments(tenant_id);
CREATE INDEX IF NOT EXISTS idx_role_tcode_role ON yuktira_sys.role_tcode_assignments(role_id, role_type, tenant_id);
CREATE INDEX IF NOT EXISTS idx_role_tcode_tcode ON yuktira_sys.role_tcode_assignments(transaction_code, tenant_id);

CREATE TABLE IF NOT EXISTS yuktira_sys.security_import_batches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    batch_number VARCHAR(50) NOT NULL,
    file_name VARCHAR(200) NOT NULL DEFAULT '',
    total_rows INT NOT NULL DEFAULT 0,
    imported_rows INT NOT NULL DEFAULT 0,
    error_rows INT NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    imported_by VARCHAR(50) NOT NULL DEFAULT '',
    imported_at TIMESTAMP,
    error_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_import_batches_tenant ON yuktira_sys.security_import_batches(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_import_batches_batch_tenant ON yuktira_sys.security_import_batches(batch_number, tenant_id);

CREATE TABLE IF NOT EXISTS yuktira_sys.user_role_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    user_id VARCHAR(50) NOT NULL DEFAULT '',
    composite_role_id VARCHAR(50) NOT NULL DEFAULT '',
    assigned_by VARCHAR(50) NOT NULL DEFAULT '',
    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_user_role_assign_tenant ON yuktira_sys.user_role_assignments(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_role_assign_unique ON yuktira_sys.user_role_assignments(user_id, composite_role_id, tenant_id);
CREATE INDEX IF NOT EXISTS idx_user_role_assign_composite ON yuktira_sys.user_role_assignments(composite_role_id);
