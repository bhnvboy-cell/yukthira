-- ============================================
-- YUKTIRA ERP SUITE - Core Schema
-- ============================================

-- Extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Schema
CREATE SCHEMA IF NOT EXISTS yuktira_core;
CREATE SCHEMA IF NOT EXISTS yuktira_mm;
CREATE SCHEMA IF NOT EXISTS yuktira_sd;
CREATE SCHEMA IF NOT EXISTS yuktira_pp;
CREATE SCHEMA IF NOT EXISTS yuktira_qm;
CREATE SCHEMA IF NOT EXISTS yuktira_wm;
CREATE SCHEMA IF NOT EXISTS yuktira_fi;
CREATE SCHEMA IF NOT EXISTS yuktira_hr;
CREATE SCHEMA IF NOT EXISTS yuktira_crm;
CREATE SCHEMA IF NOT EXISTS yuktira_lims;
CREATE SCHEMA IF NOT EXISTS yuktira_bi;
CREATE SCHEMA IF NOT EXISTS yuktira_workflow;
CREATE SCHEMA IF NOT EXISTS yuktira_audit;
CREATE SCHEMA IF NOT EXISTS yuktira_notification;
CREATE SCHEMA IF NOT EXISTS yuktira_plugin;
CREATE SCHEMA IF NOT EXISTS yuktira_dashboard;
CREATE SCHEMA IF NOT EXISTS yuktira_customization;
CREATE SCHEMA IF NOT EXISTS yuktira_approval;
CREATE SCHEMA IF NOT EXISTS yuktira_numberrange;
CREATE SCHEMA IF NOT EXISTS yuktira_integration;
CREATE SCHEMA IF NOT EXISTS yuktira_mrp;
CREATE SCHEMA IF NOT EXISTS yuktira_transaction;

-- ============================================
-- Multi-Tenant Core Tables
-- ============================================
SET search_path TO yuktira_core;

CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','SUSPENDED')),
    subscription_plan VARCHAR(50) DEFAULT 'ENTERPRISE',
    max_users INT DEFAULT 100,
    storage_gb_limit INT DEFAULT 100,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    email VARCHAR(200),
    phone VARCHAR(50),
    full_name VARCHAR(200) NOT NULL,
    default_language VARCHAR(10) DEFAULT 'EN',
    is_super_user BOOLEAN DEFAULT FALSE,
    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','LOCKED','EXPIRED')),
    failed_login_attempts INT DEFAULT 0,
    locked_until TIMESTAMPTZ,
    last_login_at TIMESTAMPTZ,
    last_login_ip VARCHAR(50),
    last_device_info VARCHAR(500),
    mfa_enabled BOOLEAN DEFAULT FALSE,
    mfa_secret VARCHAR(100),
    password_changed_at TIMESTAMPTZ DEFAULT NOW(),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL UNIQUE,
    code VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(500),
    is_system_role BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Seed roles
INSERT INTO roles (name, code, description, is_system_role) VALUES
    ('Super User', 'SUPER_USER', 'Global system administrator with full access', TRUE),
    ('Admin', 'ADMIN', 'Tenant administrator with management rights', TRUE),
    ('Power User', 'POWER_USER', 'Advanced user with configuration rights', TRUE),
    ('Normal User', 'NORMAL_USER', 'Standard operational user', TRUE),
    ('Read-Only', 'READ_ONLY', 'View-only access', TRUE);

CREATE TABLE tenant_users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES yuktira_core.users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES yuktira_core.roles(id),
    is_active BOOLEAN DEFAULT TRUE,
    assigned_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, user_id)
);

CREATE TABLE tenant_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    key VARCHAR(100) NOT NULL,
    value TEXT,
    category VARCHAR(50) DEFAULT 'GENERAL',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, key)
);

CREATE TABLE super_user_permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES yuktira_core.users(id) ON DELETE CASCADE,
    can_override_approvals BOOLEAN DEFAULT TRUE,
    can_unlock_documents BOOLEAN DEFAULT TRUE,
    can_reset_passwords BOOLEAN DEFAULT TRUE,
    can_impersonate BOOLEAN DEFAULT TRUE,
    can_modify_workflows BOOLEAN DEFAULT TRUE,
    can_modify_number_ranges BOOLEAN DEFAULT TRUE,
    can_modify_dashboards BOOLEAN DEFAULT TRUE,
    can_modify_customization BOOLEAN DEFAULT TRUE,
    can_access_audit_logs BOOLEAN DEFAULT TRUE,
    can_enable_modules BOOLEAN DEFAULT TRUE,
    can_manage_tenants BOOLEAN DEFAULT TRUE,
    can_manage_plugins BOOLEAN DEFAULT TRUE,
    granted_by UUID REFERENCES yuktira_core.users(id),
    granted_at TIMESTAMPTZ DEFAULT NOW()
);

-- Function: Check if user is super user
CREATE OR REPLACE FUNCTION yuktira_core.is_super_user(p_user_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    v_is_super BOOLEAN;
BEGIN
    SELECT is_super_user INTO v_is_super FROM yuktira_core.users WHERE id = p_user_id;
    RETURN COALESCE(v_is_super, FALSE);
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Number Range Engine
-- ============================================
SET search_path TO yuktira_numberrange;

CREATE TABLE number_range_definitions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    module VARCHAR(20) NOT NULL,
    prefix VARCHAR(10) NOT NULL,
    suffix_year BOOLEAN DEFAULT TRUE,
    next_number BIGINT DEFAULT 1,
    number_length INT DEFAULT 8,
    padding_char CHAR(1) DEFAULT '0',
    is_active BOOLEAN DEFAULT TRUE,
    UNIQUE(tenant_id, module, prefix)
);

CREATE OR REPLACE FUNCTION yuktira_numberrange.get_next_document_number(
    p_tenant_id UUID,
    p_module VARCHAR(20),
    p_prefix VARCHAR(10),
    p_year INT DEFAULT NULL
) RETURNS VARCHAR(50) AS $$
DECLARE
    v_year VARCHAR(4);
    v_next_num BIGINT;
    v_padded VARCHAR(20);
    v_prefix VARCHAR(10);
    v_num_length INT;
    v_pad_char CHAR(1);
BEGIN
    IF p_year IS NULL THEN
        v_year := TO_CHAR(NOW(), 'YYYY');
    ELSE
        v_year := p_year::VARCHAR;
    END IF;

    SELECT prefix, number_length, padding_char
    INTO v_prefix, v_num_length, v_pad_char
    FROM yuktira_numberrange.number_range_definitions
    WHERE tenant_id = p_tenant_id AND module = p_module AND prefix = p_prefix AND is_active = TRUE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Number range not found for tenant_id=%, module=%, prefix=%', p_tenant_id, p_module, p_prefix;
    END IF;

    UPDATE yuktira_numberrange.number_range_definitions
    SET next_number = next_number + 1
    WHERE tenant_id = p_tenant_id AND module = p_module AND prefix = p_prefix
    RETURNING next_number - 1 INTO v_next_num;

    v_padded := LPAD(v_next_num::VARCHAR, v_num_length, v_pad_char);
    RETURN v_prefix || v_year || v_padded;
END;
$$ LANGUAGE plpgsql;

-- Seed number ranges (uses a known demo tenant; adjust UUID as needed)
INSERT INTO yuktira_numberrange.number_range_definitions (tenant_id, module, prefix, suffix_year, next_number, number_length)
SELECT id, 'MM', 'PO', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'MM', 'PR', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'MM', 'GR', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'MM', 'INV', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'SD', 'SO', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'SD', 'QUO', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'SD', 'DEL', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'SD', 'BIL', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'PP', 'PRD', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'HR', 'EMP', TRUE, 1, 6 FROM yuktira_core.tenants LIMIT 1
UNION ALL
SELECT id, 'FI', 'DOC', TRUE, 1, 8 FROM yuktira_core.tenants LIMIT 1;

-- ============================================
-- Audit Tables
-- ============================================
SET search_path TO yuktira_audit;

CREATE TABLE audit_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    timestamp TIMESTAMPTZ DEFAULT NOW(),
    user_id UUID REFERENCES yuktira_core.users(id),
    tenant_id UUID REFERENCES yuktira_core.tenants(id),
    module_name VARCHAR(50) NOT NULL,
    action_type VARCHAR(30) NOT NULL CHECK (action_type IN ('CREATE','UPDATE','DELETE','LOGIN','LOGOUT','APPROVAL','CONFIG','WORKFLOW','EXPORT','PRINT','API_CALL')),
    entity_name VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100),
    old_value JSONB,
    new_value JSONB,
    ip_address VARCHAR(50),
    device_info VARCHAR(500),
    user_agent TEXT,
    session_id VARCHAR(100),
    is_suspicious BOOLEAN DEFAULT FALSE,
    details TEXT
);

