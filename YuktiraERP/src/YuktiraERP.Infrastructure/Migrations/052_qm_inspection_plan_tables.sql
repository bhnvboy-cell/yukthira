-- QM Inspection Plan & Characteristic Assignment Engine
-- Migration 052: inspection_plan_headers, inspection_plan_operations, inspection_plan_characteristics, mic_master

CREATE TABLE IF NOT EXISTS yuktira_qm.mic_master (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    characteristic_code VARCHAR(30) NOT NULL,
    short_text VARCHAR(200) NOT NULL,
    plant_id VARCHAR(10) NOT NULL,
    version INT DEFAULT 1,
    is_quantitative BOOLEAN NOT NULL,
    inspection_method_code VARCHAR(20) DEFAULT '',
    inspection_method_version INT,
    sampling_procedure_code VARCHAR(20) DEFAULT '',
    control_key VARCHAR(20) DEFAULT '',
    status VARCHAR(20) DEFAULT 'Released',
    valid_from TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    valid_to TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_mic_master_tenant ON yuktira_qm.mic_master(tenant_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_mic_master_code_plant ON yuktira_qm.mic_master(characteristic_code, plant_id);
CREATE INDEX IF NOT EXISTS idx_mic_master_status ON yuktira_qm.mic_master(status);

CREATE TABLE IF NOT EXISTS yuktira_qm.inspection_plan_headers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    group_key VARCHAR(50) NOT NULL,
    group_counter INT DEFAULT 1,
    plant_id VARCHAR(10),
    material_id VARCHAR(50),
    usage VARCHAR(5) DEFAULT '5',
    overall_status VARCHAR(5) DEFAULT '4',
    lot_size_from DECIMAL(18,6) DEFAULT 0,
    lot_size_to DECIMAL(18,6) DEFAULT 999999,
    description TEXT,
    valid_from TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    valid_to TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_ip_header_tenant ON yuktira_qm.inspection_plan_headers(tenant_id);
CREATE INDEX IF NOT EXISTS idx_ip_header_plant_mat ON yuktira_qm.inspection_plan_headers(plant_id, material_id, tenant_id);
CREATE INDEX IF NOT EXISTS idx_ip_header_group ON yuktira_qm.inspection_plan_headers(group_key, group_counter);

CREATE TABLE IF NOT EXISTS yuktira_qm.inspection_plan_operations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    plan_header_id UUID NOT NULL REFERENCES yuktira_qm.inspection_plan_headers(id),
    operation_no VARCHAR(5) NOT NULL,
    operation_description VARCHAR(200) NOT NULL,
    work_center VARCHAR(20),
    base_quantity DECIMAL(18,6) DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_ip_op_tenant ON yuktira_qm.inspection_plan_operations(tenant_id);
CREATE INDEX IF NOT EXISTS idx_ip_op_header ON yuktira_qm.inspection_plan_operations(plan_header_id);

CREATE TABLE IF NOT EXISTS yuktira_qm.inspection_plan_characteristics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    plan_header_id UUID NOT NULL REFERENCES yuktira_qm.inspection_plan_headers(id),
    operation_id UUID NOT NULL REFERENCES yuktira_qm.inspection_plan_operations(id),
    characteristic_no INT NOT NULL,
    mic_code VARCHAR(30) NOT NULL,
    mic_plant_id VARCHAR(10) NOT NULL,
    mic_version INT DEFAULT 1,
    is_quantitative BOOLEAN NOT NULL,
    short_text VARCHAR(200) NOT NULL,
    inspection_method_code VARCHAR(20),
    inspection_method_version INT,
    sampling_procedure_code VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_ip_mic_tenant ON yuktira_qm.inspection_plan_characteristics(tenant_id);
CREATE INDEX IF NOT EXISTS idx_ip_mic_header ON yuktira_qm.inspection_plan_characteristics(plan_header_id);
CREATE INDEX IF NOT EXISTS idx_ip_mic_operation ON yuktira_qm.inspection_plan_characteristics(operation_id);
CREATE INDEX IF NOT EXISTS idx_ip_mic_header_op_no ON yuktira_qm.inspection_plan_characteristics(plan_header_id, operation_id, characteristic_no);
