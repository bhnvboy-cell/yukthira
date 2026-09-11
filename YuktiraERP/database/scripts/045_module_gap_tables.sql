-- ══════════════════════════════════════════════════════
-- MODULE GAP TABLES: SD, PP, WM, FI, CO, HR
-- YuktiraERP v2.0.0 — Functional Coverage Closure
-- ══════════════════════════════════════════════════════

-- SD: Scheduling Agreements
CREATE TABLE IF NOT EXISTS yuktira_sd.scheduling_agreements (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "AgreementNumber" VARCHAR(50) NOT NULL,
    "CustomerCode" VARCHAR(50) NOT NULL,
    "MaterialCode" VARCHAR(50) NOT NULL,
    "TotalQuantity" DECIMAL(18,3) NOT NULL DEFAULT 0,
    "ValidFrom" TIMESTAMP NOT NULL,
    "ValidTo" TIMESTAMP NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Active',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_sched_agreements_tenant ON yuktira_sd.scheduling_agreements("TenantId");

-- SD: Schedule Lines
CREATE TABLE IF NOT EXISTS yuktira_sd.schedule_lines (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "SchedulingAgreementId" UUID NOT NULL,
    "DeliveryDate" TIMESTAMP NOT NULL,
    "Quantity" DECIMAL(18,3) NOT NULL DEFAULT 0,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Open',
    "DeliveredQuantity" DECIMAL(18,3) NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- SD: Revenue Recognition
CREATE TABLE IF NOT EXISTS yuktira_sd.revenue_recognition_schedules (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "BillingDocumentNumber" VARCHAR(50) NOT NULL,
    "CustomerCode" VARCHAR(50) NOT NULL,
    "TotalAmount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "RecognizedAmount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "DeferredAmount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "ServiceStartDate" TIMESTAMP NOT NULL,
    "ServiceEndDate" TIMESTAMP NOT NULL,
    "RecognitionMethod" VARCHAR(50) NOT NULL DEFAULT 'StraightLine',
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- PP: Capacity Load
CREATE TABLE IF NOT EXISTS yuktira_pp.capacity_loads (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "WorkCenterCode" VARCHAR(50) NOT NULL,
    "LoadDate" TIMESTAMP NOT NULL,
    "AvailableHours" DECIMAL(8,2) NOT NULL DEFAULT 0,
    "AssignedHours" DECIMAL(8,2) NOT NULL DEFAULT 0,
    "UtilizationPercent" DECIMAL(6,2) NOT NULL DEFAULT 0,
    "OverloadedOrderNumber" VARCHAR(50),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- PP: Kanban Board
CREATE TABLE IF NOT EXISTS yuktira_pp.kanban_boards (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "Plant" VARCHAR(10) NOT NULL,
    "MaterialCode" VARCHAR(50) NOT NULL,
    "BinCode" VARCHAR(50) NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'InProcess',
    "CurrentQuantity" DECIMAL(18,3) NOT NULL DEFAULT 0,
    "TargetQuantity" DECIMAL(18,3) NOT NULL DEFAULT 0,
    "AssignedProductionOrder" VARCHAR(50),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- FI: Intercompany Transactions
CREATE TABLE IF NOT EXISTS yuktira_fi.intercompany_transactions (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "TransactionType" VARCHAR(50) NOT NULL,
    "SendingCompanyCode" VARCHAR(10) NOT NULL,
    "ReceivingCompanyCode" VARCHAR(10) NOT NULL,
    "Amount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "Currency" VARCHAR(5) NOT NULL DEFAULT 'INR',
    "ExchangeRate" DECIMAL(12,6) NOT NULL DEFAULT 1.0,
    "SendingDocumentNumber" VARCHAR(50),
    "ReceivingDocumentNumber" VARCHAR(50),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Posted',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- FI: Withholding Tax
CREATE TABLE IF NOT EXISTS yuktira_fi.withholding_tax_entries (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "VendorCode" VARCHAR(50) NOT NULL,
    "TaxCode" VARCHAR(20) NOT NULL,
    "TaxType" VARCHAR(30) NOT NULL,
    "GrossAmount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "TaxRate" DECIMAL(6,2) NOT NULL DEFAULT 0,
    "TaxAmount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "NetAmount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "TaxCertificateNumber" VARCHAR(50),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- FI: Period Close
CREATE TABLE IF NOT EXISTS yuktira_fi.period_closes (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "CompanyCode" VARCHAR(10) NOT NULL,
    "FiscalYear" INTEGER NOT NULL,
    "Period" INTEGER NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Open',
    "OpenItemsCount" INTEGER NOT NULL DEFAULT 0,
    "UnpostedAmount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "ClosedAt" TIMESTAMP,
    "ClosedBy" VARCHAR(100),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- CO: Product Costing
CREATE TABLE IF NOT EXISTS yuktira_co.product_costs (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "MaterialCode" VARCHAR(50) NOT NULL,
    "Plant" VARCHAR(10) NOT NULL,
    "CostingType" VARCHAR(20) NOT NULL DEFAULT 'Standard',
    "MaterialCost" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "LaborCost" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "OverheadCost" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "TotalCost" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "CostPerUnit" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "CostingDate" TIMESTAMP NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Active',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- CO: CO-PA Profitability Segments
CREATE TABLE IF NOT EXISTS yuktira_co.profitability_segments (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "OperatingConcern" VARCHAR(50) NOT NULL,
    "SegmentName" VARCHAR(100) NOT NULL,
    "SegmentValue" VARCHAR(100) NOT NULL,
    "Revenue" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "Costs" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "Margin" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "MarginPercent" DECIMAL(6,2) NOT NULL DEFAULT 0,
    "AnalysisDate" TIMESTAMP NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- CO: Transfer Pricing
CREATE TABLE IF NOT EXISTS yuktira_co.transfer_pricing_entries (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "MaterialCode" VARCHAR(50) NOT NULL,
    "SendingCompanyCode" VARCHAR(10) NOT NULL,
    "ReceivingCompanyCode" VARCHAR(10) NOT NULL,
    "Quantity" DECIMAL(18,3) NOT NULL DEFAULT 0,
    "CostBase" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "MarkupPercent" DECIMAL(6,2) NOT NULL DEFAULT 0,
    "MarkupAmount" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "TransferPrice" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "PricingMethod" VARCHAR(30) NOT NULL DEFAULT 'CostPlus',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- HR: Benefits Enrollment
CREATE TABLE IF NOT EXISTS yuktira_hr.benefits_enrollments (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "EmployeeCode" VARCHAR(50) NOT NULL,
    "BenefitType" VARCHAR(50) NOT NULL,
    "PlanCode" VARCHAR(50) NOT NULL,
    "EmployeeContribution" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "EmployerContribution" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "TotalPremium" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "EffectiveDate" TIMESTAMP NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Active',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- HR: Succession Plans
CREATE TABLE IF NOT EXISTS yuktira_hr.succession_plans (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" UUID NOT NULL,
    "PositionCode" VARCHAR(50) NOT NULL,
    "CurrentIncumbentCode" VARCHAR(50) NOT NULL,
    "CandidateEmployeeCode" VARCHAR(50) NOT NULL,
    "CandidateName" VARCHAR(200) NOT NULL,
    "Readiness" VARCHAR(30) NOT NULL DEFAULT 'NotReady',
    "DevelopmentPlan" TEXT,
    "PerformanceRating" INTEGER NOT NULL DEFAULT 0,
    "RiskOfLoss" VARCHAR(20),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Active',
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);