CREATE INDEX idx_audit_timestamp ON yuktira_audit.audit_log(timestamp DESC);
CREATE INDEX idx_audit_tenant ON yuktira_audit.audit_log(tenant_id);
CREATE INDEX idx_audit_user ON yuktira_audit.audit_log(user_id);
CREATE INDEX idx_audit_module ON yuktira_audit.audit_log(module_name);
CREATE INDEX idx_audit_action ON yuktira_audit.audit_log(action_type);
CREATE INDEX idx_audit_entity ON yuktira_audit.audit_log(entity_name, entity_id);

-- ============================================
-- Notification Tables
-- ============================================
SET search_path TO yuktira_notification;

CREATE TABLE notification_templates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    subject_template TEXT,
    body_template TEXT NOT NULL,
    channel_default VARCHAR(20) DEFAULT 'INAPP' CHECK (channel_default IN ('EMAIL','SMS','INAPP','ALL')),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

INSERT INTO yuktira_notification.notification_templates (code, name, subject_template, body_template, channel_default) VALUES
    ('APPROVAL_PENDING', 'Approval Request Pending', 'Approval Required: {{document_type}} {{document_number}}', 'A new {{document_type}} {{document_number}} requires your approval. Amount: {{amount}}. Please review in the approval center.', 'EMAIL'),
    ('APPROVAL_GRANTED', 'Approval Granted', '{{document_type}} {{document_number}} Approved', 'Your {{document_type}} {{document_number}} has been approved by {{approver_name}}.', 'INAPP'),
    ('APPROVAL_REJECTED', 'Approval Rejected', '{{document_type}} {{document_number}} Rejected', 'Your {{document_type}} {{document_number}} has been rejected by {{approver_name}}. Reason: {{reason}}.', 'EMAIL'),
    ('DOCUMENT_POSTED', 'Document Posted', '{{document_type}} {{document_number}} Posted', 'Document {{document_type}} {{document_number}} has been successfully posted.', 'INAPP'),
    ('MRP_SHORTAGE', 'Material Shortage Alert', 'Shortage Detected: {{material_code}}', 'Material {{material_code}} - {{material_name}} has a shortage of {{shortage_qty}} {{uom}}. Current stock: {{current_stock}}.', 'EMAIL'),
    ('LOGIN_ALERT', 'New Login Detected', 'New Login to Yuktira ERP', 'A new login was detected from IP: {{ip_address}}, Device: {{device_info}} at {{timestamp}}.', 'EMAIL'),
    ('WORKFLOW_OVERDUE', 'Workflow Step Overdue', 'Workflow Overdue: {{workflow_name}}', 'The workflow step {{step_name}} is overdue by {{overdue_hours}} hours. Entity: {{entity_name}} {{entity_id}}.', 'INAPP'),
    ('PO_CREATED', 'Purchase Order Created', 'PO {{document_number}} Created', 'Purchase Order {{document_number}} has been created for vendor {{vendor_name}}. Amount: {{amount}}.', 'INAPP'),
    ('SO_CONFIRMED', 'Sales Order Confirmed', 'SO {{document_number}} Confirmed', 'Sales Order {{document_number}} has been confirmed for customer {{customer_name}}.', 'INAPP'),
    ('INVENTORY_ALERT', 'Inventory Threshold Alert', 'Inventory Alert: {{material_code}}', 'Material {{material_code}} has reached minimum stock level. Current: {{current_stock}}, Min: {{min_stock}}.', 'EMAIL');

CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES yuktira_core.users(id) ON DELETE CASCADE,
    tenant_id UUID REFERENCES yuktira_core.tenants(id),
    channel VARCHAR(20) NOT NULL CHECK (channel IN ('EMAIL','SMS','INAPP')),
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    link_url VARCHAR(500),
    template_code VARCHAR(100),
    entity_type VARCHAR(50),
    entity_id VARCHAR(100),
    is_read BOOLEAN DEFAULT FALSE,
    read_at TIMESTAMPTZ,
    is_archived BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_notif_user ON yuktira_notification.notifications(user_id, is_read, created_at DESC);
CREATE INDEX idx_notif_tenant ON yuktira_notification.notifications(tenant_id, created_at DESC);

-- ============================================
-- Plugin Tables
-- ============================================
SET search_path TO yuktira_plugin;

CREATE TABLE plugins (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(100) NOT NULL UNIQUE,
    version VARCHAR(20) NOT NULL,
    description TEXT,
    assembly_name VARCHAR(200),
    is_core BOOLEAN DEFAULT FALSE,
    is_enabled_global BOOLEAN DEFAULT TRUE,
    dependencies JSONB,
    min_version VARCHAR(20),
    max_version VARCHAR(20),
    icon_class VARCHAR(100),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE plugin_tenant (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    plugin_id UUID NOT NULL REFERENCES yuktira_plugin.plugins(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    is_enabled BOOLEAN DEFAULT TRUE,
    installed_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(plugin_id, tenant_id)
);

CREATE TABLE plugin_permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    plugin_id UUID NOT NULL REFERENCES yuktira_plugin.plugins(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES yuktira_core.roles(id),
    can_access BOOLEAN DEFAULT FALSE,
    UNIQUE(plugin_id, role_id)
);

CREATE TABLE plugin_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    plugin_id UUID NOT NULL REFERENCES yuktira_plugin.plugins(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    key VARCHAR(100) NOT NULL,
    value TEXT,
    UNIQUE(plugin_id, tenant_id, key)
);

-- ============================================
-- Dashboard Tables
-- ============================================
SET search_path TO yuktira_dashboard;

CREATE TABLE dashboard_widgets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(100) NOT NULL UNIQUE,
    widget_type VARCHAR(50) NOT NULL CHECK (widget_type IN ('KPI','CHART','TABLE','LIST','CALENDAR','CUSTOM')),
    description TEXT,
    default_width INT DEFAULT 4,
    default_height INT DEFAULT 2,
    icon_class VARCHAR(100),
    config_json JSONB,
    is_system BOOLEAN DEFAULT FALSE,
    min_role_level VARCHAR(50) DEFAULT 'READ_ONLY',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

INSERT INTO yuktira_dashboard.dashboard_widgets (name, code, widget_type, description, default_width, default_height, config_json, is_system, min_role_level) VALUES
    ('Open Purchase Orders', 'OPEN_PO', 'KPI', 'Count of open purchase orders', 2, 1, '{"query":"SELECT COUNT(*) FROM yuktira_mm.purchase_orders WHERE status != ''COMPLETED'' AND tenant_id = @tenant","icon":"shopping-cart","color":"primary"}', TRUE, 'READ_ONLY'),
    ('Pending Approvals', 'PENDING_APPROVALS', 'LIST', 'List of pending approval requests', 4, 3, '{"query":"SELECT * FROM yuktira_approval.approval_requests WHERE status = ''PENDING'' AND tenant_id = @tenant"}', TRUE, 'NORMAL_USER'),
    ('Monthly Revenue', 'MONTHLY_REVENUE', 'CHART', 'Monthly revenue bar chart', 4, 2, '{"chart_type":"bar","query":"SELECT DATE_TRUNC(''month'', posting_date) as month, SUM(amount) as revenue FROM yuktira_fi.fi_documents WHERE type = ''INVOICE'' AND tenant_id = @tenant GROUP BY 1 ORDER BY 1 DESC LIMIT 12"}', TRUE, 'READ_ONLY'),
    ('Stock Overview', 'STOCK_OVERVIEW', 'KPI', 'Total materials and stock value', 2, 1, '{"query":"SELECT COUNT(DISTINCT material_id) as materials, SUM(stock_qty) as total_qty FROM yuktira_mm.stock WHERE tenant_id = @tenant","icon":"boxes","color":"success"}', TRUE, 'READ_ONLY'),
    ('Recent Notifications', 'RECENT_NOTIFICATIONS', 'LIST', 'Recent notifications for the user', 4, 3, '{"personal":true,"type":"notifications"}', TRUE, 'READ_ONLY'),
    ('My Tasks', 'MY_TASKS', 'LIST', 'Current user task list', 4, 2, '{"personal":true,"type":"tasks"}', TRUE, 'NORMAL_USER'),
    ('Quality Alerts', 'QUALITY_ALERTS', 'KPI', 'Open quality inspection lots', 2, 1, '{"query":"SELECT COUNT(*) FROM yuktira_qm.inspection_lots WHERE status IN (''OPEN'',''IN_PROGRESS'') AND tenant_id = @tenant","icon":"check-circle","color":"warning"}', TRUE, 'READ_ONLY'),
    ('Production Status', 'PRODUCTION_STATUS', 'CHART', 'Production order status breakdown', 4, 2, '{"chart_type":"doughnut","query":"SELECT status, COUNT(*) FROM yuktira_pp.production_orders WHERE tenant_id = @tenant GROUP BY status"}', TRUE, 'READ_ONLY');

CREATE TABLE dashboard_user_widgets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES yuktira_core.users(id) ON DELETE CASCADE,
    widget_id UUID NOT NULL REFERENCES yuktira_dashboard.dashboard_widgets(id) ON DELETE CASCADE,
    position_x INT DEFAULT 0,
    position_y INT DEFAULT 0,
    width INT DEFAULT 4,
    height INT DEFAULT 2,
    is_visible BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(user_id, widget_id)
);

CREATE TABLE dashboard_role_widgets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_id UUID NOT NULL REFERENCES yuktira_core.roles(id),
    widget_id UUID NOT NULL REFERENCES yuktira_dashboard.dashboard_widgets(id) ON DELETE CASCADE,
    is_default BOOLEAN DEFAULT FALSE,
    UNIQUE(role_id, widget_id)
);

