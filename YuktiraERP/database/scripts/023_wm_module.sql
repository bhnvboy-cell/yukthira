-- 023_wm_module.sql
-- WM Module: Storage Bins, Transfer Orders, Waves, Physical Inventory
-- Also adds WM columns to stock_movements

-- Storage Bins
CREATE TABLE IF NOT EXISTS yuktira_wm."Bins" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "WarehouseNumber" VARCHAR(20) NOT NULL DEFAULT '',
    "StorageType" VARCHAR(50) NOT NULL DEFAULT '',
    "StorageSection" VARCHAR(50) NOT NULL DEFAULT '',
    "BinCode" VARCHAR(50) NOT NULL DEFAULT '',
    "MaxWeight" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "MaxVolume" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "PutawayStrategy" VARCHAR(50) NOT NULL DEFAULT 'Open',
    "BinType" VARCHAR(50) NOT NULL DEFAULT 'Standard',
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Active',
    "CurrentWeight" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "CurrentVolume" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL
);

-- Transfer Orders
CREATE TABLE IF NOT EXISTS yuktira_wm."TransferOrders" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "OrderNumber" VARCHAR(50) NOT NULL DEFAULT '',
    "MaterialCode" VARCHAR(50) NOT NULL DEFAULT '',
    "MaterialName" VARCHAR(200) NOT NULL DEFAULT '',
    "SourceBin" VARCHAR(50) NOT NULL DEFAULT '',
    "DestinationBin" VARCHAR(50) NOT NULL DEFAULT '',
    "Quantity" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "BaseUOM" VARCHAR(10) NOT NULL DEFAULT 'EA',
    "TargetBatch" VARCHAR(50) NOT NULL DEFAULT '',
    "MovementType" VARCHAR(10) NOT NULL DEFAULT '999',
    "AssignedOperator" VARCHAR(100) NOT NULL DEFAULT '',
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Created',
    "OrderDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL
);

-- Waves (Picking Waves)
CREATE TABLE IF NOT EXISTS yuktira_wm."Waves" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "WaveNumber" VARCHAR(50) NOT NULL DEFAULT '',
    "DeliveryNotes" VARCHAR(500) NOT NULL DEFAULT '',
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "AssignedZone" VARCHAR(50) NOT NULL DEFAULT '',
    "CutoffTime" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ItemCount" INTEGER NOT NULL DEFAULT 0,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Planned',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL
);

-- Physical Inventory Counts
CREATE TABLE IF NOT EXISTS yuktira_wm."InventoryCounts" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CountNumber" VARCHAR(50) NOT NULL DEFAULT '',
    "WarehouseNumber" VARCHAR(20) NOT NULL DEFAULT '',
    "StorageType" VARCHAR(50) NOT NULL DEFAULT '',
    "BinRange" VARCHAR(100) NOT NULL DEFAULT '',
    "CountType" VARCHAR(20) NOT NULL DEFAULT 'Full',
    "AssignedTo" VARCHAR(100) NOT NULL DEFAULT '',
    "ScheduledDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "TotalBins" INTEGER NOT NULL DEFAULT 0,
    "CountedBins" INTEGER NOT NULL DEFAULT 0,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Planned',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL
);

-- Add WM columns to stock_movements
ALTER TABLE IF EXISTS yuktira_mm.stock_movements
    ADD COLUMN IF NOT EXISTS material_code VARCHAR(50) NULL,
    ADD COLUMN IF NOT EXISTS source_bin VARCHAR(50) NULL,
    ADD COLUMN IF NOT EXISTS destination_bin VARCHAR(50) NULL,
    ADD COLUMN IF NOT EXISTS uom VARCHAR(10) NULL DEFAULT 'EA',
    ADD COLUMN IF NOT EXISTS batch_number VARCHAR(50) NULL,
    ADD COLUMN IF NOT EXISTS posted_by VARCHAR(100) NULL,
    ADD COLUMN IF NOT EXISTS movement_date TIMESTAMP NULL,
    ADD COLUMN IF NOT EXISTS movement_number VARCHAR(50) NULL;

-- Indexes
CREATE INDEX IF NOT EXISTS idx_bins_warehouse ON yuktira_wm."Bins"("WarehouseNumber");
CREATE INDEX IF NOT EXISTS idx_bins_code ON yuktira_wm."Bins"("BinCode");
CREATE INDEX IF NOT EXISTS idx_transfer_orders_number ON yuktira_wm."TransferOrders"("OrderNumber");
CREATE INDEX IF NOT EXISTS idx_transfer_orders_status ON yuktira_wm."TransferOrders"("Status");
CREATE INDEX IF NOT EXISTS idx_waves_number ON yuktira_wm."Waves"("WaveNumber");
CREATE INDEX IF NOT EXISTS idx_inventory_counts_number ON yuktira_wm."InventoryCounts"("CountNumber");
CREATE INDEX IF NOT EXISTS idx_stock_movements_material_code ON yuktira_mm.stock_movements(material_code);
CREATE INDEX IF NOT EXISTS idx_stock_movements_movement_number ON yuktira_mm.stock_movements(movement_number);
