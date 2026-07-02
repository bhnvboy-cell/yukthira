-- ============================================
-- YUKTIRA ERP SUITE - Sample Test Data
-- ============================================

-- Tenants
INSERT INTO yuktira_core.tenants (id, name, code, status) VALUES
    ('00000000-0000-0000-0000-000000000001', 'Demo Corporation', 'DEMO', 'ACTIVE'),
    ('00000000-0000-0000-0000-000000000002', 'Acme Industries', 'ACME', 'ACTIVE'),
    ('00000000-0000-0000-0000-000000000003', 'Globex Manufacturing', 'GLOBEX', 'ACTIVE');

-- Users
INSERT INTO yuktira_core.users (id, username, password_hash, full_name, email, is_super_user, status) VALUES
    ('10000000-0000-0000-0000-000000000001', 'superadmin', '$2a$11$K4YfGqJ1e4YHIp7q3s8x8uR0nX5w3m9v2b1c6d7e8f9g0h1i2j3k4l5m', 'Super Admin', 'superadmin@yuktira.com', TRUE, 'ACTIVE'),
    ('20000000-0000-0000-0000-000000000001', 'admin_demo', '$2a$11$K4YfGqJ1e4YHIp7q3s8x8uR0nX5w3m9v2b1c6d7e8f9g0h1i2j3k4l5m', 'Demo Admin', 'admin@demo.com', FALSE, 'ACTIVE'),
    ('30000000-0000-0000-0000-000000000001', 'jdoe', '$2a$11$K4YfGqJ1e4YHIp7q3s8x8uR0nX5w3m9v2b1c6d7e8f9g0h1i2j3k4l5m', 'John Doe', 'jdoe@demo.com', FALSE, 'ACTIVE'),
    ('40000000-0000-0000-0000-000000000001', 'asmith', '$2a$11$K4YfGqJ1e4YHIp7q3s8x8uR0nX5w3m9v2b1c6d7e8f9g0h1i2j3k4l5m', 'Alice Smith', 'asmith@demo.com', FALSE, 'ACTIVE');

-- Tenant Users
INSERT INTO yuktira_core.tenant_users (tenant_id, user_id, role_id)
SELECT '00000000-0000-0000-0000-000000000001', u.id, r.id
FROM yuktira_core.users u, yuktira_core.roles r
WHERE u.username = 'admin_demo' AND r.code = 'ADMIN';

INSERT INTO yuktira_core.tenant_users (tenant_id, user_id, role_id)
SELECT '00000000-0000-0000-0000-000000000001', u.id, r.id
FROM yuktira_core.users u, yuktira_core.roles r
WHERE u.username = 'jdoe' AND r.code = 'POWER_USER';

INSERT INTO yuktira_core.tenant_users (tenant_id, user_id, role_id)
SELECT '00000000-0000-0000-0000-000000000001', u.id, r.id
FROM yuktira_core.users u, yuktira_core.roles r
WHERE u.username = 'asmith' AND r.code = 'NORMAL_USER';

-- Super User Permissions
INSERT INTO yuktira_core.super_user_permissions (user_id, granted_by)
SELECT id, id FROM yuktira_core.users WHERE username = 'superadmin';

-- Material Masters
INSERT INTO yuktira_mm.material_masters (tenant_id, material_code, material_name, material_type, base_uom, min_stock_qty, reorder_level, reorder_qty, standard_price) VALUES
    ('00000000-0000-0000-0000-000000000001', 'FG-001', 'Finished Product Alpha', 'FINISHED', 'EA', 100, 200, 500, 25.00),
    ('00000000-0000-0000-0000-000000000001', 'FG-002', 'Finished Product Beta', 'FINISHED', 'EA', 50, 100, 300, 45.00),
    ('00000000-0000-0000-0000-000000000001', 'RM-001', 'Raw Material Xylene', 'RAW', 'KG', 500, 1000, 2000, 5.50),
    ('00000000-0000-0000-0000-000000000001', 'RM-002', 'Raw Material Polymer', 'RAW', 'KG', 300, 500, 1000, 8.20),
    ('00000000-0000-0000-0000-000000000001', 'PK-001', 'Standard Packaging Box', 'PACKAGING', 'EA', 1000, 2000, 5000, 0.75),
    ('00000000-0000-0000-0000-000000000001', 'SF-001', 'Semi-Finished Assembly A', 'SEMI_FINISHED', 'EA', 50, 100, 200, 12.00);