-- ============================================
-- Customization Engine
-- ============================================
SET search_path TO yuktira_customization;

CREATE TABLE customization_columns (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    screen_name VARCHAR(100) NOT NULL,
    column_name VARCHAR(100) NOT NULL,
    column_label VARCHAR(200) NOT NULL,
    data_type VARCHAR(50) DEFAULT 'TEXT' CHECK (data_type IN ('TEXT','NUMBER','DATE','BOOLEAN','DECIMAL','FORMULA')),
    default_width INT DEFAULT 150,
    default_order INT DEFAULT 0,
    is_default_visible BOOLEAN DEFAULT TRUE,
    formula TEXT,
    conditional_format_json JSONB,
    source_entity VARCHAR(200),
    source_field VARCHAR(100),
    is_system BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, screen_name, column_name)
);

CREATE TABLE customization_user_layout (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES yuktira_core.users(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    screen_name VARCHAR(100) NOT NULL,
    column_name VARCHAR(100) NOT NULL,
    column_label VARCHAR(200),
    width INT DEFAULT 150,
    order_index INT DEFAULT 0,
    is_visible BOOLEAN DEFAULT TRUE,
    is_frozen BOOLEAN DEFAULT FALSE,
    UNIQUE(user_id, tenant_id, screen_name, column_name)
);

CREATE TABLE customization_role_rules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_id UUID NOT NULL REFERENCES yuktira_core.roles(id),
    screen_name VARCHAR(100) NOT NULL,
    column_name VARCHAR(100) NOT NULL,
    is_allowed_to_modify BOOLEAN DEFAULT TRUE,
    is_allowed_to_add BOOLEAN DEFAULT FALSE,
    is_allowed_to_delete BOOLEAN DEFAULT FALSE,
    UNIQUE(role_id, screen_name, column_name)
);

-- ============================================
-- Approval Engine Tables
-- ============================================
SET search_path TO yuktira_approval;

CREATE TABLE approval_matrices (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    module VARCHAR(50) NOT NULL,
    document_type VARCHAR(50) NOT NULL,
    description VARCHAR(500),
    min_amount DECIMAL(18,2) DEFAULT 0,
    max_amount DECIMAL(18,2) DEFAULT 999999999.99,
    level INT NOT NULL DEFAULT 1,
    approval_role_id UUID REFERENCES yuktira_core.roles(id),
    approval_user_id UUID REFERENCES yuktira_core.users(id),
    escalation_hours INT DEFAULT 24,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE approval_requests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    workflow_id UUID REFERENCES yuktira_workflow.workflows(id),
    workflow_node_id UUID REFERENCES yuktira_workflow.workflow_nodes(id),
    module VARCHAR(50) NOT NULL,
    document_type VARCHAR(50) NOT NULL,
    document_id VARCHAR(100) NOT NULL,
    document_number VARCHAR(100),
    amount DECIMAL(18,2) DEFAULT 0,
    requested_by UUID REFERENCES yuktira_core.users(id),
    current_level INT DEFAULT 1,
    max_level INT DEFAULT 1,
    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING','APPROVED','REJECTED','ESCALATED','CANCELLED')),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    completed_at TIMESTAMPTZ
);

CREATE TABLE approval_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    approval_request_id UUID NOT NULL REFERENCES yuktira_approval.approval_requests(id) ON DELETE CASCADE,
    level INT NOT NULL,
    approver_id UUID REFERENCES yuktira_core.users(id),
    action VARCHAR(20) NOT NULL CHECK (action IN ('APPROVED','REJECTED','ESCALATED','DELEGATED')),
    comments TEXT,
    action_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- Workflow Tables
-- ============================================
SET search_path TO yuktira_workflow;

CREATE TABLE workflow_definitions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    code VARCHAR(100) NOT NULL,
    module VARCHAR(50),
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    version INT DEFAULT 1,
    bpmn_xml TEXT,
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, code)
);

CREATE TABLE workflow_nodes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workflow_id UUID NOT NULL REFERENCES yuktira_workflow.workflow_definitions(id) ON DELETE CASCADE,
    node_type VARCHAR(30) NOT NULL CHECK (node_type IN ('START','TASK','APPROVAL','DECISION','TIMER','API_CALL','EMAIL','SMS','CONDITION','END')),
    label VARCHAR(200) NOT NULL,
    description TEXT,
    config_json JSONB,
    position_x FLOAT DEFAULT 0,
    position_y FLOAT DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE workflow_edges (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workflow_id UUID NOT NULL REFERENCES yuktira_workflow.workflow_definitions(id) ON DELETE CASCADE,
    from_node_id UUID NOT NULL REFERENCES yuktira_workflow.workflow_nodes(id) ON DELETE CASCADE,
    to_node_id UUID NOT NULL REFERENCES yuktira_workflow.workflow_nodes(id) ON DELETE CASCADE,
    condition_expression TEXT,
    label VARCHAR(200),
    sequence_order INT DEFAULT 0,
    UNIQUE(workflow_id, from_node_id, to_node_id)
);

CREATE TABLE workflow_instances (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workflow_id UUID NOT NULL REFERENCES yuktira_workflow.workflow_definitions(id),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id),
    entity_name VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    current_node_id UUID REFERENCES yuktira_workflow.workflow_nodes(id),
    status VARCHAR(30) NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','COMPLETED','TERMINATED','SUSPENDED')),
    variables JSONB,
    started_by UUID REFERENCES yuktira_core.users(id),
    started_at TIMESTAMPTZ DEFAULT NOW(),
    completed_at TIMESTAMPTZ
);

CREATE TABLE workflow_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workflow_instance_id UUID NOT NULL REFERENCES yuktira_workflow.workflow_instances(id) ON DELETE CASCADE,
    node_id UUID REFERENCES yuktira_workflow.workflow_nodes(id),
    action VARCHAR(50) NOT NULL,
    actor_id UUID REFERENCES yuktira_core.users(id),
    comment TEXT,
    payload JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- MM - Materials Management Schema
-- ============================================
SET search_path TO yuktira_mm;

CREATE TABLE material_masters (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    material_code VARCHAR(50) NOT NULL,
    material_name VARCHAR(200) NOT NULL,
    material_group VARCHAR(50),
    material_type VARCHAR(50) DEFAULT 'RAW' CHECK (material_type IN ('RAW','SEMI_FINISHED','FINISHED','TRADING','PACKAGING','CONSUMABLE','SERVICE')),
    base_uom VARCHAR(10) DEFAULT 'EA',
    purchase_uom VARCHAR(10) DEFAULT 'EA',
    sales_uom VARCHAR(10) DEFAULT 'EA',
    weight DECIMAL(18,4),
    weight_uom VARCHAR(10),
    volume DECIMAL(18,4),
    volume_uom VARCHAR(10),
    min_stock_qty DECIMAL(18,4) DEFAULT 0,
    max_stock_qty DECIMAL(18,4) DEFAULT 0,
    reorder_level DECIMAL(18,4) DEFAULT 0,
    reorder_qty DECIMAL(18,4) DEFAULT 0,
    lead_time_days INT DEFAULT 0,
    valuation_class VARCHAR(20),
    standard_price DECIMAL(18,4) DEFAULT 0,
    moving_price DECIMAL(18,4) DEFAULT 0,
    last_purchase_price DECIMAL(18,4) DEFAULT 0,
    batch_managed BOOLEAN DEFAULT FALSE,
    serial_managed BOOLEAN DEFAULT FALSE,
    shelf_life_days INT,
    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','BLOCKED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, material_code)
);

CREATE TABLE vendor_masters (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    vendor_code VARCHAR(50) NOT NULL,
    vendor_name VARCHAR(200) NOT NULL,
    vendor_type VARCHAR(50) DEFAULT 'REGULAR' CHECK (vendor_type IN ('REGULAR','ONE_TIME','INTERCOMPANY','GOVERNMENT')),
    tax_id VARCHAR(50),
    payment_terms VARCHAR(50) DEFAULT 'NET30',
    payment_days INT DEFAULT 30,
    currency VARCHAR(10) DEFAULT 'USD',
    address_line1 VARCHAR(200),
    address_line2 VARCHAR(200),
    city VARCHAR(100),
    state VARCHAR(100),
    country VARCHAR(100),
    postal_code VARCHAR(20),
    contact_person VARCHAR(200),
    email VARCHAR(200),
    phone VARCHAR(50),
    mobile VARCHAR(50),
    bank_name VARCHAR(200),
    bank_account VARCHAR(100),
    bank_swift VARCHAR(50),
    credit_limit DECIMAL(18,2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','BLOCKED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, vendor_code)
);

