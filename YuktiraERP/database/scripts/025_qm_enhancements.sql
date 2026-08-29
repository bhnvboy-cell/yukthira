-- ============================================================
-- Migration 025: QM Module Enhancements
-- ============================================================

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

-- ALTER InspectionLotEntity
DO $$ BEGIN
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "MaterialCode" VARCHAR(100);
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "Plant" VARCHAR(20);
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "StorageLocation" VARCHAR(20);
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "BatchNumber" VARCHAR(50);
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "InspectionType" VARCHAR(20);
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "BaseUOM" VARCHAR(20);
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "ReferenceOrderNumber" VARCHAR(50);
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "SampleSize" INT;
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "InspectionPlanID" VARCHAR(50);
    ALTER TABLE "yuktira_qm"."inspection_lots" ADD COLUMN IF NOT EXISTS "AssignedInspector" VARCHAR(100);
EXCEPTION WHEN duplicate_column THEN NULL;
END $$;

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
