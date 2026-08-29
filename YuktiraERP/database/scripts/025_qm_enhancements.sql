-- ============================================================
-- Migration 025: QM Module Enhancements
-- ============================================================

-- CREATE quality_notifications table (if missing)
CREATE TABLE IF NOT EXISTS "yuktira_qm"."quality_notifications" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "NotificationNumber" VARCHAR(50) NOT NULL,
    "NotificationType" VARCHAR(10) DEFAULT 'Q1',
    "Description" TEXT NOT NULL DEFAULT '',
    "LongText" TEXT DEFAULT '',
    "Plant" VARCHAR(20) DEFAULT '1000',
    "ReferenceDocument" VARCHAR(50) DEFAULT '',
    "ReferenceDocType" VARCHAR(50) DEFAULT '',
    "MaterialCode" VARCHAR(100) DEFAULT '',
    "MaterialName" VARCHAR(200) DEFAULT '',
    "Batch" VARCHAR(50) DEFAULT '',
    "BatchNumber" VARCHAR(50) DEFAULT '',
    "PartnerId" VARCHAR(50) DEFAULT '',
    "PartnerName" VARCHAR(200) DEFAULT '',
    "SubjectCoding" VARCHAR(100) DEFAULT '',
    "DefectLocation" VARCHAR(100) DEFAULT '',
    "DefectCode" VARCHAR(50) DEFAULT '',
    "DefectType" VARCHAR(50) DEFAULT '',
    "CauseCode" VARCHAR(50) DEFAULT '',
    "Impact" VARCHAR(50) DEFAULT 'None',
    "RootCause" TEXT DEFAULT '',
    "Priority" VARCHAR(20) DEFAULT 'Medium',
    "Status" VARCHAR(20) DEFAULT 'NEW',
    "CreatedBy" VARCHAR(100) DEFAULT '',
    "CompletedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- CREATE quality_notification_tasks table (if missing)
CREATE TABLE IF NOT EXISTS "yuktira_qm"."quality_notification_tasks" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "NotificationId" UUID NOT NULL REFERENCES "yuktira_qm"."quality_notifications"("Id"),
    "TaskNumber" VARCHAR(50) NOT NULL DEFAULT '',
    "Description" TEXT DEFAULT '',
    "UserResponsible" VARCHAR(100) DEFAULT '',
    "CompletionText" TEXT DEFAULT '',
    "Status" VARCHAR(20) DEFAULT 'OPEN',
    "CompletedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ALTER InspectionPlanEntity
DO $$ BEGIN
    ALTER TABLE "yuktira_qm"."inspection_plans" ADD COLUMN IF NOT EXISTS "PlanId" VARCHAR(50);
    ALTER TABLE "yuktira_qm"."inspection_plans" ADD COLUMN IF NOT EXISTS "ControlKey" VARCHAR(20);
    ALTER TABLE "yuktira_qm"."inspection_plans" ADD COLUMN IF NOT EXISTS "SamplingProcedure" VARCHAR(50);
    ALTER TABLE "yuktira_qm"."inspection_plans" ADD COLUMN IF NOT EXISTS "MaterialCode" VARCHAR(100);
    ALTER TABLE "yuktira_qm"."inspection_plans" ADD COLUMN IF NOT EXISTS "ValidityStart" TIMESTAMP;
    ALTER TABLE "yuktira_qm"."inspection_plans" ADD COLUMN IF NOT EXISTS "ValidityEnd" TIMESTAMP;
EXCEPTION WHEN duplicate_column THEN NULL;
END $$;

-- ALTER InspectionResultEntity
DO $$ BEGIN
    ALTER TABLE "yuktira_qm"."inspection_results" ADD COLUMN IF NOT EXISTS "BatchNumber" VARCHAR(50);
    ALTER TABLE "yuktira_qm"."inspection_results" ADD COLUMN IF NOT EXISTS "TargetMin" DECIMAL(18,4);
    ALTER TABLE "yuktira_qm"."inspection_results" ADD COLUMN IF NOT EXISTS "TargetMax" DECIMAL(18,4);
    ALTER TABLE "yuktira_qm"."inspection_results" ADD COLUMN IF NOT EXISTS "MeasuredValue" DECIMAL(18,4);
    ALTER TABLE "yuktira_qm"."inspection_results" ADD COLUMN IF NOT EXISTS "Unit" VARCHAR(20);
    ALTER TABLE "yuktira_qm"."inspection_results" ADD COLUMN IF NOT EXISTS "Evaluation" VARCHAR(20);
    ALTER TABLE "yuktira_qm"."inspection_results" ADD COLUMN IF NOT EXISTS "InspectorNotes" TEXT;
    ALTER TABLE "yuktira_qm"."inspection_results" ADD COLUMN IF NOT EXISTS "InspectorID" VARCHAR(50);