CREATE TABLE purchase_requisitions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    pr_number VARCHAR(50) NOT NULL,
    pr_date DATE DEFAULT CURRENT_DATE,
    requested_by UUID REFERENCES yuktira_core.users(id),
    department VARCHAR(100),
    notes TEXT,
    status VARCHAR(30) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','SUBMITTED','APPROVED','REJECTED','CONVERTED','CANCELLED')),
    total_amount DECIMAL(18,2) DEFAULT 0,
    approved_by UUID REFERENCES yuktira_core.users(id),
    approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, pr_number)
);

CREATE TABLE purchase_requisition_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    pr_id UUID NOT NULL REFERENCES yuktira_mm.purchase_requisitions(id) ON DELETE CASCADE,
    line_no INT NOT NULL,
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity DECIMAL(18,4) NOT NULL,
    uom VARCHAR(10) DEFAULT 'EA',
    estimated_price DECIMAL(18,4) DEFAULT 0,
    currency VARCHAR(10) DEFAULT 'USD',
    required_date DATE,
    notes TEXT,
    status VARCHAR(20) DEFAULT 'PENDING'
);

CREATE TABLE purchase_orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    po_number VARCHAR(50) NOT NULL,
    po_date DATE DEFAULT CURRENT_DATE,
    vendor_id UUID REFERENCES yuktira_mm.vendor_masters(id),
    vendor_code VARCHAR(50),
    vendor_name VARCHAR(200),
    payment_terms VARCHAR(50),
    delivery_date DATE,
    shipping_address TEXT,
    notes TEXT,
    status VARCHAR(30) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','SUBMITTED','APPROVED','PARTIALLY_GR','COMPLETED','CANCELLED')),
    total_amount DECIMAL(18,2) DEFAULT 0,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    grand_total DECIMAL(18,2) DEFAULT 0,
    currency VARCHAR(10) DEFAULT 'USD',
    created_by UUID REFERENCES yuktira_core.users(id),
    approved_by UUID REFERENCES yuktira_core.users(id),
    approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, po_number)
);

CREATE TABLE purchase_order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    po_id UUID NOT NULL REFERENCES yuktira_mm.purchase_orders(id) ON DELETE CASCADE,
    line_no INT NOT NULL,
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity DECIMAL(18,4) NOT NULL,
    received_qty DECIMAL(18,4) DEFAULT 0,
    uom VARCHAR(10) DEFAULT 'EA',
    unit_price DECIMAL(18,4) DEFAULT 0,
    net_amount DECIMAL(18,2) DEFAULT 0,
    tax_rate DECIMAL(5,2) DEFAULT 0,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    required_date DATE,
    pr_id UUID REFERENCES yuktira_mm.purchase_requisitions(id),
    status VARCHAR(20) DEFAULT 'OPEN' CHECK (status IN ('OPEN','PARTIALLY_GR','COMPLETED','CANCELLED'))
);

CREATE TABLE goods_receipts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    grn_number VARCHAR(50) NOT NULL,
    grn_date DATE DEFAULT CURRENT_DATE,
    po_id UUID REFERENCES yuktira_mm.purchase_orders(id),
    po_number VARCHAR(50),
    vendor_id UUID REFERENCES yuktira_mm.vendor_masters(id),
    vendor_name VARCHAR(200),
    document_type VARCHAR(30) DEFAULT 'PO_RECEIPT' CHECK (document_type IN ('PO_RECEIPT','RETURN','ADJUSTMENT','TRANSFER')),
    notes TEXT,
    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','POSTED','REVERSED','CANCELLED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    posted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, grn_number)
);

CREATE TABLE goods_receipt_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    grn_id UUID NOT NULL REFERENCES yuktira_mm.goods_receipts(id) ON DELETE CASCADE,
    line_no INT NOT NULL,
    po_item_id UUID REFERENCES yuktira_mm.purchase_order_items(id),
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity DECIMAL(18,4) NOT NULL,
    uom VARCHAR(10) DEFAULT 'EA',
    unit_price DECIMAL(18,4) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    batch_no VARCHAR(50),
    manufacture_date DATE,
    expiry_date DATE,
    serial_no VARCHAR(100),
    storage_location_id UUID, -- FK added below
    status VARCHAR(20) DEFAULT 'POSTED'
);

CREATE TABLE invoice_verifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    invoice_number VARCHAR(50) NOT NULL,
    invoice_date DATE DEFAULT CURRENT_DATE,
    po_id UUID REFERENCES yuktira_mm.purchase_orders(id),
    po_number VARCHAR(50),
    vendor_id UUID REFERENCES yuktira_mm.vendor_masters(id),
    vendor_name VARCHAR(200),
    invoice_amount DECIMAL(18,2) NOT NULL,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    currency VARCHAR(10) DEFAULT 'USD',
    payment_terms VARCHAR(50),
    due_date DATE,
    notes TEXT,
    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','POSTED','CANCELLED')),
    fi_document_id UUID, -- FK added below
    created_by UUID REFERENCES yuktira_core.users(id),
    posted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, invoice_number)
);

CREATE TABLE stock (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    material_id UUID NOT NULL REFERENCES yuktira_mm.material_masters(id),
    storage_location_id UUID REFERENCES yuktira_wm.storage_locations(id),
    batch_no VARCHAR(50),
    stock_qty DECIMAL(18,4) NOT NULL DEFAULT 0,
    reserved_qty DECIMAL(18,4) DEFAULT 0,
    available_qty DECIMAL(18,4) GENERATED ALWAYS AS (stock_qty - reserved_qty) STORED,
    uom VARCHAR(10) DEFAULT 'EA',
    unit_price DECIMAL(18,4) DEFAULT 0,
    total_value DECIMAL(18,2) GENERATED ALWAYS AS (stock_qty * unit_price) STORED,
    last_movement_at TIMESTAMPTZ,
    UNIQUE(tenant_id, material_id, storage_location_id, batch_no)
);

-- ============================================
-- SD - Sales & Distribution Schema
-- ============================================
SET search_path TO yuktira_sd;

CREATE TABLE customer_masters (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    customer_code VARCHAR(50) NOT NULL,
    customer_name VARCHAR(200) NOT NULL,
    customer_type VARCHAR(50) DEFAULT 'REGULAR' CHECK (customer_type IN ('REGULAR','ONE_TIME','INTERCOMPANY','GOVERNMENT')),
    tax_id VARCHAR(50),
    payment_terms VARCHAR(50) DEFAULT 'NET30',
    payment_days INT DEFAULT 30,
    currency VARCHAR(10) DEFAULT 'USD',
    credit_limit DECIMAL(18,2) DEFAULT 0,
    address_line1 VARCHAR(200),
    address_line2 VARCHAR(200),
    city VARCHAR(100),
    state VARCHAR(100),
    country VARCHAR(100),
    postal_code VARCHAR(20),
    contact_person VARCHAR(200),
    email VARCHAR(200),
    phone VARCHAR(50),
    mobile VARCHAR(50),
    shipping_address TEXT,
    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','BLOCKED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, customer_code)
);

CREATE TABLE sales_inquiries (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    inquiry_number VARCHAR(50) NOT NULL,
    inquiry_date DATE DEFAULT CURRENT_DATE,
    customer_id UUID REFERENCES yuktira_sd.customer_masters(id),
    customer_name VARCHAR(200),
    contact_person VARCHAR(200),
    email VARCHAR(200),
    phone VARCHAR(50),
    valid_until DATE,
    notes TEXT,
    status VARCHAR(20) DEFAULT 'OPEN' CHECK (status IN ('OPEN','QUOTED','CLOSED','CANCELLED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, inquiry_number)
);

CREATE TABLE sales_quotations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    quotation_number VARCHAR(50) NOT NULL,
    quotation_date DATE DEFAULT CURRENT_DATE,
    customer_id UUID REFERENCES yuktira_sd.customer_masters(id),
    customer_name VARCHAR(200),
    inquiry_id UUID REFERENCES yuktira_sd.sales_inquiries(id),
    valid_until DATE,
    payment_terms VARCHAR(50),
    delivery_terms VARCHAR(200),
    notes TEXT,
    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','SUBMITTED','ACCEPTED','REJECTED','EXPIRED','CONVERTED')),
    total_amount DECIMAL(18,2) DEFAULT 0,
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, quotation_number)
);

CREATE TABLE sales_orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    so_number VARCHAR(50) NOT NULL,
    so_date DATE DEFAULT CURRENT_DATE,
    customer_id UUID REFERENCES yuktira_sd.customer_masters(id),
    customer_name VARCHAR(200),
    quotation_id UUID REFERENCES yuktira_sd.sales_quotations(id),
    payment_terms VARCHAR(50),
    delivery_date DATE,
    shipping_address TEXT,
    notes TEXT,
    status VARCHAR(30) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','CONFIRMED','DELIVERING','PARTIALLY_DELIVERED','COMPLETED','CANCELLED')),
    total_amount DECIMAL(18,2) DEFAULT 0,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    grand_total DECIMAL(18,2) DEFAULT 0,
    currency VARCHAR(10) DEFAULT 'USD',
    created_by UUID REFERENCES yuktira_core.users(id),
    approved_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, so_number)
);

