-- ============================================================
-- Migration 026: PM Module Enhancements
-- ============================================================

-- CREATE functional_locations table
CREATE TABLE IF NOT EXISTS "yuktira_pm"."functional_locations" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "LocationCode" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL DEFAULT '',
    "Description" TEXT DEFAULT '',
    "LocationType" VARCHAR(50) DEFAULT 'Functional',
    "ParentLocationCode" VARCHAR(50) DEFAULT '',
    "Plant" VARCHAR(20) DEFAULT '1000',
    "CostCenter" VARCHAR(50) DEFAULT '',
    "MaintPlannerGroup" VARCHAR(50) DEFAULT '',
    "Status" VARCHAR(20) DEFAULT 'Active',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_FL_Code ON "yuktira_pm"."functional_locations"("LocationCode");

-- CREATE maintenance_notifications table
CREATE TABLE IF NOT EXISTS "yuktira_pm"."maintenance_notifications" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "NotificationNumber" VARCHAR(50) NOT NULL,
    "NotificationType" VARCHAR(10) DEFAULT 'M1',
    "EquipmentCode" VARCHAR(50) DEFAULT '',
    "FunctionalLocationCode" VARCHAR(50) DEFAULT '',
    "FaultCode" VARCHAR(50) DEFAULT '',
    "FaultGroup" VARCHAR(50) DEFAULT '',
    "BreakdownFlag" BOOLEAN DEFAULT FALSE,
    "Description" TEXT DEFAULT '',
    "Priority" VARCHAR(20) DEFAULT 'Medium',
    "ReportedBy" VARCHAR(100) DEFAULT '',
    "IncidentTime" TIMESTAMP NOT NULL DEFAULT NOW(),
    "Plant" VARCHAR(20) DEFAULT '1000',
    "Status" VARCHAR(20) DEFAULT 'NEW',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_MN_Number ON "yuktira_pm"."maintenance_notifications"("NotificationNumber");

-- CREATE spare_parts table
CREATE TABLE IF NOT EXISTS "yuktira_pm"."spare_parts" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MaterialCode" VARCHAR(50) NOT NULL,
    "MaterialName" VARCHAR(200) NOT NULL DEFAULT '',
    "Description" TEXT DEFAULT '',
    "EquipmentCode" VARCHAR(50) DEFAULT '',
    "Plant" VARCHAR(20) DEFAULT '1000',
    "StorageLocation" VARCHAR(20) DEFAULT '',
    "RequiredQuantity" DECIMAL(18,4) DEFAULT 0,
    "IssuedQuantity" DECIMAL(18,4) DEFAULT 0,
    "UnitPrice" DECIMAL(18,4) DEFAULT 0,
    "UOM" VARCHAR(10) DEFAULT 'EA',
    "OrderNumber" VARCHAR(50) DEFAULT '',
    "Status" VARCHAR(20) DEFAULT 'Reserved',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ALTER equipment table with SAP fields
DO $$ BEGIN
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "Description" TEXT DEFAULT '';
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "Category" VARCHAR(10) DEFAULT 'M';
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "SerialNumber" VARCHAR(100) DEFAULT '';
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "Manufacturer" VARCHAR(200) DEFAULT '';
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "Model" VARCHAR(100) DEFAULT '';
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "InstallationDate" TIMESTAMP;
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "FunctionalLocationCode" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "Plant" VARCHAR(20) DEFAULT '1000';
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "WorkCenter" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_pm"."equipment" ADD COLUMN IF NOT EXISTS "CostCenter" VARCHAR(50) DEFAULT '';
EXCEPTION WHEN duplicate_column THEN NULL;
END $$;

