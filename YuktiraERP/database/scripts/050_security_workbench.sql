-- =============================================================================
-- 050_security_workbench.sql
-- Security Workbench features:
--   (1) Authorization decision tracing (Authorization Trace)
--   (3) Security Guard & Navigation Middleware trace storage
--   (2) Transaction Code Authorization Mapping (action-level checks)
--   (6) SOX / Segregation of Duties seed duties + conflict rules
--
-- New schema: yuktira_security
--   yuktira_security.authorization_traces  - allow/deny decisions from the
--       navigation guard, t-code launch guard and API access checks.
--   yuktira_security.tcode_auth_checks     - action-level authorization
--       mapping per t-code (CREATE/CHANGE/DELETE/DISPLAY/APPROVE/POST with
--       required role + Enforced/DisplayOnly flag).
-- Seeds: public."SoxDuties" SoD duties with ConflictDuties JSON pairs and
--   tcode_auth_checks for high-value transaction codes.
--
-- Columns follow the EF Core model for
--   YuktiraERP.Infrastructure.Data.Entities.AuthorizationTraceEntity and
--   TCodeAuthCheckEntity (all identifiers quoted, snake_case tables).
-- Idempotent: safe to run more than once.
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS yuktira_security;

CREATE TABLE IF NOT EXISTS yuktira_security.authorization_traces (
    "Id"            uuid PRIMARY KEY,
    "TenantId"      uuid NOT NULL,
    "UserId"        uuid NULL,
    "UserName"      text NOT NULL DEFAULT '',
    "Role"          text NOT NULL DEFAULT '',
    "SessionId"     text NOT NULL DEFAULT '',
    "CorrelationId" text NOT NULL DEFAULT '',
    "ResourceType"  text NOT NULL DEFAULT 'Page',
    "Resource"      text NOT NULL DEFAULT '',
    "Decision"      text NOT NULL DEFAULT 'Allow',
    "RuleSource"    text NOT NULL DEFAULT '',
    "Reason"        text NOT NULL DEFAULT '',
    "HttpMethod"    text NOT NULL DEFAULT '',
    "HttpPath"      text NOT NULL DEFAULT '',
    "IpAddress"     text NOT NULL DEFAULT '',
    "UserAgent"     text NOT NULL DEFAULT '',
    "CreatedAt"     timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt"     timestamp NULL
);