-- Vendor Masters
INSERT INTO yuktira_mm.vendor_masters (tenant_id, vendor_code, vendor_name, payment_terms, payment_days, currency, email, phone, status) VALUES
    ('00000000-0000-0000-0000-000000000001', 'VEN-001', 'ABC Supplies Ltd', 'NET30', 30, 'USD', 'info@abcsupplies.com', '+1-555-0101', 'ACTIVE'),
    ('00000000-0000-0000-0000-000000000001', 'VEN-002', 'Global Raw Materials Inc', 'NET45', 45, 'USD', 'sales@globalraw.com', '+1-555-0102', 'ACTIVE'),
    ('00000000-0000-0000-0000-000000000001', 'VEN-003', 'PackPro Solutions', 'NET15', 15, 'USD', 'orders@packpro.com', '+1-555-0103', 'ACTIVE');

-- Customer Masters
INSERT INTO yuktira_sd.customer_masters (tenant_id, customer_code, customer_name, payment_terms, credit_limit, currency, email, status) VALUES
    ('00000000-0000-0000-0000-000000000001', 'CUST-001', 'Acme Corporation', 'NET30', 100000, 'USD', 'ap@acmecorp.com', 'ACTIVE'),
    ('00000000-0000-0000-0000-000000000001', 'CUST-002', 'Globex Industries', 'NET45', 250000, 'USD', 'finance@globex.com', 'ACTIVE'),
    ('00000000-0000-0000-0000-000000000001', 'CUST-003', 'Initech Global', 'NET30', 50000, 'USD', 'billing@initech.com', 'ACTIVE');

-- Purchase Orders
INSERT INTO yuktira_mm.purchase_orders (tenant_id, po_number, po_date, vendor_id, vendor_code, vendor_name, status, total_amount, grand_total, created_by)
SELECT
    '00000000-0000-0000-0000-000000000001',
    yuktira_numberrange.get_next_document_number('00000000-0000-0000-0000-000000000001', 'MM', 'PO'),
    CURRENT_DATE - INTERVAL '5 days',
    id, vendor_code, vendor_name, 'APPROVED', 15000, 16500,
    (SELECT id FROM yuktira_core.users WHERE username = 'jdoe')
FROM yuktira_mm.vendor_masters WHERE vendor_code = 'VEN-001';

INSERT INTO yuktira_mm.purchase_orders (tenant_id, po_number, po_date, vendor_id, vendor_code, vendor_name, status, total_amount, grand_total, created_by)
SELECT
    '00000000-0000-0000-0000-000000000001',
    yuktira_numberrange.get_next_document_number('00000000-0000-0000-0000-000000000001', 'MM', 'PO'),
    CURRENT_DATE - INTERVAL '2 days',
    id, vendor_code, vendor_name, 'SUBMITTED', 25000, 27500,
    (SELECT id FROM yuktira_core.users WHERE username = 'jdoe')
FROM yuktira_mm.vendor_masters WHERE vendor_code = 'VEN-002';

-- Sales Orders
INSERT INTO yuktira_sd.sales_orders (tenant_id, so_number, so_date, customer_id, customer_name, status, total_amount, grand_total, created_by)
SELECT
    '00000000-0000-0000-0000-000000000001',
    yuktira_numberrange.get_next_document_number('00000000-0000-0000-0000-000000000001', 'SD', 'SO'),
    CURRENT_DATE - INTERVAL '3 days',
    id, customer_name, 'CONFIRMED', 35000, 38500,
    (SELECT id FROM yuktira_core.users WHERE username = 'asmith')
FROM yuktira_sd.customer_masters WHERE customer_code = 'CUST-001';

-- GL Accounts
INSERT INTO yuktira_fi.gl_accounts (tenant_id, account_code, account_name, account_type) VALUES
    ('00000000-0000-0000-0000-000000000001', '1000', 'Cash', 'ASSET'),
    ('00000000-0000-0000-0000-000000000001', '1100', 'Accounts Receivable', 'ASSET'),
    ('00000000-0000-0000-0000-000000000001', '1200', 'Inventory', 'ASSET'),
    ('00000000-0000-0000-0000-000000000001', '2000', 'Accounts Payable', 'LIABILITY'),
    ('00000000-0000-0000-0000-000000000001', '3000', 'Equity', 'EQUITY'),
    ('00000000-0000-0000-0000-000000000001', '4000', 'Revenue', 'REVENUE'),
    ('00000000-0000-0000-0000-000000000001', '5000', 'Cost of Goods Sold', 'EXPENSE'),
    ('00000000-0000-0000-0000-000000000001', '6000', 'Operating Expenses', 'EXPENSE');

