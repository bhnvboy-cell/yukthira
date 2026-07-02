# YUKTIRA ERP SUITE — User Guide

## Overview

Yuktira ERP is a comprehensive enterprise platform built on ASP.NET Core 10. It features multi-tenant architecture, 60+ SAP-style transaction codes, 13 ERP modules, JWT authentication, BPMN workflow engine, AI forecasting, MRP, and plugin extensibility.

---

## 1. Getting Started

### Quick Start (No Database Required)

```batch
start.bat
```

This builds and starts:
- **Web UI**: http://localhost:5001
- **API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger

### Default Login Credentials

| User ID | Role | Password |
|---------|------|----------|
| jdoe | Power User | any password |
| superadmin | Super User | any password |
| admin_demo | Admin | any password |
| asmith | Normal User | any password |

Client Number: `1000` (maps to DEMO tenant)

---

## 2. Client Access

Clients access the system through a web browser:

**Development**: `http://<server-ip>:5001`
**Production**: `https://erp.yourcompany.com`

### Multi-Tenant Access

Each tenant gets a subdomain (subdomain mode):
- `tenant1.yourcompany.com`
- `tenant2.yourcompany.com`

Configured via Apache virtual hosting (see `apache-config/yuktira-erp.conf`).

### Client Requirements
- Modern web browser (Chrome 120+, Edge 120+, Firefox 120+, Safari 17+)
- Internet / VPN connection to reach server
- No additional software required

---

## 3. Login

The login page requires:
- **Client Number** — tenant code (e.g., `1000`)
- **User ID** — e.g., `jdoe`
- **Password**
- **Language** — English, Arabic, French, Spanish, German
- **System** — Development, QA, Production

On successful login, the user is redirected to their Dashboard with role-based widgets.

---

## 4. Transaction Codes

Yuktira uses SAP-style transaction codes for quick navigation. Type a code in the sidebar search bar or use the Transaction Launcher.

### Materials Management (MM)

| Code | Description |
|------|-------------|
| MM01 | Create Material |
| MM02 | Change Material |
| MM03 | Display Material |
| MMBE | Stock Overview |
| ME11 | Create Vendor |
| ME12 | Change Vendor |
| ME13 | Display Vendor |
| ME21N | Create Purchase Order |
| ME22N | Change Purchase Order |
| ME23N | Display Purchase Order |
| MIGO | Goods Receipt |
| MIRO | Invoice Verification |
| MB52 | Stock List |
| MB1A | Goods Issue |
| MB1C | Goods Receipt (Other) |

### Sales & Distribution (SD)

| Code | Description |
|------|-------------|
| VA01 | Create Sales Order |
| VA02 | Change Sales Order |
| VA03 | Display Sales Order |
| VA05 | Sales Order List |
| VLO1N | Create Delivery |
| VF01 | Create Billing Document |
| VD01 | Create Customer |
| VD02 | Change Customer |
| VD03 | Display Customer |
| VKD1 | Customer List |

### Production Planning (PP)

| Code | Description |
|------|-------------|
| CS01 | Create BOM |
| CS02 | Change BOM |
| CS03 | Display BOM |
| CO01 | Create Production Order |
| CO02 | Change Production Order |
| CO03 | Display Production Order |
| MD01 | MRP Run |
| MD04 | Stock Requirements List |
| CR01 | Create Work Center |

### Quality Management (QM)

| Code | Description |
|------|-------------|
| QE01 | Create Inspection Lot |
| QE02 | Change Inspection Lot |
| QE03 | Display Inspection Lot |
| QS01 | Create Inspection Plan |
| QS02 | Change Inspection Plan |
| QA01 | Record Inspection Result |
| QUD | Usage Decision |

### Warehouse (WM)

| Code | Description |
|------|-------------|
| LT01 | Create Transfer |
| LT02 | Change Transfer |
| LT03 | Display Transfer |
| LS01 | Create Storage Location |
| LS02 | Change Storage Location |

### Finance (FI)

| Code | Description |
|------|-------------|
| FB50 | Journal Entry |
| FB60 | AP Invoice |
| FB70 | AR Invoice |
| FBL1N | Vendor Line Items |
| FBL5N | Customer Line Items |
| FS10N | G/L Account Balance |
| FAGLL03 | G/L Line Items |
| F.01 | Balance Sheet |
| F.02 | Profit & Loss |
| F-03 | AP Payment |
| F-28 | AR Payment |

### Controlling (CO)

| Code | Description |
|------|-------------|
| KA01 | Create Cost Center |
| KA02 | Change Cost Center |
| KA03 | Display Cost Center |
| KOB1 | Cost Center Report |

### Human Resources (HR)

| Code | Description |
|------|-------------|
| PA30 | Maintain Employee |
| PA20 | Display Employee |
| PA40 | Employee List |
| PT60 | Attendance Report |
| PR01 | Run Payroll |
| PR05 | Payroll History |

### CRM

| Code | Description |
|------|-------------|
| CRM01 | Create Lead |
| CRM02 | Edit Lead |
| CRM03 | Display Lead |
| CRM04 | Create Opportunity |
| CRM05 | Edit Opportunity |
| CRM06 | Pipeline Report |

