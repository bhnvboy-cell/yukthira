-- 020_production_planning.sql
-- Production Planning lifecycle and MRP execution enhancements

-- Add new columns to production_orders table
ALTER TABLE IF EXISTS yuktira_pp.production_orders
    ADD COLUMN IF NOT EXISTS bom_id UUID NULL,
    ADD COLUMN IF NOT EXISTS routing_id UUID NULL,
    ADD COLUMN IF NOT EXISTS batch_no VARCHAR(100) NULL,
    ADD COLUMN IF NOT EXISTS scrap_qty DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS yield_qty DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS actual_cost DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS planned_cost DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS released_at TIMESTAMP NULL,
    ADD COLUMN IF NOT EXISTS confirmed_at TIMESTAMP NULL,
    ADD COLUMN IF NOT EXISTS tecod_at TIMESTAMP NULL,
    ADD COLUMN IF NOT EXISTS cancelled_at TIMESTAMP NULL,
    ADD COLUMN IF NOT EXISTS release_by VARCHAR(255) NULL,
    ADD COLUMN IF NOT EXISTS confirm_by VARCHAR(255) NULL;

-- Create production_order_items table for multi-component tracking
CREATE TABLE IF NOT EXISTS yuktira_pp.production_order_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    production_order_id UUID NOT NULL,
    material_name VARCHAR(255) NOT NULL,
    required_qty DECIMAL(18,2) NOT NULL DEFAULT 0,
    issued_qty DECIMAL(18,2) NOT NULL DEFAULT 0,
    scrap_qty DECIMAL(18,2) NOT NULL DEFAULT 0,
    uom VARCHAR(20) NOT NULL DEFAULT 'EA',
    status VARCHAR(50) NOT NULL DEFAULT 'PLANNED',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

-- Create material_staging table
CREATE TABLE IF NOT EXISTS yuktira_pp.material_stagings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    production_order_id UUID NOT NULL,
    material_name VARCHAR(255) NOT NULL,
    required_qty DECIMAL(18,2) NOT NULL DEFAULT 0,
    staged_qty DECIMAL(18,2) NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    notes VARCHAR(500) NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

-- Enhance stock_movements table
ALTER TABLE IF EXISTS yuktira_mm.stock_movements
    ADD COLUMN IF NOT EXISTS document_number VARCHAR(100) NULL,
    ADD COLUMN IF NOT EXISTS stock_before DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS stock_after DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS reference VARCHAR(255) NULL,
    ADD COLUMN IF NOT EXISTS status VARCHAR(50) NOT NULL DEFAULT 'Posted';

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_production_order_items_order ON yuktira_pp.production_order_items(production_order_id);
CREATE INDEX IF NOT EXISTS idx_production_order_items_material ON yuktira_pp.production_order_items(material_name);
CREATE INDEX IF NOT EXISTS idx_material_stagings_order ON yuktira_pp.material_stagings(production_order_id);
CREATE INDEX IF NOT EXISTS idx_material_stagings_status ON yuktira_pp.material_stagings(status);
CREATE INDEX IF NOT EXISTS idx_stock_movements_material ON yuktira_mm.stock_movements(material_name);
CREATE INDEX IF NOT EXISTS idx_stock_movements_reference ON yuktira_mm.stock_movements(reference);
CREATE INDEX IF NOT EXISTS idx_stock_movements_status ON yuktira_mm.stock_movements(status);

-- Add status index to production_orders for lifecycle queries
CREATE INDEX IF NOT EXISTS idx_production_orders_status ON yuktira_pp.production_orders(status);
CREATE INDEX IF NOT EXISTS idx_production_orders_bom ON yuktira_pp.production_orders(bom_id);
CREATE INDEX IF NOT EXISTS idx_production_orders_routing ON yuktira_pp.production_orders(routing_id);
