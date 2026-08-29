-- ============================================================
-- Migration 027: Seed missing TCode Engine transaction codes
-- ============================================================
-- These TCodes are registered in TCodeLayoutRegistry but were missing
-- from the transaction_codes table, causing 404 on API calls.

INSERT INTO yuktira_transaction.transaction_codes ("Id", "Code", "Name", "Description", "Module", "GroupName", "Route", "Icon", "SortOrder", "Status", "IsSystem", "RequiredRole", "Params", "CreatedAt") VALUES
(gen_random_uuid(), 'CO11N', 'Production Order Confirmation', 'Production Order Confirmation (CO11N)', 'PP', 'Transactions', '/Transactions/Engine/CO11N', 'bi-check-circle', 50, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'ME51N', 'Create Purchase Requisition', 'Create Purchase Requisition (ME51N)', 'MM', 'Transactions', '/Transactions/Engine/ME51N', 'bi-cart-plus', 50, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'ME28', 'PO Release / Approval', 'PO Release / Approval (ME28)', 'MM', 'Transactions', '/Transactions/Engine/ME28', 'bi-check2-square', 50, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'MD61', 'Planned Independent Requirements', 'Planned Independent Requirements (MD61)', 'PP', 'Transactions', '/Transactions/Engine/MD61', 'bi-calendar-date', 50, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'F-53', 'Vendor Outgoing Payment', 'Vendor Outgoing Payment (F-53)', 'FI', 'Transactions', '/Transactions/Engine/F-53', 'bi-cash-stack', 50, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'IL01', 'Create Functional Location', 'Create Functional Location (IL01)', 'PM', 'Transactions', '/Transactions/Engine/IL01', 'bi-geo-alt', 50, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'CRRETURN', 'Customer Complaint & Return', 'Customer Complaint & Return Order (CRRETURN)', 'SD', 'Transactions', '/Transactions/Engine/CRRETURN', 'bi-arrow-return-left', 50, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'CRINSPECT', 'Quality Inspection - Return Analysis', 'Quality Inspection - Return Analysis (CRINSPECT)', 'QM', 'Transactions', '/Transactions/Engine/CRINSPECT', 'bi-search', 51, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'CRUDPOST', 'Post Usage Decision - Return', 'Post Usage Decision - Return (CRUDPOST)', 'QM', 'Transactions', '/Transactions/Engine/CRUDPOST', 'bi-check2-circle', 52, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'CRCREDIT', 'Customer Credit Memo - Return', 'Customer Credit Memo - Return (CRCREDIT)', 'SD', 'Transactions', '/Transactions/Engine/CRCREDIT', 'bi-credit-card', 53, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'CRSUPPLY', 'Supplier Complaint & Claim', 'Supplier Complaint & Claim (CRSUPPLY)', 'QM', 'Transactions', '/Transactions/Engine/CRSUPPLY', 'bi-exclamation-triangle', 54, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'CRSRET', 'Supplier Return Delivery', 'Supplier Return Delivery (CRSRET)', 'MM', 'Transactions', '/Transactions/Engine/CRSRET', 'bi-arrow-return-left', 55, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'CRDEBIT', 'Supplier Debit Memo', 'Supplier Debit Memo (CRDEBIT)', 'FI', 'Transactions', '/Transactions/Engine/CRDEBIT', 'bi-receipt', 56, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'RFSCAN', 'RF Scanner Menu', 'RF Scanner Menu (RFSCAN)', 'WM', 'Transactions', '/Transactions/Engine/RFSCAN', 'bi-upc-scan', 50, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'RFPICK', 'RF Pick Task', 'RF Pick Task (RFPICK)', 'WM', 'Transactions', '/Transactions/Engine/RFPICK', 'bi-box-arrow-in-right', 51, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'WAVEPK', 'Wave Pick Management', 'Wave Pick Management (WAVEPK)', 'WM', 'Transactions', '/Transactions/Engine/WAVEPK', 'bi-water', 52, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'VSLOTT', 'Velocity Slotting', 'Velocity Slotting (VSLOTT)', 'WM', 'Transactions', '/Transactions/Engine/VSLOTT', 'bi-speedometer2', 53, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'PPDS', 'PP/DS Finite Scheduling', 'PP/DS Finite Scheduling (PPDS)', 'PP', 'Transactions', '/Transactions/Engine/PPDS', 'bi-clock-history', 54, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'MRPEVT', 'MRP Event Monitor', 'MRP Event Monitor (MRPEVT)', 'MM', 'Transactions', '/Transactions/Engine/MRPEVT', 'bi-activity', 55, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'CONSOL', 'Consolidation Workbench', 'Consolidation Workbench (CONSOL)', 'FI', 'Transactions', '/Transactions/Engine/CONSOL', 'bi-layers', 56, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'TAXRET', 'Tax Return Filing', 'Tax Return Filing (TAXRET)', 'FI', 'Transactions', '/Transactions/Engine/TAXRET', 'bi-file-earmark-text', 57, 'Active', true, 'NORMAL_USER', '{}', NOW()),
(gen_random_uuid(), 'AIOCR', 'Document OCR Processing', 'Document OCR Processing (AIOCR)', 'AI', 'Transactions', '/Transactions/Engine/AIOCR', 'bi-eye', 58, 'Active', true, 'NORMAL_USER', '{}', NOW())
ON CONFLICT DO NOTHING;

-- Fix VL01N typo
UPDATE yuktira_transaction.transaction_codes SET "Code" = 'VL01N' WHERE "Code" = 'VLO1N';