-- ALTER maintenance_orders table with SAP fields
DO $$ BEGIN
    ALTER TABLE "yuktira_pm"."maintenance_orders" ADD COLUMN IF NOT EXISTS "OrderType" VARCHAR(10) DEFAULT 'PM01';
    ALTER TABLE "yuktira_pm"."maintenance_orders" ADD COLUMN IF NOT EXISTS "FunctionalLocationCode" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_pm"."maintenance_orders" ADD COLUMN IF NOT EXISTS "WorkCenter" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_pm"."maintenance_orders" ADD COLUMN IF NOT EXISTS "CostCenter" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_pm"."maintenance_orders" ADD COLUMN IF NOT EXISTS "ScheduledStartDate" TIMESTAMP;
    ALTER TABLE "yuktira_pm"."maintenance_orders" ADD COLUMN IF NOT EXISTS "ScheduledFinishDate" TIMESTAMP;
    ALTER TABLE "yuktira_pm"."maintenance_orders" ADD COLUMN IF NOT EXISTS "PlannedHours" DECIMAL(18,4) DEFAULT 0;
    ALTER TABLE "yuktira_pm"."maintenance_orders" ADD COLUMN IF NOT EXISTS "ActualHours" DECIMAL(18,4) DEFAULT 0;
EXCEPTION WHEN duplicate_column THEN NULL;
END $$;

-- ALTER maintenance_plans table with SAP fields
DO $$ BEGIN
    ALTER TABLE "yuktira_pm"."maintenance_plans" ADD COLUMN IF NOT EXISTS "FunctionalLocationCode" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_pm"."maintenance_plans" ADD COLUMN IF NOT EXISTS "PlanCategory" VARCHAR(50) DEFAULT 'Time-Based';
    ALTER TABLE "yuktira_pm"."maintenance_plans" ADD COLUMN IF NOT EXISTS "WorkCenter" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_pm"."maintenance_plans" ADD COLUMN IF NOT EXISTS "MaintPlannerGroup" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_pm"."maintenance_plans" ADD COLUMN IF NOT EXISTS "NextDueDate" TIMESTAMP;
EXCEPTION WHEN duplicate_column THEN NULL;
END $$;

-- Fix existing NULLs for new equipment columns
UPDATE "yuktira_pm"."equipment" SET "Category" = 'M' WHERE "Category" IS NULL;
UPDATE "yuktira_pm"."equipment" SET "Plant" = '1000' WHERE "Plant" IS NULL;
UPDATE "yuktira_pm"."equipment" SET "FunctionalLocationCode" = '' WHERE "FunctionalLocationCode" IS NULL;
UPDATE "yuktira_pm"."equipment" SET "WorkCenter" = '' WHERE "WorkCenter" IS NULL;
UPDATE "yuktira_pm"."equipment" SET "CostCenter" = '' WHERE "CostCenter" IS NULL;
UPDATE "yuktira_pm"."equipment" SET "Description" = '' WHERE "Description" IS NULL;
UPDATE "yuktira_pm"."equipment" SET "SerialNumber" = '' WHERE "SerialNumber" IS NULL;
UPDATE "yuktira_pm"."equipment" SET "Manufacturer" = '' WHERE "Manufacturer" IS NULL;
UPDATE "yuktira_pm"."equipment" SET "Model" = '' WHERE "Model" IS NULL;

-- Fix existing NULLs for new maintenance_orders columns
UPDATE "yuktira_pm"."maintenance_orders" SET "OrderType" = 'PM01' WHERE "OrderType" IS NULL;
UPDATE "yuktira_pm"."maintenance_orders" SET "FunctionalLocationCode" = '' WHERE "FunctionalLocationCode" IS NULL;
UPDATE "yuktira_pm"."maintenance_orders" SET "WorkCenter" = '' WHERE "WorkCenter" IS NULL;
UPDATE "yuktira_pm"."maintenance_orders" SET "CostCenter" = '' WHERE "CostCenter" IS NULL;

-- Fix existing NULLs for new maintenance_plans columns
UPDATE "yuktira_pm"."maintenance_plans" SET "PlanCategory" = 'Time-Based' WHERE "PlanCategory" IS NULL;
UPDATE "yuktira_pm"."maintenance_plans" SET "WorkCenter" = '' WHERE "WorkCenter" IS NULL;
UPDATE "yuktira_pm"."maintenance_plans" SET "MaintPlannerGroup" = '' WHERE "MaintPlannerGroup" IS NULL;
