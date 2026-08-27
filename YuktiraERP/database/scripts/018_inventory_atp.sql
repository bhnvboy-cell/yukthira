-- 018_inventory_atp.sql
-- ATP/CTP Inventory Availability Check tables

CREATE TABLE IF NOT EXISTS yuktira_mm.stock_reservations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    material_id UUID NOT NULL,
    material_name VARCHAR(255) NOT NULL,
    quantity DECIMAL(18,4) NOT NULL,
    order_id UUID NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Active',
    reserved_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    released_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS yuktira_mm.stock_allocations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    material_id UUID NOT NULL,
    material_name VARCHAR(255) NOT NULL,
    quantity DECIMAL(18,4) NOT NULL,
    allocation_type VARCHAR(100) NOT NULL,
    reference VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Allocated',
    allocated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

CREATE INDEX IF NOT EXISTS idx_stock_reservations_material ON yuktira_mm.stock_reservations(material_id);
CREATE INDEX IF NOT EXISTS idx_stock_reservations_status ON yuktira_mm.stock_reservations(status);
CREATE INDEX IF NOT EXISTS idx_stock_allocations_material ON yuktira_mm.stock_allocations(material_id);
CREATE INDEX IF NOT EXISTS idx_stock_allocations_status ON yuktira_mm.stock_allocations(status);

-- Add reserved_quantity column to stock table if not exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'yuktira_mm' 
                   AND table_name = 'stock_items' 
                   AND column_name = 'reserved_quantity') THEN
        ALTER TABLE yuktira_mm.stock_items ADD COLUMN reserved_quantity DECIMAL(18,4) NOT NULL DEFAULT 0;
    END IF;
END $$;
