-- 028_hr_crm_module_upgrade.sql
-- HR & CRM module upgrade: new entities, extended schemas, NULL defaults

-- ============================================================
-- HR MODULE
-- ============================================================

-- Extend employee_masters with SAP HR fields
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "PersonnelNumber" VARCHAR(50) DEFAULT '';
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "CompanyCode" VARCHAR(50) DEFAULT '';
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "PersonnelArea" VARCHAR(100) DEFAULT '';
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "OrgUnitCode" VARCHAR(50) DEFAULT '';
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "Position" VARCHAR(100) DEFAULT '';
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "EmployeeGroup" VARCHAR(50) DEFAULT 'Permanent';
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "BasicSalary" DECIMAL(18,2) DEFAULT 0;
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "CostCenter" VARCHAR(50) DEFAULT '';
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "PaymentMethod" VARCHAR(50) DEFAULT 'Bank Transfer';
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "Email" VARCHAR(200) DEFAULT '';
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "DateOfBirth" TIMESTAMP NULL;
ALTER TABLE yuktira_hr.employee_masters ADD COLUMN IF NOT EXISTS "JoiningDate" TIMESTAMP NULL;

UPDATE yuktira_hr.employee_masters SET "PersonnelNumber" = '' WHERE "PersonnelNumber" IS NULL;
UPDATE yuktira_hr.employee_masters SET "CompanyCode" = '' WHERE "CompanyCode" IS NULL;
UPDATE yuktira_hr.employee_masters SET "PersonnelArea" = '' WHERE "PersonnelArea" IS NULL;
UPDATE yuktira_hr.employee_masters SET "OrgUnitCode" = '' WHERE "OrgUnitCode" IS NULL;
UPDATE yuktira_hr.employee_masters SET "Position" = '' WHERE "Position" IS NULL;
UPDATE yuktira_hr.employee_masters SET "EmployeeGroup" = 'Permanent' WHERE "EmployeeGroup" IS NULL;
UPDATE yuktira_hr.employee_masters SET "BasicSalary" = 0 WHERE "BasicSalary" IS NULL;
UPDATE yuktira_hr.employee_masters SET "CostCenter" = '' WHERE "CostCenter" IS NULL;
UPDATE yuktira_hr.employee_masters SET "PaymentMethod" = 'Bank Transfer' WHERE "PaymentMethod" IS NULL;
UPDATE yuktira_hr.employee_masters SET "Email" = '' WHERE "Email" IS NULL;

-- Create org_units table
CREATE TABLE IF NOT EXISTS yuktira_hr.org_units (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "UnitCode" VARCHAR(50) NOT NULL DEFAULT '',
    "UnitName" VARCHAR(200) NOT NULL DEFAULT '',
    "UnitType" VARCHAR(50) NOT NULL DEFAULT 'Department',
    "ParentUnit" VARCHAR(100) NOT NULL DEFAULT '',
    "Manager" VARCHAR(200) NOT NULL DEFAULT '',
    "CostCenter" VARCHAR(50) NOT NULL DEFAULT '',
    "Headcount" INT NOT NULL DEFAULT 0,
    "Location" VARCHAR(200) NOT NULL DEFAULT '',
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Active',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NULL
);

-- Create time_entries table
CREATE TABLE IF NOT EXISTS yuktira_hr.time_entries (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "EntryId" VARCHAR(50) NOT NULL DEFAULT '',
    "EmployeeCode" VARCHAR(50) NOT NULL DEFAULT '',
    "EmployeeName" VARCHAR(200) NOT NULL DEFAULT '',
    "EntryDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "EntryType" VARCHAR(50) NOT NULL DEFAULT 'Work',
    "Hours" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "ProjectCode" VARCHAR(50) NOT NULL DEFAULT '',
    "Description" VARCHAR(500) NOT NULL DEFAULT '',
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Submitted',
    "ApprovedBy" VARCHAR(200) NOT NULL DEFAULT '',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NULL
);

-- Create recruitments table
CREATE TABLE IF NOT EXISTS yuktira_hr.recruitments (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "RequisitionId" VARCHAR(50) NOT NULL DEFAULT '',
    "JobTitle" VARCHAR(200) NOT NULL DEFAULT '',
    "Department" VARCHAR(100) NOT NULL DEFAULT '',
    "Position" VARCHAR(100) NOT NULL DEFAULT '',
    "Headcount" INT NOT NULL DEFAULT 1,
    "HiringManager" VARCHAR(200) NOT NULL DEFAULT '',
    "Priority" VARCHAR(50) NOT NULL DEFAULT 'Normal',
    "EmploymentType" VARCHAR(50) NOT NULL DEFAULT 'Full-Time',
    "MinSalary" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "MaxSalary" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "Justification" VARCHAR(500) NOT NULL DEFAULT '',
    "RequestedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "TargetDate" TIMESTAMP NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Open',
    "ApplicantsCount" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NULL
);

-- ============================================================
-- CRM MODULE
-- ============================================================

-- Extend crm_opportunities with SAP CRM fields
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "AccountId" VARCHAR(50) DEFAULT '';
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "ContactPerson" VARCHAR(200) DEFAULT '';
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "SalesOrg" VARCHAR(50) DEFAULT '';
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "DistributionChannel" VARCHAR(50) DEFAULT '';
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "SalesStage" VARCHAR(50) DEFAULT 'Prospecting';
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "ClosingProbability" DECIMAL(5,2) DEFAULT 0;
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "TargetValue" DECIMAL(18,2) DEFAULT 0;
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "ExpectedCloseDate" TIMESTAMP NULL;
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "ProductItems" TEXT DEFAULT '';
ALTER TABLE yuktira_crm.crm_opportunities ADD COLUMN IF NOT EXISTS "AssignedTo" VARCHAR(200) DEFAULT '';

