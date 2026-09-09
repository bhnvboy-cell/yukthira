-- UD Reversal & Stock Reversion Engine tables
-- Phase 4.2

CREATE TABLE IF NOT EXISTS yuktira_mm.stock_balances (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    "TenantId" UUID NOT NULL,
    "MaterialCode" TEXT NOT NULL DEFAULT '',
    "MaterialName" TEXT NOT NULL DEFAULT '',
    "Plant" TEXT NOT NULL DEFAULT '1000',
    "StorageLocation" TEXT NOT NULL DEFAULT '',
    "BatchNumber" TEXT NOT NULL DEFAULT '',
    "StockType" TEXT NOT NULL DEFAULT 'Unrestricted',
    "Quantity" NUMERIC(18,4) NOT NULL DEFAULT 0,
    "UOM" TEXT NOT NULL DEFAULT 'EA',
    "UnitPrice" NUMERIC(18,4) NOT NULL DEFAULT 0,
    "TotalValue" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "Status" TEXT NOT NULL DEFAULT 'Active'
);

CREATE INDEX IF NOT EXISTS idx_stock_balances_tenant ON yuktira_mm.stock_balances("TenantId");
CREATE INDEX IF NOT EXISTS idx_stock_balances_material ON yuktira_mm.stock_balances("MaterialCode");
CREATE INDEX IF NOT EXISTS idx_stock_balances_batch ON yuktira_mm.stock_balances("BatchNumber");
CREATE INDEX IF NOT EXISTS idx_stock_balances_stock_type ON yuktira_mm.stock_balances("StockType");

CREATE TABLE IF NOT EXISTS yuktira_qm.inspection_lot_audits (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    "TenantId" UUID NOT NULL,
    "InspectionLotId" UUID NOT NULL,
    "LotNumber" TEXT NOT NULL DEFAULT '',
    "Action" TEXT NOT NULL DEFAULT '',
    "PreviousStatus" TEXT NOT NULL DEFAULT '',
    "NewStatus" TEXT NOT NULL DEFAULT '',
    "PreviousUDCode" TEXT NOT NULL DEFAULT '',
    "NewUDCode" TEXT NOT NULL DEFAULT '',
    "StockMovementType" TEXT NOT NULL DEFAULT '',
    "StockQuantityMoved" NUMERIC(18,4) NOT NULL DEFAULT 0,
    "StockFromLocation" TEXT NOT NULL DEFAULT '',
    "StockToLocation" TEXT NOT NULL DEFAULT '',
    "Reason" TEXT NOT NULL DEFAULT '',
    "UserId" TEXT NOT NULL DEFAULT '',
    "ActionTimestamp" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Notes" TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS idx_inspection_lot_audits_tenant ON yuktira_qm.inspection_lot_audits("TenantId");
CREATE INDEX IF NOT EXISTS idx_inspection_lot_audits_lot ON yuktira_qm.inspection_lot_audits("InspectionLotId");
CREATE INDEX IF NOT EXISTS idx_inspection_lot_audits_action ON yuktira_qm.inspection_lot_audits("Action");
CREATE INDEX IF NOT EXISTS idx_inspection_lot_audits_timestamp ON yuktira_qm.inspection_lot_audits("ActionTimestamp");

COMMENT ON TABLE yuktira_mm.stock_balances IS 'Stock type balances per material/batch for QM stock reversion';
COMMENT ON TABLE yuktira_qm.inspection_lot_audits IS 'Audit trail for UD reversals and inspection lot actions';
