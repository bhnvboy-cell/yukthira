-- MRP Engine Extensions
-- Adds tables for MRP run history, exception messages, plants, vendor lead times, and capacity leveling

CREATE SCHEMA IF NOT EXISTS yuktira_mrp;

CREATE TABLE IF NOT EXISTS yuktira_mrp.mrp_run_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    run_type VARCHAR(50) NOT NULL DEFAULT '',
    run_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    status VARCHAR(50) NOT NULL DEFAULT 'Completed',
    materials_processed INTEGER NOT NULL DEFAULT 0,
    suggestions_generated INTEGER NOT NULL DEFAULT 0,
    exception_messages INTEGER NOT NULL DEFAULT 0,
    parameters TEXT NOT NULL DEFAULT '{}',
    duration_ms BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS yuktira_mrp.mrp_exception_message (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    run_history_id UUID,
    material_code VARCHAR(100) NOT NULL DEFAULT '',
    material_name VARCHAR(500) NOT NULL DEFAULT '',
    exception_type VARCHAR(100) NOT NULL DEFAULT '',
    message TEXT NOT NULL DEFAULT '',
    severity VARCHAR(50) NOT NULL DEFAULT 'Error',
    suggested_action TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS yuktira_mrp.plant (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    code VARCHAR(50) NOT NULL DEFAULT '',
    name VARCHAR(200) NOT NULL DEFAULT '',
    location VARCHAR(200) NOT NULL DEFAULT '',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS yuktira_mrp.vendor_lead_time (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    vendor_id UUID NOT NULL,
    material_code VARCHAR(100) NOT NULL DEFAULT '',
    lead_time_days INTEGER NOT NULL DEFAULT 0,
    reliability DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS yuktira_mrp.mrp_capacity_level (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    run_history_id UUID,
    work_center_code VARCHAR(100) NOT NULL DEFAULT '',
    available_hours DECIMAL(18,2) NOT NULL DEFAULT 0,
    required_hours DECIMAL(18,2) NOT NULL DEFAULT 0,
    load_percent DECIMAL(5,1) NOT NULL DEFAULT 0,
    leveling_suggestion TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mrp_run_history_tenant ON yuktira_mrp.mrp_run_history(tenant_id);
CREATE INDEX IF NOT EXISTS idx_mrp_run_history_run_at ON yuktira_mrp.mrp_run_history(tenant_id, run_at DESC);
CREATE INDEX IF NOT EXISTS idx_mrp_exception_tenant ON yuktira_mrp.mrp_exception_message(tenant_id);
CREATE INDEX IF NOT EXISTS idx_mrp_exception_run ON yuktira_mrp.mrp_exception_message(run_history_id);
CREATE INDEX IF NOT EXISTS idx_mrp_exception_type ON yuktira_mrp.mrp_exception_message(exception_type);
CREATE INDEX IF NOT EXISTS idx_plant_tenant ON yuktira_mrp.plant(tenant_id);
CREATE INDEX IF NOT EXISTS idx_plant_code ON yuktira_mrp.plant(tenant_id, code);
CREATE INDEX IF NOT EXISTS idx_vendor_lead_time_tenant ON yuktira_mrp.vendor_lead_time(tenant_id);
CREATE INDEX IF NOT EXISTS idx_vendor_lead_time_material ON yuktira_mrp.vendor_lead_time(tenant_id, material_code);
CREATE INDEX IF NOT EXISTS idx_mrp_capacity_tenant ON yuktira_mrp.mrp_capacity_level(tenant_id);
CREATE INDEX IF NOT EXISTS idx_mrp_capacity_run ON yuktira_mrp.mrp_capacity_level(run_history_id);
CREATE INDEX IF NOT EXISTS idx_mrp_capacity_work_center ON yuktira_mrp.mrp_capacity_level(tenant_id, work_center_code);
