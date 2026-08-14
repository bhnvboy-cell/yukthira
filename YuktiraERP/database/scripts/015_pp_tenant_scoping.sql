-- 015_pp_tenant_scoping.sql
-- Add TenantId to Production-Planning module tables and backfill existing rows to the first tenant.

DO $$
DECLARE
    v_tenant_id uuid := (SELECT "Id" FROM yuktira_core."Tenants" ORDER BY "CreatedAt" LIMIT 1);
BEGIN
    ALTER TABLE yuktira_core."ProductionPlans" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
    ALTER TABLE yuktira_core."BillOfMaterials" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
    ALTER TABLE yuktira_core."ProductionRoutings" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
    ALTER TABLE yuktira_core."WorkCenters" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
    ALTER TABLE yuktira_core."ProductionOrders" ADD COLUMN IF NOT EXISTS "TenantId" uuid;

    UPDATE yuktira_core."ProductionPlans" SET "TenantId" = v_tenant_id WHERE "TenantId" IS NULL;
    UPDATE yuktira_core."BillOfMaterials" SET "TenantId" = v_tenant_id WHERE "TenantId" IS NULL;
    UPDATE yuktira_core."ProductionRoutings" SET "TenantId" = v_tenant_id WHERE "TenantId" IS NULL;
    UPDATE yuktira_core."WorkCenters" SET "TenantId" = v_tenant_id WHERE "TenantId" IS NULL;
    UPDATE yuktira_core."ProductionOrders" SET "TenantId" = v_tenant_id WHERE "TenantId" IS NULL;

    ALTER TABLE yuktira_core."ProductionPlans" ALTER COLUMN "TenantId" SET NOT NULL;
    ALTER TABLE yuktira_core."BillOfMaterials" ALTER COLUMN "TenantId" SET NOT NULL;
    ALTER TABLE yuktira_core."ProductionRoutings" ALTER COLUMN "TenantId" SET NOT NULL;
    ALTER TABLE yuktira_core."WorkCenters" ALTER COLUMN "TenantId" SET NOT NULL;
    ALTER TABLE yuktira_core."ProductionOrders" ALTER COLUMN "TenantId" SET NOT NULL;

    CREATE INDEX IF NOT EXISTS IX_ProductionPlans_TenantId ON yuktira_core."ProductionPlans" ("TenantId");
    CREATE INDEX IF NOT EXISTS IX_BillOfMaterials_TenantId ON yuktira_core."BillOfMaterials" ("TenantId");
    CREATE INDEX IF NOT EXISTS IX_ProductionRoutings_TenantId ON yuktira_core."ProductionRoutings" ("TenantId");
    CREATE INDEX IF NOT EXISTS IX_WorkCenters_TenantId ON yuktira_core."WorkCenters" ("TenantId");
    CREATE INDEX IF NOT EXISTS IX_ProductionOrders_TenantId ON yuktira_core."ProductionOrders" ("TenantId");
END $$;