UPDATE yuktira_crm.crm_opportunities SET "AccountId" = '' WHERE "AccountId" IS NULL;
UPDATE yuktira_crm.crm_opportunities SET "ContactPerson" = '' WHERE "ContactPerson" IS NULL;
UPDATE yuktira_crm.crm_opportunities SET "SalesOrg" = '' WHERE "SalesOrg" IS NULL;
UPDATE yuktira_crm.crm_opportunities SET "DistributionChannel" = '' WHERE "DistributionChannel" IS NULL;
UPDATE yuktira_crm.crm_opportunities SET "SalesStage" = 'Prospecting' WHERE "SalesStage" IS NULL;
UPDATE yuktira_crm.crm_opportunities SET "ClosingProbability" = 0 WHERE "ClosingProbability" IS NULL;
UPDATE yuktira_crm.crm_opportunities SET "TargetValue" = 0 WHERE "TargetValue" IS NULL;
UPDATE yuktira_crm.crm_opportunities SET "ProductItems" = '' WHERE "ProductItems" IS NULL;
UPDATE yuktira_crm.crm_opportunities SET "AssignedTo" = '' WHERE "AssignedTo" IS NULL;

-- Extend crm_service_tickets with SAP fields
ALTER TABLE yuktira_crm.crm_service_tickets ADD COLUMN IF NOT EXISTS "TicketType" VARCHAR(50) DEFAULT 'Complaint';
ALTER TABLE yuktira_crm.crm_service_tickets ADD COLUMN IF NOT EXISTS "Category" VARCHAR(100) DEFAULT '';
ALTER TABLE yuktira_crm.crm_service_tickets ADD COLUMN IF NOT EXISTS "AssignedTo" VARCHAR(200) DEFAULT '';
ALTER TABLE yuktira_crm.crm_service_tickets ADD COLUMN IF NOT EXISTS "Resolution" TEXT DEFAULT '';
ALTER TABLE yuktira_crm.crm_service_tickets ADD COLUMN IF NOT EXISTS "ResolvedDate" TIMESTAMP NULL;
ALTER TABLE yuktira_crm.crm_service_tickets ADD COLUMN IF NOT EXISTS "CustomerEmail" VARCHAR(200) DEFAULT '';
ALTER TABLE yuktira_crm.crm_service_tickets ADD COLUMN IF NOT EXISTS "CustomerPhone" VARCHAR(50) DEFAULT '';

UPDATE yuktira_crm.crm_service_tickets SET "TicketType" = 'Complaint' WHERE "TicketType" IS NULL;
UPDATE yuktira_crm.crm_service_tickets SET "Category" = '' WHERE "Category" IS NULL;
UPDATE yuktira_crm.crm_service_tickets SET "AssignedTo" = '' WHERE "AssignedTo" IS NULL;
UPDATE yuktira_crm.crm_service_tickets SET "Resolution" = '' WHERE "Resolution" IS NULL;
UPDATE yuktira_crm.crm_service_tickets SET "CustomerEmail" = '' WHERE "CustomerEmail" IS NULL;
UPDATE yuktira_crm.crm_service_tickets SET "CustomerPhone" = '' WHERE "CustomerPhone" IS NULL;

-- Create crm_accounts table
CREATE TABLE IF NOT EXISTS yuktira_crm.crm_accounts (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "AccountId" VARCHAR(50) NOT NULL DEFAULT '',
    "AccountName" VARCHAR(200) NOT NULL DEFAULT '',
    "AccountType" VARCHAR(50) NOT NULL DEFAULT 'Customer',
    "Industry" VARCHAR(100) NOT NULL DEFAULT '',
    "Website" VARCHAR(200) NOT NULL DEFAULT '',
    "Phone" VARCHAR(50) NOT NULL DEFAULT '',
    "Email" VARCHAR(200) NOT NULL DEFAULT '',
    "Address" VARCHAR(500) NOT NULL DEFAULT '',
    "City" VARCHAR(100) NOT NULL DEFAULT '',
    "Country" VARCHAR(100) NOT NULL DEFAULT '',
    "Currency" VARCHAR(10) NOT NULL DEFAULT 'USD',
    "PaymentTerms" VARCHAR(100) NOT NULL DEFAULT '',
    "CreditLimit" VARCHAR(50) NOT NULL DEFAULT '',
    "AssignedTo" VARCHAR(200) NOT NULL DEFAULT '',
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Active',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NULL
);

-- Create crm_sales_pipelines table
CREATE TABLE IF NOT EXISTS yuktira_crm.crm_sales_pipelines (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "PipelineId" VARCHAR(50) NOT NULL DEFAULT '',
    "DealName" VARCHAR(200) NOT NULL DEFAULT '',
    "AccountName" VARCHAR(200) NOT NULL DEFAULT '',
    "ContactPerson" VARCHAR(200) NOT NULL DEFAULT '',
    "Stage" VARCHAR(50) NOT NULL DEFAULT 'Prospecting',
    "DealValue" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "WinningProbability" DECIMAL(5,2) NOT NULL DEFAULT 0,
    "ExpectedCloseDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "ProductLine" VARCHAR(200) NOT NULL DEFAULT '',
    "SalesOrg" VARCHAR(50) NOT NULL DEFAULT '',
    "AssignedTo" VARCHAR(200) NOT NULL DEFAULT '',
    "Notes" TEXT NOT NULL DEFAULT '',
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Open',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NULL
);
