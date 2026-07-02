-- BI Engine Extensions
-- Adds columns for BIReportEntity, KPI snapshots, and dashboard widgets

-- Add columns to existing bi_reports table (column is query_definition in 001_core_schema)
ALTER TABLE yuktira_bi.bi_reports ADD COLUMN IF NOT EXISTS query_definition TEXT NOT NULL DEFAULT '';
ALTER TABLE yuktira_bi.bi_reports ADD COLUMN IF NOT EXISTS chart_type VARCHAR(50) NOT NULL DEFAULT 'bar';
ALTER TABLE yuktira_bi.bi_reports ADD COLUMN IF NOT EXISTS filter_json TEXT NOT NULL DEFAULT '{}';

-- KPI Snapshots table
CREATE TABLE IF NOT EXISTS yuktira_bi.kpi_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    kpi_code VARCHAR(100) NOT NULL,
    value DECIMAL(18,4) NOT NULL DEFAULT 0,
    snapshot_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_kpi_snapshots_tenant ON yuktira_bi.kpi_snapshots(tenant_id);
CREATE INDEX IF NOT EXISTS idx_kpi_snapshots_code ON yuktira_bi.kpi_snapshots(tenant_id, kpi_code);
CREATE INDEX IF NOT EXISTS idx_kpi_snapshots_time ON yuktira_bi.kpi_snapshots(tenant_id, snapshot_at DESC);

-- Dashboard widgets instance table
CREATE TABLE IF NOT EXISTS yuktira_dashboard.dashboard_widget_instances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    dashboard_id UUID NOT NULL,
    widget_code VARCHAR(100) NOT NULL,
    widget_type VARCHAR(50) NOT NULL DEFAULT 'KPI',
    title VARCHAR(200) NOT NULL DEFAULT '',
    config_json TEXT NOT NULL DEFAULT '{}',
    position_x INT NOT NULL DEFAULT 0,
    position_y INT NOT NULL DEFAULT 0,
    width INT NOT NULL DEFAULT 4,
    height INT NOT NULL DEFAULT 2,
    is_visible BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_dashboard_widgets_dash ON yuktira_dashboard.dashboard_widget_instances(dashboard_id);