CREATE TABLE sales_order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    so_id UUID NOT NULL REFERENCES yuktira_sd.sales_orders(id) ON DELETE CASCADE,
    line_no INT NOT NULL,
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity DECIMAL(18,4) NOT NULL,
    delivered_qty DECIMAL(18,4) DEFAULT 0,
    uom VARCHAR(10) DEFAULT 'EA',
    unit_price DECIMAL(18,4) DEFAULT 0,
    net_amount DECIMAL(18,2) DEFAULT 0,
    tax_rate DECIMAL(5,2) DEFAULT 0,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    total_amount DECIMAL(18,2) DEFAULT 0,
    required_date DATE,
    status VARCHAR(20) DEFAULT 'OPEN'
);

CREATE TABLE deliveries (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    delivery_number VARCHAR(50) NOT NULL,
    delivery_date DATE DEFAULT CURRENT_DATE,
    so_id UUID REFERENCES yuktira_sd.sales_orders(id),
    so_number VARCHAR(50),
    customer_id UUID REFERENCES yuktira_sd.customer_masters(id),
    customer_name VARCHAR(200),
    shipping_address TEXT,
    delivery_type VARCHAR(30) DEFAULT 'DELIVERY' CHECK (delivery_type IN ('DELIVERY','RETURN','TRANSFER')),
    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','PICKING','PACKED','SHIPPED','DELIVERED','CANCELLED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, delivery_number)
);

CREATE TABLE delivery_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    delivery_id UUID NOT NULL REFERENCES yuktira_sd.deliveries(id) ON DELETE CASCADE,
    line_no INT NOT NULL,
    so_item_id UUID REFERENCES yuktira_sd.sales_order_items(id),
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity DECIMAL(18,4) NOT NULL,
    uom VARCHAR(10) DEFAULT 'EA',
    batch_no VARCHAR(50),
    serial_no VARCHAR(100),
    storage_location_id UUID REFERENCES yuktira_wm.storage_locations(id)
);

CREATE TABLE billing_documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    billing_number VARCHAR(50) NOT NULL,
    billing_date DATE DEFAULT CURRENT_DATE,
    so_id UUID REFERENCES yuktira_sd.sales_orders(id),
    so_number VARCHAR(50),
    delivery_id UUID REFERENCES yuktira_sd.deliveries(id),
    customer_id UUID REFERENCES yuktira_sd.customer_masters(id),
    customer_name VARCHAR(200),
    billing_type VARCHAR(30) DEFAULT 'INVOICE' CHECK (billing_type IN ('INVOICE','CREDIT_NOTE','DEBIT_NOTE')),
    total_amount DECIMAL(18,2) DEFAULT 0,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    grand_total DECIMAL(18,2) DEFAULT 0,
    currency VARCHAR(10) DEFAULT 'USD',
    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','POSTED','CANCELLED','PAID')),
    fi_document_id UUID, -- FK added below
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, billing_number)
);

-- ============================================
-- PP - Production Planning Schema
-- ============================================
SET search_path TO yuktira_pp;

CREATE TABLE work_centers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    code VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    work_center_type VARCHAR(50) DEFAULT 'MACHINE' CHECK (work_center_type IN ('MACHINE','LABOR','ASSEMBLY','MAINTENANCE')),
    capacity_per_day DECIMAL(18,4) DEFAULT 8,
    capacity_uom VARCHAR(10) DEFAULT 'HRS',
    efficiency DECIMAL(5,2) DEFAULT 100,
    status VARCHAR(20) DEFAULT 'ACTIVE',
    UNIQUE(tenant_id, code)
);

CREATE TABLE bill_of_materials (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    bom_code VARCHAR(50) NOT NULL,
    bom_name VARCHAR(200),
    finished_material_id UUID NOT NULL REFERENCES yuktira_mm.material_masters(id),
    bom_type VARCHAR(30) DEFAULT 'PRODUCTION' CHECK (bom_type IN ('PRODUCTION','ENGINEERING','MAINTENANCE','PACKAGING')),
    base_qty DECIMAL(18,4) DEFAULT 1,
    uom VARCHAR(10) DEFAULT 'EA',
    is_active BOOLEAN DEFAULT TRUE,
    version INT DEFAULT 1,
    valid_from DATE,
    valid_to DATE,
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, bom_code)
);

CREATE TABLE bom_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    bom_id UUID NOT NULL REFERENCES yuktira_pp.bill_of_materials(id) ON DELETE CASCADE,
    line_no INT NOT NULL,
    material_id UUID NOT NULL REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity DECIMAL(18,4) NOT NULL,
    uom VARCHAR(10) DEFAULT 'EA',
    scrap_pct DECIMAL(5,2) DEFAULT 0,
    item_type VARCHAR(30) DEFAULT 'RAW' CHECK (item_type IN ('RAW','SEMI_FINISHED','PACKAGING','TOOL')),
    storage_location_id UUID REFERENCES yuktira_wm.storage_locations(id)
);

CREATE TABLE routings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    routing_code VARCHAR(50) NOT NULL,
    routing_name VARCHAR(200),
    bom_id UUID REFERENCES yuktira_pp.bill_of_materials(id),
    is_active BOOLEAN DEFAULT TRUE,
    UNIQUE(tenant_id, routing_code)
);

CREATE TABLE routing_operations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    routing_id UUID NOT NULL REFERENCES yuktira_pp.routings(id) ON DELETE CASCADE,
    operation_no INT NOT NULL,
    operation_name VARCHAR(200) NOT NULL,
    work_center_id UUID REFERENCES yuktira_pp.work_centers(id),
    setup_time_min DECIMAL(18,4) DEFAULT 0,
    run_time_min DECIMAL(18,4) DEFAULT 0,
    teardown_time_min DECIMAL(18,4) DEFAULT 0,
    machine_time_min DECIMAL(18,4) DEFAULT 0,
    labor_time_min DECIMAL(18,4) DEFAULT 0,
    queue_time_min DECIMAL(18,4) DEFAULT 0,
    move_time_min DECIMAL(18,4) DEFAULT 0
);

CREATE TABLE planned_orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    planned_order_number VARCHAR(50) NOT NULL,
    material_id UUID NOT NULL REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity DECIMAL(18,4) NOT NULL,
    uom VARCHAR(10) DEFAULT 'EA',
    bom_id UUID REFERENCES yuktira_pp.bill_of_materials(id),
    routing_id UUID REFERENCES yuktira_pp.routings(id),
    start_date DATE,
    end_date DATE,
    order_type VARCHAR(30) DEFAULT 'IN_HOUSE' CHECK (order_type IN ('IN_HOUSE','EXTERNAL','BOTH')),
    source VARCHAR(50) DEFAULT 'MRP' CHECK (source IN ('MRP','MANUAL','MPS')),
    status VARCHAR(20) DEFAULT 'OPEN' CHECK (status IN ('OPEN','CONVERTED','CANCELLED')),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, planned_order_number)
);

CREATE TABLE production_orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    production_order_number VARCHAR(50) NOT NULL,
    material_id UUID NOT NULL REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity DECIMAL(18,4) NOT NULL,
    produced_qty DECIMAL(18,4) DEFAULT 0,
    scrapped_qty DECIMAL(18,4) DEFAULT 0,
    uom VARCHAR(10) DEFAULT 'EA',
    bom_id UUID REFERENCES yuktira_pp.bill_of_materials(id),
    routing_id UUID REFERENCES yuktira_pp.routings(id),
    planned_order_id UUID REFERENCES yuktira_pp.planned_orders(id),
    start_date DATE,
    end_date DATE,
    status VARCHAR(30) DEFAULT 'PLANNED' CHECK (status IN ('PLANNED','RELEASED','IN_PROGRESS','COMPLETED','PARTIALLY_COMPLETED','CANCELLED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, production_order_number)
);

CREATE TABLE production_order_operations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    production_order_id UUID NOT NULL REFERENCES yuktira_pp.production_orders(id) ON DELETE CASCADE,
    operation_no INT NOT NULL,
    operation_name VARCHAR(200),
    work_center_id UUID REFERENCES yuktira_pp.work_centers(id),
    planned_setup_min DECIMAL(18,4) DEFAULT 0,
    planned_run_min DECIMAL(18,4) DEFAULT 0,
    actual_setup_min DECIMAL(18,4),
    actual_run_min DECIMAL(18,4),
    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING','IN_PROGRESS','COMPLETED','CLOSED')),
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ
);

