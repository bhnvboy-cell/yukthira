-- Migration 017: Batch & Serial Lifecycle Management
-- Adds batch tracking, serial numbers, batch movements, and recall management

-- Batches table
CREATE TABLE IF NOT EXISTS yuktira_mm.batches (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    batch_number VARCHAR(50) NOT NULL,
    material_id UUID NOT NULL,
    material_name VARCHAR(200) NOT NULL DEFAULT '',
    manufacturing_date TIMESTAMP NOT NULL,
    expiry_date TIMESTAMP,
    shelf_life_days INTEGER,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    quantity_consumed DECIMAL(18,4) NOT NULL DEFAULT 0,
    unit_of_measure VARCHAR(10) NOT NULL DEFAULT 'EA',
    storage_location_id UUID,
    storage_location_name VARCHAR(100) NOT NULL DEFAULT '',
    supplier_id UUID,
    supplier_name VARCHAR(200) NOT NULL DEFAULT '',
    certificate_of_analysis TEXT NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_batches_tenant ON yuktira_mm.batches(tenant_id);
CREATE INDEX IF NOT EXISTS idx_batches_batch_number ON yuktira_mm.batches(batch_number);
CREATE INDEX IF NOT EXISTS idx_batches_material_id ON yuktira_mm.batches(material_id);
CREATE INDEX IF NOT EXISTS idx_batches_expiry_date ON yuktira_mm.batches(expiry_date);
CREATE INDEX IF NOT EXISTS idx_batches_status ON yuktira_mm.batches(status);

-- Serial Numbers table
CREATE TABLE IF NOT EXISTS yuktira_mm.serial_numbers (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    serial_number VARCHAR(50) NOT NULL,
    material_id UUID NOT NULL,
    material_name VARCHAR(200) NOT NULL DEFAULT '',
    batch_id UUID,
    batch_number VARCHAR(50) NOT NULL DEFAULT '',
    manufacturing_date TIMESTAMP NOT NULL,
    warranty_expiry_date TIMESTAMP,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    current_owner_id UUID,
    current_owner_name VARCHAR(200) NOT NULL DEFAULT '',
    po_reference VARCHAR(50) NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_serial_numbers_tenant ON yuktira_mm.serial_numbers(tenant_id);
CREATE INDEX IF NOT EXISTS idx_serial_numbers_serial_number ON yuktira_mm.serial_numbers(serial_number);
CREATE INDEX IF NOT EXISTS idx_serial_numbers_material_id ON yuktira_mm.serial_numbers(material_id);
CREATE INDEX IF NOT EXISTS idx_serial_numbers_batch_id ON yuktira_mm.serial_numbers(batch_id);
CREATE INDEX IF NOT EXISTS idx_serial_numbers_status ON yuktira_mm.serial_numbers(status);

-- Batch Movements table
CREATE TABLE IF NOT EXISTS yuktira_mm.batch_movements (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    batch_id UUID NOT NULL,
    batch_number VARCHAR(50) NOT NULL DEFAULT '',
    movement_type VARCHAR(20) NOT NULL,
    quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    from_location VARCHAR(100) NOT NULL DEFAULT '',
    to_location VARCHAR(100) NOT NULL DEFAULT '',
    document_number VARCHAR(50) NOT NULL DEFAULT '',
    movement_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    user_id UUID NOT NULL,
    user_name VARCHAR(200) NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_batch_movements_tenant ON yuktira_mm.batch_movements(tenant_id);
CREATE INDEX IF NOT EXISTS idx_batch_movements_batch_id ON yuktira_mm.batch_movements(batch_id);
CREATE INDEX IF NOT EXISTS idx_batch_movements_movement_type ON yuktira_mm.batch_movements(movement_type);
CREATE INDEX IF NOT EXISTS idx_batch_movements_movement_date ON yuktira_mm.batch_movements(movement_date);

-- Batch Recalls table
CREATE TABLE IF NOT EXISTS yuktira_mm.batch_recalls (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    recall_number VARCHAR(50) NOT NULL,
    reason TEXT NOT NULL DEFAULT '',
    affected_batch_ids JSONB NOT NULL DEFAULT '[]',
    affected_batch_numbers VARCHAR(500) NOT NULL DEFAULT '',
    initiated_by UUID NOT NULL,
    initiated_by_name VARCHAR(200) NOT NULL DEFAULT '',
    initiated_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) NOT NULL DEFAULT 'OPEN',
    resolution_notes TEXT NOT NULL DEFAULT '',
    resolved_date TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_batch_recalls_tenant ON yuktira_mm.batch_recalls(tenant_id);
CREATE INDEX IF NOT EXISTS idx_batch_recalls_recall_number ON yuktira_mm.batch_recalls(recall_number);
CREATE INDEX IF NOT EXISTS idx_batch_recalls_status ON yuktira_mm.batch_recalls(status);