CREATE INDEX IF NOT EXISTS ix_authz_trace_tenant_created
    ON yuktira_security.authorization_traces ("TenantId", "CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS ix_authz_trace_tenant_decision
    ON yuktira_security.authorization_traces ("TenantId", "Decision");
CREATE INDEX IF NOT EXISTS ix_authz_trace_user
    ON yuktira_security.authorization_traces ("UserId");

CREATE TABLE IF NOT EXISTS yuktira_security.tcode_auth_checks (
    "Id"           uuid PRIMARY KEY,
    "TenantId"     uuid NOT NULL,
    "TCode"        text NOT NULL,
    "CheckCode"    text NOT NULL,
    "ActionType"   text NOT NULL DEFAULT 'DISPLAY',
    "RequiredRole" text NOT NULL DEFAULT 'NORMAL_USER',
    "Enforcement"  text NOT NULL DEFAULT 'Enforced',
    "Description"  text NOT NULL DEFAULT '',
    "IsActive"     boolean NOT NULL DEFAULT true,
    "CreatedAt"    timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt"    timestamp NULL
);

CREATE INDEX IF NOT EXISTS ix_tcode_auth_checks_tcode
    ON yuktira_security.tcode_auth_checks ("TCode", "TenantId");
CREATE INDEX IF NOT EXISTS ix_tcode_auth_checks_tenant
    ON yuktira_security.tcode_auth_checks ("TenantId");

-- =============================================================================
-- SOX SoD duties + conflict rules (public."SoxDuties")
-- ConflictDuties is a JSON array of duty codes that conflict with this duty.
-- TransactionCode links the duty to a real t-code for t-code-derived scans.
-- =============================================================================
INSERT INTO public."SoxDuties"
    ("Id", "TenantId", "DutyCode", "DutyName", "Description", "Module",
     "TransactionCode", "ActionType", "MinApprovers", "RequiredRoles",
     "ConflictDuties", "IsActive", "EffectiveFrom", "EffectiveTo",
     "CreatedAt", "UpdatedAt")
SELECT * FROM (VALUES
    ('a5000001-0000-4000-8000-000000000001'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'PO_CREATE', 'Create Purchase Order', 'Create purchase orders in the procurement cycle', 'MM', 'ME21N', 'CREATE', 1, '["NORMAL_USER"]', '["PO_APPROVE","GOODS_RECEIPT","VENDOR_CREATE"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000002'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'PO_APPROVE', 'Release / Approve Purchase Order', 'Release or approve purchase orders', 'MM', 'ME28', 'APPROVE', 2, '["POWER_USER"]', '["PO_CREATE"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000003'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'PR_CREATE', 'Create Purchase Requisition', 'Create purchase requisitions', 'MM', 'ME51N', 'CREATE', 1, '["NORMAL_USER"]', '["PO_APPROVE"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000004'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'GOODS_RECEIPT', 'Post Goods Receipt', 'Post goods receipts against purchase orders', 'MM', 'MIGO', 'RECEIVE', 1, '["NORMAL_USER"]', '["INVOICE_POST","PO_CREATE"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000005'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'INVOICE_POST', 'Post Vendor Invoice (LIV)', 'Post vendor invoices / invoice verification', 'FI', 'MIRO', 'POST', 1, '["POWER_USER"]', '["GOODS_RECEIPT","VENDOR_CREATE","PAYMENT_EXECUTE"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000006'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'PAYMENT_EXECUTE', 'Execute Vendor Payment', 'Execute outgoing vendor payments', 'FI', 'F-53', 'PAYMENT', 2, '["POWER_USER"]', '["VENDOR_CREATE","INVOICE_POST","JOURNAL_POST"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000007'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'JOURNAL_POST', 'Post Journal Entry', 'Post general journal entries', 'FI', 'FB50', 'POST', 1, '["POWER_USER"]', '["PAYMENT_EXECUTE"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000008'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'VENDOR_CREATE', 'Create / Maintain Vendor Master', 'Create or change vendor master records', 'MM', 'ME11', 'CREATE', 1, '["NORMAL_USER"]', '["PAYMENT_EXECUTE","INVOICE_POST","PO_CREATE"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000009'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'USER_ADMIN', 'User Administration', 'Create / maintain user accounts and access', 'ADM', 'SU01', 'ADMIN', 1, '["ADMIN"]', '["COMPLIANCE_ADMIN","AUDIT_REVIEW"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000010'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'COMPLIANCE_ADMIN', 'SOX Compliance Administration', 'Maintain SoD rules, duties and violations', 'ADM', 'SOXADM', 'ADMIN', 1, '["ADMIN"]', '["USER_ADMIN"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000011'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'AUDIT_REVIEW', 'Audit Log Review', 'Review and flag audit trail entries', 'AUD', '', 'DISPLAY', 1, '["ADMIN"]', '["USER_ADMIN","JOURNAL_POST"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000012'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'SALES_ORDER_CREATE', 'Create Sales Order', 'Create customer sales orders', 'SD', 'VA01', 'CREATE', 1, '["NORMAL_USER"]', '["BILLING_CREATE"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000001-0000-4000-8000-000000000013'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'BILLING_CREATE', 'Create Billing Document', 'Create billing documents / invoices for deliveries', 'SD', 'VF01', 'CREATE', 1, '["POWER_USER"]', '["SALES_ORDER_CREATE"]', true, CURRENT_DATE, NULL::timestamp, CURRENT_TIMESTAMP, NULL::timestamp)
) AS v("Id","TenantId","DutyCode","DutyName","Description","Module",
       "TransactionCode","ActionType","MinApprovers","RequiredRoles",
       "ConflictDuties","IsActive","EffectiveFrom","EffectiveTo",
       "CreatedAt","UpdatedAt")
WHERE NOT EXISTS (
    SELECT 1 FROM public."SoxDuties" d WHERE d."DutyCode" = v."DutyCode"
);

-- =============================================================================
-- Transaction code authorization checks (action-level mapping)
-- Enforcement='Enforced' rows are evaluated by
-- TransactionCodeService.ValidateAccessAsync on t-code launch / execute.
-- =============================================================================
INSERT INTO yuktira_security.tcode_auth_checks
    ("Id", "TenantId", "TCode", "CheckCode", "ActionType", "RequiredRole",
     "Enforcement", "Description", "IsActive", "CreatedAt", "UpdatedAt")
SELECT * FROM (VALUES
    ('a5000002-0000-4000-8000-000000000001'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'ME21N', 'PO_CREATE', 'CREATE', 'NORMAL_USER', 'Enforced', 'Create purchase orders', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000002'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'ME28', 'PO_APPROVE', 'APPROVE', 'POWER_USER', 'Enforced', 'Releasing purchase orders requires power user rank', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000003'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'ME51N', 'PR_CREATE', 'CREATE', 'NORMAL_USER', 'Enforced', 'Create purchase requisitions', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000004'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'MIGO', 'GOODS_RECEIPT', 'RECEIVE', 'NORMAL_USER', 'Enforced', 'Post goods receipts', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000005'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'MIRO', 'INVOICE_POST', 'POST', 'POWER_USER', 'Enforced', 'Invoice verification posting requires power user rank', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000006'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'F-53', 'PAYMENT_EXECUTE', 'PAYMENT', 'POWER_USER', 'Enforced', 'Vendor outgoing payments require power user rank', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000007'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'FB50', 'JOURNAL_POST', 'POST', 'POWER_USER', 'Enforced', 'Journal entry posting requires power user rank', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000008'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'SU01', 'USER_ADMIN', 'ADMIN', 'ADMIN', 'Enforced', 'User administration restricted to admins', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000009'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'SOXADM', 'COMPLIANCE_ADMIN', 'ADMIN', 'ADMIN', 'Enforced', 'Compliance administration restricted to admins', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000010'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'VA01', 'SALES_ORDER_CREATE', 'CREATE', 'NORMAL_USER', 'Enforced', 'Create sales orders', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000011'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'VF01', 'BILLING_CREATE', 'CREATE', 'POWER_USER', 'Enforced', 'Billing document creation requires power user rank', true, CURRENT_TIMESTAMP, NULL::timestamp),
    ('a5000002-0000-4000-8000-000000000012'::uuid, '2eba0f9c-d565-440c-8440-044c68681110'::uuid, 'SU02', 'USER_ADMIN', 'ADMIN', 'ADMIN', 'DisplayOnly', 'Password administration documentation', true, CURRENT_TIMESTAMP, NULL::timestamp)
) AS v("Id","TenantId","TCode","CheckCode","ActionType","RequiredRole",
       "Enforcement","Description","IsActive","CreatedAt","UpdatedAt")
WHERE NOT EXISTS (
    SELECT 1 FROM yuktira_security.tcode_auth_checks c
    WHERE c."TCode" = v."TCode" AND c."CheckCode" = v."CheckCode" AND c."TenantId" = v."TenantId"
);

-- Verify:
--   SELECT count(*) FROM yuktira_security.authorization_traces;
--   SELECT "TCode","CheckCode","ActionType","RequiredRole","Enforcement"
--     FROM yuktira_security.tcode_auth_checks ORDER BY "TCode";
--   SELECT "DutyCode","TransactionCode","ConflictDuties"
--     FROM public."SoxDuties" WHERE "IsActive" ORDER BY "DutyCode";