-- Cost Centers
INSERT INTO yuktira_fi.cost_centers (tenant_id, cost_center_code, cost_center_name, department) VALUES
    ('00000000-0000-0000-0000-000000000001', 'CC-ADMIN', 'Administration', 'Admin'),
    ('00000000-0000-0000-0000-000000000001', 'CC-PROD', 'Production', 'Manufacturing'),
    ('00000000-0000-0000-0000-000000000001', 'CC-SALES', 'Sales & Marketing', 'Sales'),
    ('00000000-0000-0000-0000-000000000001', 'CC-RD', 'R&D', 'Engineering');

-- Work Centers
INSERT INTO yuktira_pp.work_centers (tenant_id, code, name, work_center_type, capacity_per_day) VALUES
    ('00000000-0000-0000-0000-000000000001', 'WC-001', 'Assembly Line 1', 'ASSEMBLY', 16),
    ('00000000-0000-0000-0000-000000000001', 'WC-002', 'CNC Machine', 'MACHINE', 8),
    ('00000000-0000-0000-0000-000000000001', 'WC-003', 'Packing Station', 'LABOR', 8);

-- BOM
INSERT INTO yuktira_pp.bill_of_materials (tenant_id, bom_code, bom_name, finished_material_id, base_qty)
SELECT '00000000-0000-0000-0000-000000000001', 'BOM-FG-001', 'BOM for Finished Product Alpha', id, 1
FROM yuktira_mm.material_masters WHERE material_code = 'FG-001';

INSERT INTO yuktira_pp.bom_items (bom_id, line_no, material_id, material_code, material_name, quantity, uom)
SELECT b.id, 1, m.id, m.material_code, m.material_name, 2, m.base_uom
FROM yuktira_pp.bill_of_materials b, yuktira_mm.material_masters m
WHERE b.bom_code = 'BOM-FG-001' AND m.material_code = 'RM-001';

INSERT INTO yuktira_pp.bom_items (bom_id, line_no, material_id, material_code, material_name, quantity, uom)
SELECT b.id, 2, m.id, m.material_code, m.material_name, 1, m.base_uom
FROM yuktira_pp.bill_of_materials b, yuktira_mm.material_masters m
WHERE b.bom_code = 'BOM-FG-001' AND m.material_code = 'SF-001';

-- Storage Locations
INSERT INTO yuktira_wm.storage_locations (tenant_id, warehouse_code, location_code, location_name, location_type) VALUES
    ('00000000-0000-0000-0000-000000000001', 'WH01', 'WH01-A-01', 'Main Warehouse - Aisle A', 'STORAGE'),
    ('00000000-0000-0000-0000-000000000001', 'WH01', 'WH01-A-02', 'Main Warehouse - Aisle B', 'STORAGE'),
    ('00000000-0000-0000-0000-000000000001', 'WH01', 'WH01-QC-01', 'Quarantine Area', 'QUARANTINE'),
    ('00000000-0000-0000-0000-000000000001', 'WH01', 'WH01-PK-01', 'Picking Area', 'PICKING');

-- Stock
INSERT INTO yuktira_mm.stock (tenant_id, material_id, storage_location_id, stock_qty, uom, unit_price)
SELECT t.id, m.id, sl.id, 500, 'KG', 5.50
FROM yuktira_core.tenants t, yuktira_mm.material_masters m, yuktira_wm.storage_locations sl
WHERE t.code = 'DEMO' AND m.material_code = 'RM-001' AND sl.location_code = 'WH01-A-01';

INSERT INTO yuktira_mm.stock (tenant_id, material_id, storage_location_id, stock_qty, uom, unit_price)
SELECT t.id, m.id, sl.id, 300, 'KG', 8.20
FROM yuktira_core.tenants t, yuktira_mm.material_masters m, yuktira_wm.storage_locations sl
WHERE t.code = 'DEMO' AND m.material_code = 'RM-002' AND sl.location_code = 'WH01-A-01';

INSERT INTO yuktira_mm.stock (tenant_id, material_id, storage_location_id, stock_qty, uom, unit_price)
SELECT t.id, m.id, sl.id, 200, 'EA', 25.00
FROM yuktira_core.tenants t, yuktira_mm.material_masters m, yuktira_wm.storage_locations sl
WHERE t.code = 'DEMO' AND m.material_code = 'FG-001' AND sl.location_code = 'WH01-A-02';

