-- QM Master Inspection Characteristics
CREATE TABLE IF NOT EXISTS yuktira_qm.mic_master (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    plant_id VARCHAR(10) NOT NULL,
    characteristic_code VARCHAR(30) NOT NULL,
    valid_from TIMESTAMP NOT NULL,
    is_quantitative BOOLEAN NOT NULL DEFAULT false,
    short_text VARCHAR(200) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'BeingCreated',
    lower_spec_limit BOOLEAN DEFAULT false,
    upper_spec_limit BOOLEAN DEFAULT false,
    target_value_required BOOLEAN DEFAULT false,
    results_confirmation VARCHAR(30) NOT NULL DEFAULT 'SingleResult',
    requirement VARCHAR(30) NOT NULL DEFAULT 'RequiredCharc',
    decimal_places INT DEFAULT 2,
    lower_tolerance DECIMAL(18,6),
    upper_tolerance DECIMAL(18,6),
    target_value DECIMAL(18,6),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_mic_tenant ON yuktira_qm.mic_master(tenant_id);
CREATE INDEX IF NOT EXISTS idx_mic_plant ON yuktira_qm.mic_master(plant_id, tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_mic_composite ON yuktira_qm.mic_master(plant_id, characteristic_code, tenant_id);
CREATE INDEX IF NOT EXISTS idx_mic_status ON yuktira_qm.mic_master(status, tenant_id);