CREATE TABLE production_order_materials (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    production_order_id UUID NOT NULL REFERENCES yuktira_pp.production_orders(id) ON DELETE CASCADE,
    material_id UUID NOT NULL REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    required_qty DECIMAL(18,4) NOT NULL,
    issued_qty DECIMAL(18,4) DEFAULT 0,
    uom VARCHAR(10) DEFAULT 'EA',
    storage_location_id UUID REFERENCES yuktira_wm.storage_locations(id),
    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING','PARTIALLY_ISSUED','ISSUED'))
);

CREATE TABLE production_order_confirmation (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    production_order_id UUID NOT NULL REFERENCES yuktira_pp.production_orders(id),
    confirm_type VARCHAR(30) DEFAULT 'FINAL' CHECK (confirm_type IN ('OPERATION','FINAL','SCRAP')),
    operation_no INT,
    confirmed_qty DECIMAL(18,4) NOT NULL,
    scrap_qty DECIMAL(18,4) DEFAULT 0,
    uom VARCHAR(10) DEFAULT 'EA',
    confirmed_at TIMESTAMPTZ DEFAULT NOW(),
    confirmed_by UUID REFERENCES yuktira_core.users(id)
);

-- ============================================
-- QM - Quality Management Schema
-- ============================================
SET search_path TO yuktira_qm;

CREATE TABLE inspection_plans (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    plan_code VARCHAR(50) NOT NULL,
    plan_name VARCHAR(200),
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    inspection_type VARCHAR(30) DEFAULT 'INCOMING' CHECK (inspection_type IN ('INCOMING','IN_PROCESS','FINAL','OUTGOING')),
    is_active BOOLEAN DEFAULT TRUE,
    UNIQUE(tenant_id, plan_code)
);

CREATE TABLE inspection_plan_characteristics (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    inspection_plan_id UUID NOT NULL REFERENCES yuktira_qm.inspection_plans(id) ON DELETE CASCADE,
    characteristic_no INT NOT NULL,
    characteristic_name VARCHAR(200) NOT NULL,
    characteristic_type VARCHAR(30) DEFAULT 'QUANTITATIVE' CHECK (characteristic_type IN ('QUANTITATIVE','QUALITATIVE','VISUAL')),
    target_value DECIMAL(18,4),
    upper_spec_limit DECIMAL(18,4),
    lower_spec_limit DECIMAL(18,4),
    nominal_value DECIMAL(18,4),
    tolerance_plus DECIMAL(18,4),
    tolerance_minus DECIMAL(18,4),
    uom VARCHAR(20),
    sample_size INT DEFAULT 1,
    inspection_method TEXT
);

CREATE TABLE inspection_lots (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    lot_number VARCHAR(50) NOT NULL,
    inspection_plan_id UUID REFERENCES yuktira_qm.inspection_plans(id),
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    batch_no VARCHAR(50),
    quantity DECIMAL(18,4) NOT NULL,
    uom VARCHAR(10) DEFAULT 'EA',
    source_type VARCHAR(30) CHECK (source_type IN ('GRN','PRODUCTION','TRANSFER','RETURN')),
    source_document_id VARCHAR(100),
    source_document_number VARCHAR(50),
    status VARCHAR(30) DEFAULT 'OPEN' CHECK (status IN ('OPEN','IN_PROGRESS','COMPLETED','USAGE_DECIDED','CANCELLED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, lot_number)
);

CREATE TABLE inspection_results (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    inspection_lot_id UUID NOT NULL REFERENCES yuktira_qm.inspection_lots(id) ON DELETE CASCADE,
    characteristic_id UUID REFERENCES yuktira_qm.inspection_plan_characteristics(id),
    characteristic_name VARCHAR(200),
    result_value TEXT,
    is_acceptable BOOLEAN,
    tested_by UUID REFERENCES yuktira_core.users(id),
    tested_at TIMESTAMPTZ DEFAULT NOW(),
    notes TEXT
);

CREATE TABLE usage_decisions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    inspection_lot_id UUID NOT NULL REFERENCES yuktira_qm.inspection_lots(id) ON DELETE CASCADE,
    decision VARCHAR(30) NOT NULL CHECK (decision IN ('ACCEPTED','REJECTED','REWORK','SCRAP','SELECTIVE_USE')),
    decision_by UUID REFERENCES yuktira_core.users(id),
    decision_at TIMESTAMPTZ DEFAULT NOW(),
    notes TEXT
);

CREATE TABLE certificates_of_analysis (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    coa_number VARCHAR(50) NOT NULL,
    inspection_lot_id UUID REFERENCES yuktira_qm.inspection_lots(id),
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    batch_no VARCHAR(50),
    coa_date DATE DEFAULT CURRENT_DATE,
    document_url VARCHAR(500),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, coa_number)
);

-- ============================================
-- WM - Warehouse Management Schema
-- ============================================
SET search_path TO yuktira_wm;

CREATE TABLE storage_locations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    warehouse_code VARCHAR(20) NOT NULL,
    location_code VARCHAR(50) NOT NULL,
    location_name VARCHAR(200),
    location_type VARCHAR(30) DEFAULT 'STORAGE' CHECK (location_type IN ('STORAGE','PICKING','STAGING','QUARANTINE','RETURN','SCRAP')),
    is_blocked BOOLEAN DEFAULT FALSE,
    max_weight DECIMAL(18,4),
    max_volume DECIMAL(18,4),
    capacity_qty DECIMAL(18,4),
    UNIQUE(tenant_id, warehouse_code, location_code)
);

CREATE TABLE stock_transfers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    transfer_number VARCHAR(50) NOT NULL,
    transfer_date DATE DEFAULT CURRENT_DATE,
    from_location_id UUID REFERENCES yuktira_wm.storage_locations(id),
    to_location_id UUID REFERENCES yuktira_wm.storage_locations(id),
    transfer_type VARCHAR(30) DEFAULT 'INTERNAL' CHECK (transfer_type IN ('INTERNAL','INTER_WAREHOUSE','RETURN')),
    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','PICKING','TRANSFERRING','COMPLETED','CANCELLED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, transfer_number)
);

CREATE TABLE stock_transfer_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    transfer_id UUID NOT NULL REFERENCES yuktira_wm.stock_transfers(id) ON DELETE CASCADE,
    line_no INT NOT NULL,
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    quantity DECIMAL(18,4) NOT NULL,
    uom VARCHAR(10) DEFAULT 'EA',
    batch_no VARCHAR(50)
);

CREATE TABLE cycle_counts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    count_number VARCHAR(50) NOT NULL,
    count_date DATE DEFAULT CURRENT_DATE,
    storage_location_id UUID REFERENCES yuktira_wm.storage_locations(id),
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    expected_qty DECIMAL(18,4) NOT NULL,
    counted_qty DECIMAL(18,4),
    variance_qty DECIMAL(18,4) GENERATED ALWAYS AS (counted_qty - expected_qty) STORED,
    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING','IN_PROGRESS','COMPLETED','ADJUSTED')),
    counted_by UUID REFERENCES yuktira_core.users(id),
    counted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, count_number)
);

-- ============================================
-- FI/CO - Finance & Controlling Schema
-- ============================================
SET search_path TO yuktira_fi;

CREATE TABLE gl_accounts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    account_code VARCHAR(50) NOT NULL,
    account_name VARCHAR(200) NOT NULL,
    account_type VARCHAR(30) NOT NULL CHECK (account_type IN ('ASSET','LIABILITY','EQUITY','REVENUE','EXPENSE')),
    account_group VARCHAR(50),
    is_control_account BOOLEAN DEFAULT FALSE,
    currency VARCHAR(10) DEFAULT 'USD',
    is_active BOOLEAN DEFAULT TRUE,
    UNIQUE(tenant_id, account_code)
);

CREATE TABLE cost_centers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    cost_center_code VARCHAR(50) NOT NULL,
    cost_center_name VARCHAR(200) NOT NULL,
    department VARCHAR(100),
    manager_id UUID REFERENCES yuktira_core.users(id),
    is_active BOOLEAN DEFAULT TRUE,
    UNIQUE(tenant_id, cost_center_code)
);

CREATE TABLE fi_documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    document_number VARCHAR(50) NOT NULL,
    document_date DATE NOT NULL,
    posting_date DATE NOT NULL,
    document_type VARCHAR(30) NOT NULL CHECK (document_type IN ('INVOICE','PAYMENT','RECEIPT','JOURNAL','CREDIT_NOTE','DEBIT_NOTE','CONTRA')),
    reference VARCHAR(200),
    header_text TEXT,
    total_debit DECIMAL(18,2) DEFAULT 0,
    total_credit DECIMAL(18,2) DEFAULT 0,
    currency VARCHAR(10) DEFAULT 'USD',
    fiscal_year INT,
    period INT,
    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT','POSTED','REVERSED','CANCELLED')),
    posted_by UUID REFERENCES yuktira_core.users(id),
    posted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, document_number)
);

