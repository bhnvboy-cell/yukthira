-- Core UoM Engine (T006/CUNI parity)
CREATE SCHEMA IF NOT EXISTS yuktira_core;

CREATE TABLE IF NOT EXISTS yuktira_core.uom_dimensions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    dimension_code VARCHAR(20) NOT NULL,
    si_base_uom VARCHAR(10) NOT NULL,
    long_description VARCHAR(200) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_uom_dim_tenant ON yuktira_core.uom_dimensions(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_uom_dim_code_tenant ON yuktira_core.uom_dimensions(dimension_code, tenant_id);

CREATE TABLE IF NOT EXISTS yuktira_core.units_of_measure (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    msehi VARCHAR(10) NOT NULL,
    iso_code VARCHAR(20) NOT NULL,
    dimension_code VARCHAR(20) NOT NULL,
    numerator DECIMAL(18,6) DEFAULT 1,
    denominator DECIMAL(18,6) DEFAULT 1,
    add_offset DECIMAL(18,6) DEFAULT 0,
    decimals INT DEFAULT 2,
    is_active BOOLEAN DEFAULT true,
    short_text VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_uom_tenant ON yuktira_core.units_of_measure(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_uom_msehi_tenant ON yuktira_core.units_of_measure(msehi, tenant_id);

CREATE TABLE IF NOT EXISTS yuktira_core.material_uom_conversions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    material_code VARCHAR(50) NOT NULL,
    source_uom_code VARCHAR(10) NOT NULL,
    target_uom_code VARCHAR(10) NOT NULL,
    conversion_factor DECIMAL(18,6) NOT NULL,
    density_factor DECIMAL(18,6),
    plant_code VARCHAR(10),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_mat_uom_tenant ON yuktira_core.material_uom_conversions(tenant_id);
CREATE INDEX IF NOT EXISTS idx_mat_uom_material ON yuktira_core.material_uom_conversions(material_code, tenant_id);