EXCEPTION WHEN duplicate_column THEN NULL;
END $$;

-- ALTER UsageDecisionEntity
DO $$ BEGIN
    ALTER TABLE "yuktira_qm"."usage_decisions" ADD COLUMN IF NOT EXISTS "UDCode" VARCHAR(50);
    ALTER TABLE "yuktira_qm"."usage_decisions" ADD COLUMN IF NOT EXISTS "QualityScore" DECIMAL(18,4);
    ALTER TABLE "yuktira_qm"."usage_decisions" ADD COLUMN IF NOT EXISTS "InspectorID" VARCHAR(50);
    ALTER TABLE "yuktira_qm"."usage_decisions" ADD COLUMN IF NOT EXISTS "UnrestrictedStock" DECIMAL(18,4);
    ALTER TABLE "yuktira_qm"."usage_decisions" ADD COLUMN IF NOT EXISTS "BlockedStock" DECIMAL(18,4);
    ALTER TABLE "yuktira_qm"."usage_decisions" ADD COLUMN IF NOT EXISTS "ScrapQuantity" DECIMAL(18,4);
EXCEPTION WHEN duplicate_column THEN NULL;
END $$;

-- ALTER InspectionLotEntity with defaults
DO $$ BEGIN
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "MaterialCode" VARCHAR(100) DEFAULT 'UNKNOWN';
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "Plant" VARCHAR(20) DEFAULT '1000';
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "StorageLocation" VARCHAR(20) DEFAULT '';
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "BatchNumber" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "InspectionType" VARCHAR(20) DEFAULT '01';
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "BaseUOM" VARCHAR(20) DEFAULT 'EA';
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "ReferenceOrderNumber" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "SampleSize" INT DEFAULT 0;
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "InspectionPlanID" VARCHAR(50) DEFAULT '';
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "AssignedInspector" VARCHAR(100) DEFAULT 'Unassigned';
EXCEPTION WHEN duplicate_column THEN NULL;
END $$;

-- Fix existing NULLs
UPDATE "yuktira_qm"."inspection_lots" SET "AssignedInspector" = 'Unassigned' WHERE "AssignedInspector" IS NULL;
UPDATE "yuktira_qm"."inspection_lots" SET "MaterialCode" = 'UNKNOWN' WHERE "MaterialCode" IS NULL;
UPDATE "yuktira_qm"."inspection_lots" SET "Plant" = '1000' WHERE "Plant" IS NULL;
UPDATE "yuktira_qm"."inspection_lots" SET "InspectionType" = '01' WHERE "InspectionType" IS NULL;
UPDATE "yuktira_qm"."inspection_lots" SET "BaseUOM" = 'EA' WHERE "BaseUOM" IS NULL;
UPDATE "yuktira_qm"."inspection_lots" SET "SampleSize" = 0 WHERE "SampleSize" IS NULL;

-- CREATE Certificates of Analysis table
CREATE TABLE IF NOT EXISTS "yuktira_qm"."certificates_of_analysis" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "COANumber" VARCHAR(50) NOT NULL,
    "InspectionLotNumber" VARCHAR(50) NOT NULL,
    "MaterialCode" VARCHAR(100),
    "MaterialName" VARCHAR(200),
    "BatchNumber" VARCHAR(50),
    "Plant" VARCHAR(20) DEFAULT '1000',
    "IssuedBy" VARCHAR(100),
    "IssueDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "CustomerName" VARCHAR(200),
    "CustomerPO" VARCHAR(50),
    "OverallResult" VARCHAR(20) DEFAULT 'Passed',
    "Remarks" TEXT,
    "Status" VARCHAR(20) DEFAULT 'Issued',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS IX_COA_Lot ON "yuktira_qm"."certificates_of_analysis"("InspectionLotNumber");
CREATE INDEX IF NOT EXISTS IX_COA_Batch ON "yuktira_qm"."certificates_of_analysis"("BatchNumber");
CREATE INDEX IF NOT EXISTS IX_COA_Material ON "yuktira_qm"."certificates_of_analysis"("MaterialCode");
CREATE INDEX IF NOT EXISTS IX_COA_Customer ON "yuktira_qm"."certificates_of_analysis"("CustomerName");
CREATE INDEX IF NOT EXISTS IX_COA_Status ON "yuktira_qm"."certificates_of_analysis"("Status");
