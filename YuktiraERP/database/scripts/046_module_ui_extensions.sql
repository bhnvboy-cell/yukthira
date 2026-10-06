-- 046_module_ui_extensions.sql
-- Tenant scoping for QM inspection data (SPC dashboard) + interchange log fields (EDI Workbench)

ALTER TABLE yuktira_qm.inspection_results ADD COLUMN IF NOT EXISTS "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE yuktira_qm.inspection_lots ADD COLUMN IF NOT EXISTS "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE public."EdiTransmissions" ADD COLUMN IF NOT EXISTS "Direction" text NOT NULL DEFAULT '';
ALTER TABLE public."EdiTransmissions" ADD COLUMN IF NOT EXISTS "PartnerCode" text NOT NULL DEFAULT '';
ALTER TABLE public."EdiTransmissions" ADD COLUMN IF NOT EXISTS "DocumentType" text NOT NULL DEFAULT '';