CREATE TABLE fi_document_lines (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    fi_document_id UUID NOT NULL REFERENCES yuktira_fi.fi_documents(id) ON DELETE CASCADE,
    line_no INT NOT NULL,
    gl_account_id UUID REFERENCES yuktira_fi.gl_accounts(id),
    gl_account_code VARCHAR(50),
    gl_account_name VARCHAR(200),
    cost_center_id UUID REFERENCES yuktira_fi.cost_centers(id),
    debit_amount DECIMAL(18,2) DEFAULT 0,
    credit_amount DECIMAL(18,2) DEFAULT 0,
    text VARCHAR(500),
    partner_type VARCHAR(30),
    partner_id VARCHAR(100),
    partner_name VARCHAR(200)
);

CREATE TABLE ar_ap_aging (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    partner_type VARCHAR(10) NOT NULL CHECK (partner_type IN ('CUSTOMER','VENDOR')),
    partner_id UUID NOT NULL,
    partner_code VARCHAR(50),
    partner_name VARCHAR(200),
    document_number VARCHAR(50),
    document_date DATE,
    due_date DATE,
    amount DECIMAL(18,2),
    open_amount DECIMAL(18,2),
    days_overdue INT,
    aging_bucket VARCHAR(20) GENERATED ALWAYS AS (
        CASE
            WHEN days_overdue <= 0 THEN 'CURRENT'
            WHEN days_overdue <= 30 THEN '1-30'
            WHEN days_overdue <= 60 THEN '31-60'
            WHEN days_overdue <= 90 THEN '61-90'
            ELSE '90+'
        END
    ) STORED,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- HR - Human Resources Schema
-- ============================================
SET search_path TO yuktira_hr;

CREATE TABLE employee_masters (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    employee_code VARCHAR(50) NOT NULL,
    employee_name VARCHAR(200) NOT NULL,
    user_id UUID REFERENCES yuktira_core.users(id),
    department VARCHAR(100),
    designation VARCHAR(100),
    grade VARCHAR(20),
    date_of_joining DATE,
    date_of_birth DATE,
    gender VARCHAR(10),
    email VARCHAR(200),
    phone VARCHAR(50),
    address TEXT,
    bank_name VARCHAR(200),
    bank_account VARCHAR(100),
    pan_number VARCHAR(20),
    pf_number VARCHAR(50),
    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE','SUSPENDED','TERMINATED')),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, employee_code)
);

CREATE TABLE attendance_records (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    employee_id UUID NOT NULL REFERENCES yuktira_hr.employee_masters(id),
    attendance_date DATE NOT NULL,
    check_in TIME,
    check_out TIME,
    hours_worked DECIMAL(5,2),
    status VARCHAR(20) DEFAULT 'PRESENT' CHECK (status IN ('PRESENT','ABSENT','HALF_DAY','LEAVE','HOLIDAY','WEEK_OFF')),
    UNIQUE(employee_id, attendance_date)
);

CREATE TABLE leave_records (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    employee_id UUID NOT NULL REFERENCES yuktira_hr.employee_masters(id),
    leave_type VARCHAR(30) NOT NULL CHECK (leave_type IN ('ANNUAL','SICK','PERSONAL','MATERNITY','PATERNITY','COMP_OFF')),
    from_date DATE NOT NULL,
    to_date DATE NOT NULL,
    total_days DECIMAL(5,2) NOT NULL,
    reason TEXT,
    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING','APPROVED','REJECTED','CANCELLED')),
    approved_by UUID REFERENCES yuktira_core.users(id)
);

-- ============================================
-- CRM - Customer Relationship Management Schema
-- ============================================
SET search_path TO yuktira_crm;

CREATE TABLE crm_leads (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    lead_number VARCHAR(50) NOT NULL,
    company_name VARCHAR(200),
    contact_name VARCHAR(200),
    email VARCHAR(200),
    phone VARCHAR(50),
    source VARCHAR(50) CHECK (source IN ('WEBSITE','REFERRAL','SOCIAL_MEDIA','COLD_CALL','EXHIBITION','OTHER')),
    status VARCHAR(30) DEFAULT 'NEW' CHECK (status IN ('NEW','CONTACTED','QUALIFIED','PROPOSAL','NEGOTIATION','WON','LOST')),
    expected_revenue DECIMAL(18,2),
    notes TEXT,
    assigned_to UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, lead_number)
);

CREATE TABLE crm_opportunities (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    opportunity_number VARCHAR(50) NOT NULL,
    lead_id UUID REFERENCES yuktira_crm.crm_leads(id),
    opportunity_name VARCHAR(200) NOT NULL,
    customer_id UUID REFERENCES yuktira_sd.customer_masters(id),
    expected_close_date DATE,
    stage VARCHAR(30) DEFAULT 'PROSPECTING' CHECK (stage IN ('PROSPECTING','ANALYSIS','PROPOSAL','NEGOTIATION','CLOSED_WON','CLOSED_LOST')),
    probability INT DEFAULT 10,
    expected_amount DECIMAL(18,2),
    assigned_to UUID REFERENCES yuktira_core.users(id),
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, opportunity_number)
);

CREATE TABLE crm_activities (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    activity_type VARCHAR(30) NOT NULL CHECK (activity_type IN ('CALL','EMAIL','MEETING','TASK','NOTE')),
    subject VARCHAR(200) NOT NULL,
    description TEXT,
    lead_id UUID REFERENCES yuktira_crm.crm_leads(id),
    opportunity_id UUID REFERENCES yuktira_crm.crm_opportunities(id),
    customer_id UUID REFERENCES yuktira_sd.customer_masters(id),
    assigned_to UUID REFERENCES yuktira_core.users(id),
    due_date TIMESTAMPTZ,
    status VARCHAR(20) DEFAULT 'OPEN' CHECK (status IN ('OPEN','COMPLETED','CANCELLED')),
    completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- LIMS - Laboratory Information Management Schema
-- ============================================
SET search_path TO yuktira_lims;

CREATE TABLE lab_samples (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    sample_number VARCHAR(50) NOT NULL,
    material_id UUID REFERENCES yuktira_mm.material_masters(id),
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    batch_no VARCHAR(50),
    sample_type VARCHAR(30) DEFAULT 'ROUTINE' CHECK (sample_type IN ('ROUTINE','RANDOM','CUSTOMER','COMPLAINT','VALIDATION')),
    source VARCHAR(30) DEFAULT 'INCOMING' CHECK (source IN ('INCOMING','IN_PROCESS','FINAL','WAREHOUSE','CUSTOMER')),
    source_document_id VARCHAR(100),
    quantity DECIMAL(18,4),
    sampling_date DATE DEFAULT CURRENT_DATE,
    status VARCHAR(30) DEFAULT 'REGISTERED' CHECK (status IN ('REGISTERED','IN_TEST','COMPLETED','APPROVED','REJECTED')),
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, sample_number)
);

CREATE TABLE lab_tests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    test_code VARCHAR(50) NOT NULL,
    test_name VARCHAR(200) NOT NULL,
    test_method VARCHAR(200),
    standard_value VARCHAR(100),
    uom VARCHAR(20),
    is_active BOOLEAN DEFAULT TRUE,
    UNIQUE(tenant_id, test_code)
);

CREATE TABLE lab_sample_tests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    sample_id UUID NOT NULL REFERENCES yuktira_lims.lab_samples(id) ON DELETE CASCADE,
    test_id UUID NOT NULL REFERENCES yuktira_lims.lab_tests(id),
    result_value VARCHAR(200),
    is_acceptable BOOLEAN,
    tested_by UUID REFERENCES yuktira_core.users(id),
    tested_at TIMESTAMPTZ,
    notes TEXT
);

-- ============================================
-- BI - Business Intelligence Views
-- ============================================
SET search_path TO yuktira_bi;

CREATE TABLE bi_reports (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    report_code VARCHAR(50) NOT NULL,
    report_name VARCHAR(200) NOT NULL,
    module VARCHAR(50),
    report_type VARCHAR(30) DEFAULT 'TABLE' CHECK (report_type IN ('TABLE','CHART','PIVOT','SUMMARY')),
    query_definition TEXT,
    parameters_json JSONB,
    is_system BOOLEAN DEFAULT FALSE,
    created_by UUID REFERENCES yuktira_core.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, report_code)
);

CREATE TABLE bi_kpis (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    kpi_code VARCHAR(50) NOT NULL,
    kpi_name VARCHAR(200) NOT NULL,
    module VARCHAR(50),
    query_definition TEXT,
    target_value DECIMAL(18,2),
    threshold_green DECIMAL(5,2),
    threshold_amber DECIMAL(5,2),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, kpi_code)
);

-- ============================================
-- Integration Hub Tables
-- ============================================
SET search_path TO yuktira_integration;

