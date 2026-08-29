-- 024_pp_enhancements.sql
-- PP Module: Extended SAP PP parameters + Order Confirmation entity

-- Extend BOM with SAP fields (table: bill_of_materials)
ALTER TABLE IF EXISTS yuktira_pp.bill_of_materials
    ADD COLUMN IF NOT EXISTS "MaterialCode" VARCHAR(50) NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "ComponentCode" VARCHAR(50) NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "BOMUsage" VARCHAR(50) NOT NULL DEFAULT 'Production',
    ADD COLUMN IF NOT EXISTS "BaseQuantity" DECIMAL(18,4) NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "ItemCategory" VARCHAR(10) NOT NULL DEFAULT 'L',
    ADD COLUMN IF NOT EXISTS "ComponentScrap" DECIMAL(5,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ValidFrom" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS "ValidTo" TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP + INTERVAL '5 years');

-- Extend WorkCenters with SAP fields (table: work_centers)
ALTER TABLE IF EXISTS yuktira_pp.work_centers
    ADD COLUMN IF NOT EXISTS "ShiftsPerDay" INTEGER NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "CapacityPerDay" DECIMAL(18,4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "WorkCenterCategory" VARCHAR(50) NOT NULL DEFAULT 'Machine',
    ADD COLUMN IF NOT EXISTS "CostCenter" VARCHAR(50) NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "MachineTimeHrs" DECIMAL(18,4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "LaborTimeHrs" DECIMAL(18,4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ResponsiblePerson" VARCHAR(100) NULL DEFAULT '';

-- Extend Routings with SAP fields (table: production_routings)
ALTER TABLE IF EXISTS yuktira_pp.production_routings
    ADD COLUMN IF NOT EXISTS "RoutingGroup" VARCHAR(50) NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "RoutingGroupCounter" INTEGER NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "OperationDescription" VARCHAR(200) NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "MachineTimeHrs" DECIMAL(18,4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "LaborTimeHrs" DECIMAL(18,4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "QueueTimeHrs" DECIMAL(18,4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ScrapPercent" DECIMAL(5,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "ControlKey" VARCHAR(10) NOT NULL DEFAULT 'PP01';

-- Extend ProductionOrders with SAP fields (table: production_orders)
ALTER TABLE IF EXISTS yuktira_pp.production_orders
    ADD COLUMN IF NOT EXISTS "OrderType" VARCHAR(20) NOT NULL DEFAULT 'PP01',
    ADD COLUMN IF NOT EXISTS "MaterialCode" VARCHAR(50) NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "BaseUOM" VARCHAR(10) NOT NULL DEFAULT 'EA',
    ADD COLUMN IF NOT EXISTS "Plant" VARCHAR(10) NOT NULL DEFAULT '1000',
    ADD COLUMN IF NOT EXISTS "StorageLocation" VARCHAR(20) NOT NULL DEFAULT 'RM01',
    ADD COLUMN IF NOT EXISTS "MRPController" VARCHAR(50) NULL DEFAULT '';

-- Indexes for new columns
CREATE INDEX IF NOT EXISTS idx_bom_material ON yuktira_pp.bill_of_materials("MaterialCode");
CREATE INDEX IF NOT EXISTS idx_bom_usage ON yuktira_pp.bill_of_materials("BOMUsage");
CREATE INDEX IF NOT EXISTS idx_workcenter_category ON yuktira_pp.work_centers("WorkCenterCategory");
CREATE INDEX IF NOT EXISTS idx_routing_group ON yuktira_pp.production_routings("RoutingGroup");
CREATE INDEX IF NOT EXISTS idx_prodorder_type ON yuktira_pp.production_orders("OrderType");
CREATE INDEX IF NOT EXISTS idx_prodorder_material ON yuktira_pp.production_orders("MaterialCode");
CREATE INDEX IF NOT EXISTS idx_confirmation_number ON yuktira_pp."OrderConfirmations"("ConfirmationNumber");
CREATE INDEX IF NOT EXISTS idx_confirmation_order ON yuktira_pp."OrderConfirmations"("ProductionOrderNumber");
CREATE INDEX IF NOT EXISTS idx_confirmation_date ON yuktira_pp."OrderConfirmations"("ConfirmationDate");
