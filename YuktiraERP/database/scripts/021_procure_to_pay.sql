-- 021_procure_to_pay.sql
-- Procure-to-Pay enhancements migration

-- Department Keys
CREATE TABLE IF NOT EXISTS "DepartmentKeys" (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(500) DEFAULT '',
    "CostCenterDefault" VARCHAR(50) DEFAULT '',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- Release Strategies
CREATE TABLE IF NOT EXISTS "ReleaseStrategies" (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(500) DEFAULT '',
    "DocumentType" VARCHAR(10) NOT NULL,
    "MinAmount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "MaxAmount" DECIMAL(18,2) NOT NULL DEFAULT 999999999,
    "Plant" VARCHAR(50) DEFAULT '',
    "DepartmentKey" VARCHAR(50) DEFAULT '',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- Release Codes
CREATE TABLE IF NOT EXISTS "ReleaseCodes" (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "ReleaseStrategyId" UUID NOT NULL REFERENCES "ReleaseStrategies"("Id"),
    "Level" INT NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "ApproverRole" VARCHAR(100) DEFAULT '',
    "ApproverUserId" VARCHAR(100) DEFAULT '',
    "IsRequired" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- Purchase Requisition Items
CREATE TABLE IF NOT EXISTS "PurchaseRequisitionItems" (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "PurchaseRequisitionId" UUID NOT NULL REFERENCES "PurchaseRequisitions"("Id"),
    "LineNumber" INT NOT NULL DEFAULT 1,
    "MaterialName" VARCHAR(200) NOT NULL DEFAULT '',
    "MaterialCode" VARCHAR(50) NOT NULL DEFAULT '',
    "Quantity" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "UOM" VARCHAR(20) NOT NULL DEFAULT 'EA',
    "UnitPrice" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "TotalPrice" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "Plant" VARCHAR(50) DEFAULT '',
    "StorageLocation" VARCHAR(50) DEFAULT '',
    "DeliveryDate" VARCHAR(20) DEFAULT '',
    "Status" VARCHAR(20) NOT NULL DEFAULT 'OPEN',
    "DepartmentKey" VARCHAR(50) DEFAULT '',
    "CostCenter" VARCHAR(50) DEFAULT '',
    "Remarks" VARCHAR(500) DEFAULT '',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- Purchase Order Items
CREATE TABLE IF NOT EXISTS "PurchaseOrderItems" (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "PurchaseOrderId" UUID NOT NULL REFERENCES "PurchaseOrders"("Id"),
    "LineNumber" INT NOT NULL DEFAULT 1,
    "MaterialName" VARCHAR(200) NOT NULL DEFAULT '',
    "MaterialCode" VARCHAR(50) NOT NULL DEFAULT '',
    "Quantity" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "UOM" VARCHAR(20) NOT NULL DEFAULT 'EA',
    "UnitPrice" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "TotalPrice" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "Plant" VARCHAR(50) DEFAULT '',
    "StorageLocation" VARCHAR(50) DEFAULT '',
    "DeliveryDate" VARCHAR(20) DEFAULT '',
    "ReceivedQty" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "InvoicedQty" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'OPEN',
    "DepartmentKey" VARCHAR(50) DEFAULT '',
    "CostCenter" VARCHAR(50) DEFAULT '',
    "BatchNo" VARCHAR(50) DEFAULT '',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- Add new columns to PurchaseRequisitions if not exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseRequisitions' AND column_name = 'TenantId') THEN
        ALTER TABLE "PurchaseRequisitions" ADD COLUMN "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseRequisitions' AND column_name = 'DepartmentKey') THEN
        ALTER TABLE "PurchaseRequisitions" ADD COLUMN "DepartmentKey" VARCHAR(50) DEFAULT '';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseRequisitions' AND column_name = 'CostCenter') THEN
        ALTER TABLE "PurchaseRequisitions" ADD COLUMN "CostCenter" VARCHAR(50) DEFAULT '';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseRequisitions' AND column_name = 'TotalAmount') THEN
        ALTER TABLE "PurchaseRequisitions" ADD COLUMN "TotalAmount" DECIMAL(18,2) NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseRequisitions' AND column_name = 'ItemCount') THEN
        ALTER TABLE "PurchaseRequisitions" ADD COLUMN "ItemCount" INT NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseRequisitions' AND column_name = 'ReleaseStatus') THEN
        ALTER TABLE "PurchaseRequisitions" ADD COLUMN "ReleaseStatus" VARCHAR(20) DEFAULT '';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseRequisitions' AND column_name = 'ConvertedPoNumber') THEN
        ALTER TABLE "PurchaseRequisitions" ADD COLUMN "ConvertedPoNumber" VARCHAR(50) DEFAULT '';
    END IF;
END $$;

-- Add new columns to PurchaseOrders if not exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'VendorCode') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "VendorCode" VARCHAR(50) DEFAULT '';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'DepartmentKey') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "DepartmentKey" VARCHAR(50) DEFAULT '';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'CostCenter') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "CostCenter" VARCHAR(50) DEFAULT '';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'TotalAmount') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "TotalAmount" DECIMAL(18,2) NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'ItemCount') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "ItemCount" INT NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'PaymentTerms') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "PaymentTerms" VARCHAR(50) NOT NULL DEFAULT 'Net 30';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'Incoterms') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "Incoterms" VARCHAR(50) DEFAULT '';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PurchaseOrders' AND column_name = 'ReleaseStatus') THEN
        ALTER TABLE "PurchaseOrders" ADD COLUMN "ReleaseStatus" VARCHAR(20) DEFAULT '';
    END IF;