### LIMS (Lab)

| Code | Description |
|------|-------------|
| LM01 | Create Sample |
| LM02 | Edit Sample |
| LM03 | Display Sample |
| LM04 | Record Test Result |
| LM05 | Maintain Instrument |

### BI / Analytics

| Code | Description |
|------|-------------|
| BI01 | Create Report |
| BI02 | Run Report |
| BI03 | Display Dashboard |
| BI04 | Monitor KPI |

### System / Administration

| Code | Description |
|------|-------------|
| SU01 | User Management |
| SU02 | Tenant Management |
| SU03 | System Config |
| AL01 | Audit Log |
| AL02 | Suspicious Activity |
| WF01 | Workflow Designer |
| WF02 | Workflow Instances |
| AP01 | Pending Approvals |
| AP02 | Approval History |
| NO01 | Notifications |
| TC01 | Transaction Launcher |
| TC02 | Transaction Management |
| PL01 | Plugin Management |

---

## 5. Modules

| Module | Description |
|--------|-------------|
| MM | Materials Management — inventory, purchasing, vendors, GRN, stock valuation |
| SD | Sales & Distribution — orders, customers, billing, delivery |
| PP | Production Planning — BOMs, work centers, MRP, routing, capacity |
| QM | Quality Management — inspections, plans, lots, SPC |
| WM | Warehouse Management — transfers, storage locations, bin-level tracking |
| FI | Finance — ledger, AP/AR, P&L, balance sheet, fixed assets |
| CO | Controlling — cost centers, profit centers, internal orders |
| HR | Human Resources — employees, payroll (PF/ESI/PT/TDS), leave, attendance |
| CRM | Customer Relationship Management — leads, opportunities, campaigns |
| LIMS | Lab Information Management — samples, tests, instruments |
| BI | Business Intelligence — reports, dashboards, KPI formula engine |
| PM | Plant Maintenance — equipment, maintenance orders, schedules |
| PS | Project Systems — projects, tasks, timesheets |

---

## 6. Users & Roles

| Role | Code | Access |
|------|------|--------|
| Super User | `SUPER_USER` | Global admin — impersonate, unlock, manage tenants, override approvals |
| Admin | `ADMIN` | Tenant admin — user management, config, dashboard customization |
| Power User | `POWER_USER` | Operational with configuration rights |
| Normal User | `NORMAL_USER` | Standard transaction execution, document creation |
| Read-Only | `READ_ONLY` | View dashboards, run reports, no mutations |

### Password Policy
- Minimum length: configurable (default 8)
- Max failed attempts: configurable (default 5) before account lockout
- MFA support: TOTP-ready

---

## 7. Dashboard

The dashboard provides role-based multi-widget KPI display:
- Open POs, pending approvals, monthly revenue
- Stock overview, quality alerts, production status
- System widgets pre-seeded (OPEN_PO, PENDING_APPROVALS, MONTHLY_REVENUE, STOCK_OVERVIEW, QUALITY_ALERTS, PRODUCTION_STATUS)
- Layout editor with widget configuration

---

## 8. Workflow Designer

BPMN-style node editor with Start → Approval → Task → Decision → Email → End pipeline.

**Node types**: START, TASK, APPROVAL, DECISION, TIMER, API_CALL, EMAIL, SMS, CONDITION, END

Features:
- Conditional edges with expression evaluation
- DB-backed persistence via `yuktira_workflow` schema
- Simulation mode
- Full execution history

---

## 9. API Access

Base URL: `http://localhost:5000/api`

Authentication: Bearer JWT token (obtained via `POST /api/auth/login`)

Full reference: `docs/api-reference.md`

---

## 10. Plugins

Extend system functionality with plugins (e.g., DairyExtension, AdvancedQC, ExtraReports).

- **Manage**: Plugin Management (PL01)
- **SDK**: `src/YuktiraERP.PluginSdk`
- **Example plugins**: `src/plugins/`
- **Development guide**: `docs/plugin-development.md`

---

## 11. Deployment Architecture

```
                    Client Browser
                         |
                    Apache (port 443)
                    /     |       \
                   /      |        \
              Web UI   API Node 1   API Node 2...
             (:5001)    (:5000)      (:5000)
                  |        |        /
                  |        |       /
              PostgreSQL  Redis (sessions/cache)
```

---

## 12. Troubleshooting

### Build fails with "Copying file from..."
```batch
rmdir /s src\YuktiraERP.Api\bin
rmdir /s src\YuktiraERP.Api\obj
dotnet build
```

### Port already in use
Change ports in `src/YuktiraERP.Api/Program.cs` (line 66) or `src/YuktiraERP.Web/Program.cs` (line 30).

### Database connection refused
Ensure PostgreSQL is running and update connection string in `appsettings.json`.

### Login fails
- Use `jdoe` / any password with client number `1000`
- Account locked? Admin can unlock via `POST /api/security/unlock-user/{userId}`
- Check JWT secret is configured (min 32 chars)