CREATE TABLE webhooks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    target_url VARCHAR(500) NOT NULL,
    http_method VARCHAR(10) DEFAULT 'POST',
    headers JSONB,
    secret_key VARCHAR(200),
    is_active BOOLEAN DEFAULT TRUE,
    retry_count INT DEFAULT 3,
    timeout_seconds INT DEFAULT 30,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE webhook_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    webhook_id UUID NOT NULL REFERENCES yuktira_integration.webhooks(id) ON DELETE CASCADE,
    event_type VARCHAR(100),
    payload JSONB,
    response_status INT,
    response_body TEXT,
    executed_at TIMESTAMPTZ DEFAULT NOW(),
    is_success BOOLEAN
);

CREATE TABLE api_clients (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES yuktira_core.tenants(id) ON DELETE CASCADE,
    client_name VARCHAR(200) NOT NULL,
    client_id VARCHAR(100) NOT NULL UNIQUE,
    client_secret_hash VARCHAR(500) NOT NULL,
    allowed_ips TEXT,
    rate_limit INT DEFAULT 1000,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- MRP Materialized Views
-- ============================================
SET search_path TO yuktira_mrp;

CREATE MATERIALIZED VIEW yuktira_mrp.mrp_stock_view AS
SELECT
    s.tenant_id,
    s.material_id,
    mm.material_code,
    mm.material_name,
    mm.material_type,
    mm.base_uom,
    s.stock_qty,
    s.reserved_qty,
    s.available_qty,
    mm.reorder_level,
    mm.reorder_qty,
    mm.lead_time_days,
    mm.min_stock_qty,
    mm.max_stock_qty
FROM yuktira_mm.stock s
JOIN yuktira_mm.material_masters mm ON s.material_id = mm.id AND s.tenant_id = mm.tenant_id;

CREATE INDEX ON yuktira_mrp.mrp_stock_view(tenant_id, material_id);

CREATE MATERIALIZED VIEW yuktira_mrp.mrp_open_po_view AS
SELECT
    po.tenant_id,
    poi.material_id,
    mm.material_code,
    po.po_number,
    po.po_date,
    poi.line_no,
    poi.quantity - poi.received_qty AS open_qty,
    poi.uom,
    poi.required_date,
    po.status
FROM yuktira_mm.purchase_orders po
JOIN yuktira_mm.purchase_order_items poi ON po.id = poi.po_id
JOIN yuktira_mm.material_masters mm ON poi.material_id = mm.id
WHERE po.status NOT IN ('COMPLETED','CANCELLED');

CREATE INDEX ON yuktira_mrp.mrp_open_po_view(tenant_id, material_id);

-- ============================================
-- MRP Lite Engine Functions
-- ============================================

-- MRP Run: Calculate shortages and generate suggestions
CREATE OR REPLACE FUNCTION yuktira_mrp.run_mrp(
    p_tenant_id UUID,
    p_material_id UUID DEFAULT NULL
) RETURNS TABLE(
    material_id UUID,
    material_code VARCHAR(50),
    material_name VARCHAR(200),
    current_stock DECIMAL(18,4),
    open_po_qty DECIMAL(18,4),
    total_demand DECIMAL(18,4),
    shortage_qty DECIMAL(18,4),
    suggestion_type VARCHAR(20),
    suggested_qty DECIMAL(18,4)
) AS $$
BEGIN
    RETURN QUERY
    WITH stock_data AS (
        SELECT
            s.material_id,
            mm.material_code,
            mm.material_name,
            COALESCE(SUM(s.available_qty), 0) as current_stock,
            mm.reorder_level,
            mm.reorder_qty
        FROM yuktira_mm.material_masters mm
        LEFT JOIN yuktira_mm.stock s ON mm.id = s.material_id AND mm.tenant_id = s.tenant_id
        WHERE mm.tenant_id = p_tenant_id
            AND (p_material_id IS NULL OR mm.id = p_material_id)
        GROUP BY s.material_id, mm.material_code, mm.material_name, mm.reorder_level, mm.reorder_qty
    ),
    open_po AS (
        SELECT
            material_id,
            COALESCE(SUM(open_qty), 0) as open_po_qty
        FROM yuktira_mrp.mrp_open_po_view
        WHERE tenant_id = p_tenant_id
            AND (p_material_id IS NULL OR material_id = p_material_id)
        GROUP BY material_id
    ),
    demand AS (
        SELECT
            soi.material_id,
            COALESCE(SUM(soi.quantity - soi.delivered_qty), 0) as sales_demand
        FROM yuktira_sd.sales_order_items soi
        JOIN yuktira_sd.sales_orders so ON soi.so_id = so.id
        WHERE so.tenant_id = p_tenant_id
            AND so.status IN ('CONFIRMED','DELIVERING')
            AND (p_material_id IS NULL OR soi.material_id = p_material_id)
        GROUP BY soi.material_id
    )
    SELECT
        sd.material_id,
        sd.material_code,
        sd.material_name,
        sd.current_stock,
        COALESCE(op.open_po_qty, 0) as open_po_qty,
        COALESCE(d.sales_demand, 0) as total_demand,
        GREATEST(COALESCE(d.sales_demand, 0) - sd.current_stock - COALESCE(op.open_po_qty, 0), 0) as shortage_qty,
        CASE
            WHEN sd.current_stock <= sd.reorder_level THEN 'PURCHASE'
            WHEN GREATEST(COALESCE(d.sales_demand, 0) - sd.current_stock - COALESCE(op.open_po_qty, 0), 0) > 0 THEN 'PURCHASE'
            ELSE 'NO_ACTION'
        END as suggestion_type,
        CASE
            WHEN GREATEST(COALESCE(d.sales_demand, 0) - sd.current_stock - COALESCE(op.open_po_qty, 0), 0) > 0
            THEN GREATEST(COALESCE(d.sales_demand, 0) - sd.current_stock - COALESCE(op.open_po_qty, 0), 0)
            ELSE sd.reorder_qty
        END as suggested_qty
    FROM stock_data sd
    LEFT JOIN open_po op ON sd.material_id = op.material_id
    LEFT JOIN demand d ON sd.material_id = d.material_id
    WHERE sd.current_stock <= sd.reorder_level
        OR GREATEST(COALESCE(d.sales_demand, 0) - sd.current_stock - COALESCE(op.open_po_qty, 0), 0) > 0
    ORDER BY shortage_qty DESC;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Transaction Code Engine Schema
-- ============================================
SET search_path TO yuktira_transaction;

CREATE TABLE transaction_codes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(10) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    description TEXT DEFAULT '',
    module VARCHAR(10) NOT NULL DEFAULT '',
    group_name VARCHAR(50) NOT NULL DEFAULT 'Transactions',
    route VARCHAR(500) NOT NULL DEFAULT '',
    icon VARCHAR(50) NOT NULL DEFAULT 'bi-asterisk',
    sort_order INT NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    is_system BOOLEAN NOT NULL DEFAULT FALSE,
    required_role VARCHAR(50) NOT NULL DEFAULT 'NORMAL_USER',
    params TEXT NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX idx_tx_code ON transaction_codes(code);
CREATE INDEX idx_tx_module ON transaction_codes(module);
CREATE INDEX idx_tx_group ON transaction_codes(group_name);
CREATE INDEX idx_tx_status ON transaction_codes(status);

CREATE TABLE transaction_permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    transaction_code_id UUID NOT NULL REFERENCES transaction_codes(id) ON DELETE CASCADE,
    principal_type VARCHAR(20) NOT NULL DEFAULT 'Role',
    principal_value VARCHAR(100) NOT NULL DEFAULT '',
    can_access BOOLEAN NOT NULL DEFAULT TRUE,
    is_favorite BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_tp_code ON transaction_permissions(transaction_code_id);
CREATE INDEX idx_tp_principal ON transaction_permissions(principal_type, principal_value);

CREATE TABLE transaction_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    transaction_code_id UUID,
    transaction_code VARCHAR(10) NOT NULL,
    user_id UUID,
    user_name VARCHAR(200) DEFAULT '',
    tenant_id UUID,
    status VARCHAR(20) NOT NULL DEFAULT 'Success',
    ip_address VARCHAR(45) DEFAULT '',
    duration_ms BIGINT NOT NULL DEFAULT 0,
    error_message TEXT,
    request_data TEXT,
    response_data TEXT,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_tl_code ON transaction_logs(transaction_code);
CREATE INDEX idx_tl_user ON transaction_logs(user_id);
CREATE INDEX idx_tl_timestamp ON transaction_logs(timestamp DESC);
CREATE INDEX idx_tl_status ON transaction_logs(status);

-- Forward FK constraints (defined after all tables exist)
ALTER TABLE yuktira_mm.goods_receipt_items
    ADD CONSTRAINT fk_gri_storage_location FOREIGN KEY (storage_location_id) REFERENCES yuktira_wm.storage_locations(id);
ALTER TABLE yuktira_mm.invoice_verifications
    ADD CONSTRAINT fk_iv_fi_document FOREIGN KEY (fi_document_id) REFERENCES yuktira_fi.fi_documents(id);
ALTER TABLE yuktira_sd.billing_documents
    ADD CONSTRAINT fk_bd_fi_document FOREIGN KEY (fi_document_id) REFERENCES yuktira_fi.fi_documents(id);