END $$;

-- Add ApproverUserId to ApprovalSteps if not exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ApprovalSteps' AND column_name = 'ApproverUserId') THEN
        ALTER TABLE "ApprovalSteps" ADD COLUMN "ApproverUserId" VARCHAR(100) DEFAULT '';
    END IF;
END $$;

-- Seed Department Keys
INSERT INTO "DepartmentKeys" ("Id", "TenantId", "Code", "Name", "Description", "CostCenterDefault", "IsActive", "CreatedAt")
VALUES
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'PUR', 'Procurement', 'Procurement Department', 'CC-PUR', TRUE, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'MFG', 'Manufacturing', 'Manufacturing Department', 'CC-MFG', TRUE, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'ADM', 'Administration', 'Administration Department', 'CC-ADM', TRUE, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'WH', 'Warehouse', 'Warehouse Department', 'CC-WH', TRUE, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'QCO', 'Quality Control', 'Quality Control Department', 'CC-QCO', TRUE, CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;

-- Seed Release Strategies
INSERT INTO "ReleaseStrategies" ("Id", "TenantId", "Code", "Name", "Description", "DocumentType", "MinAmount", "MaxAmount", "Plant", "DepartmentKey", "IsActive", "CreatedAt")
VALUES
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'RS01', 'Standard PR Approval', 'Standard approval for PR up to 10000', 'PR', 0, 10000, '', '', TRUE, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'RS02', 'High Value PR Approval', 'Approval for PR above 10000', 'PR', 10000, 999999999, '', '', TRUE, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'RS03', 'Standard PO Approval', 'Standard approval for PO up to 50000', 'PO', 0, 50000, '', '', TRUE, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'RS04', 'High Value PO Approval', 'Approval for PO above 50000', 'PO', 50000, 999999999, '', '', TRUE, CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;

-- Seed Release Codes for RS01 (Standard PR)
INSERT INTO "ReleaseCodes" ("Id", "TenantId", "ReleaseStrategyId", "Level", "Code", "ApproverRole", "ApproverUserId", "IsRequired", "CreatedAt")
SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000000', "Id", 1, 'RC01', 'PURCHASER', '', TRUE, CURRENT_TIMESTAMP
FROM "ReleaseStrategies" WHERE "Code" = 'RS01'
ON CONFLICT DO NOTHING;

INSERT INTO "ReleaseCodes" ("Id", "TenantId", "ReleaseStrategyId", "Level", "Code", "ApproverRole", "ApproverUserId", "IsRequired", "CreatedAt")
SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000000', "Id", 2, 'RC02', 'MANAGER', '', TRUE, CURRENT_TIMESTAMP
FROM "ReleaseStrategies" WHERE "Code" = 'RS01'
ON CONFLICT DO NOTHING;

-- Seed Release Codes for RS03 (Standard PO)
INSERT INTO "ReleaseCodes" ("Id", "TenantId", "ReleaseStrategyId", "Level", "Code", "ApproverRole", "ApproverUserId", "IsRequired", "CreatedAt")
SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000000', "Id", 1, 'RC03', 'PURCHASER', '', TRUE, CURRENT_TIMESTAMP
FROM "ReleaseStrategies" WHERE "Code" = 'RS03'
ON CONFLICT DO NOTHING;

INSERT INTO "ReleaseCodes" ("Id", "TenantId", "ReleaseStrategyId", "Level", "Code", "ApproverRole", "ApproverUserId", "IsRequired", "CreatedAt")
SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000000', "Id", 2, 'RC04', 'APPROVER', '', TRUE, CURRENT_TIMESTAMP
FROM "ReleaseStrategies" WHERE "Code" = 'RS03'
ON CONFLICT DO NOTHING;

-- Ensure Number Range definitions for PR and PO
INSERT INTO "NumberRangeDefinitions" ("Id", "TenantId", "Module", "Prefix", "Code", "Name", "NextNumber", "CreatedAt")
VALUES
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'MM', 'PR', 'PR', 'Purchase Requisition Number Range', 1, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', 'MM', 'PO', 'PO', 'Purchase Order Number Range', 1, CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;