-- Employees
INSERT INTO yuktira_hr.employee_masters (tenant_id, employee_code, employee_name, department, designation, email, status)
SELECT t.id, 'EMP-001', 'John Doe', 'Production', 'Production Manager', 'jdoe@demo.com', 'ACTIVE'
FROM yuktira_core.tenants t WHERE t.code = 'DEMO';

INSERT INTO yuktira_hr.employee_masters (tenant_id, employee_code, employee_name, department, designation, email, status)
SELECT t.id, 'EMP-002', 'Alice Smith', 'Sales', 'Sales Executive', 'asmith@demo.com', 'ACTIVE'
FROM yuktira_core.tenants t WHERE t.code = 'DEMO';

-- CRM Leads
INSERT INTO yuktira_crm.crm_leads (tenant_id, lead_number, company_name, contact_name, email, phone, source, status, expected_revenue)
SELECT t.id, 'LEAD-001', 'TechStart Inc', 'Bob Johnson', 'bob@techstart.com', '+1-555-0201', 'WEBSITE', 'NEW', 50000
FROM yuktira_core.tenants t WHERE t.code = 'DEMO';

INSERT INTO yuktira_crm.crm_leads (tenant_id, lead_number, company_name, contact_name, email, phone, source, status, expected_revenue)
SELECT t.id, 'LEAD-002', 'MegaCorp Ltd', 'Carol Williams', 'carol@megacorp.com', '+1-555-0202', 'REFERRAL', 'QUALIFIED', 150000
FROM yuktira_core.tenants t WHERE t.code = 'DEMO';

-- Notification Templates
INSERT INTO yuktira_notification.notification_templates (code, name, subject_template, body_template, channel_default)
VALUES ('SYSTEM_ALERT', 'System Alert', 'Yuktira System Alert', 'System notification: {{message}}', 'INAPP')
ON CONFLICT (code) DO NOTHING;

-- Dashboard Widgets for Demo User
INSERT INTO yuktira_dashboard.dashboard_user_widgets (user_id, widget_id, position_x, position_y, width, height)
SELECT u.id, w.id, 0, 0, 2, 1
FROM yuktira_core.users u, yuktira_dashboard.dashboard_widgets w
WHERE u.username = 'jdoe' AND w.code = 'OPEN_PO';

INSERT INTO yuktira_dashboard.dashboard_user_widgets (user_id, widget_id, position_x, position_y, width, height)
SELECT u.id, w.id, 2, 0, 2, 1
FROM yuktira_core.users u, yuktira_dashboard.dashboard_widgets w
WHERE u.username = 'jdoe' AND w.code = 'STOCK_OVERVIEW';

INSERT INTO yuktira_dashboard.dashboard_user_widgets (user_id, widget_id, position_x, position_y, width, height)
SELECT u.id, w.id, 4, 0, 2, 1
FROM yuktira_core.users u, yuktira_dashboard.dashboard_widgets w
WHERE u.username = 'jdoe' AND w.code = 'QUALITY_ALERTS';

INSERT INTO yuktira_dashboard.dashboard_user_widgets (user_id, widget_id, position_x, position_y, width, height)
SELECT u.id, w.id, 0, 1, 6, 3
FROM yuktira_core.users u, yuktira_dashboard.dashboard_widgets w
WHERE u.username = 'jdoe' AND w.code = 'PENDING_APPROVALS';

-- Workflow Definition
INSERT INTO yuktira_workflow.workflow_definitions (tenant_id, name, code, module, is_active, version, created_by)
SELECT t.id, 'PO Approval Workflow', 'PO_APPROVAL', 'MM', TRUE, 1, u.id
FROM yuktira_core.tenants t, yuktira_core.users u
WHERE t.code = 'DEMO' AND u.username = 'admin_demo';

INSERT INTO yuktira_workflow.workflow_definitions (tenant_id, name, code, module, is_active, version, created_by)
SELECT t.id, 'SO Fulfillment Workflow', 'SO_FULFILLMENT', 'SD', TRUE, 1, u.id
FROM yuktira_core.tenants t, yuktira_core.users u
WHERE t.code = 'DEMO' AND u.username = 'admin_demo';

SELECT 'Sample data loaded successfully!' AS message;
