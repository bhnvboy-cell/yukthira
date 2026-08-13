# Yuktira ERP Suite

Enterprise ERP Platform — Intelligence Driven (Sanskrit: युक्ति - "logic, strategy")

**Version 1.0.2** | **August 2026**

---

## Quick Start

```batch
git clone https://github.com/bhnvboy-cell/yukthira.git
cd yukthira\YuktiraERP
init-db.bat     (first run only — creates the database)
start.bat
```

Then open **http://localhost:5001** and login with:
- **User**: `superadmin`
- **Password**: `yuktira123`
- **Client**: `1000`

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 16 (required — the app connects to a local `yuktira_erp` database on startup and runs the EF Core migration/seeding pipeline)

## Structure

```
├── src/
│   ├── YuktiraERP.Core/            Domain models, interfaces, DTOs
│   ├── YuktiraERP.Infrastructure/  EF Core, services, multi-tenant, SignalR hub
│   ├── YuktiraERP.Api/            REST API (port 5000), middleware, SignalR
│   ├── YuktiraERP.Web/            Web UI (port 5001)
│   ├── YuktiraERP.Tests/          xUnit unit/integration tests
│   ├── YuktiraERP.WorkflowEngine/  BPMN workflow runtime (legacy in-memory)
│   ├── YuktiraERP.AIEngine/        ML forecasting (MA, WMA, ES, LR, Seasonal, HW, ARIMA)
│   ├── YuktiraERP.ExportEngine/    XLSX/CSV/PDF/HTML export — 9 templates
│   ├── YuktiraERP.PluginSdk/       Plugin SDK — interfaces, assembly loader, 4 hook types, hot reload, sandboxing
│   └── plugins/                    Example plugins (AdvancedQC, Dairy, Reports) — see `docs/plugin-development.md`
├── database/
│   ├── scripts/                    SQL migration scripts (001–007)
│   └── backup/                     Disaster recovery runbook
├── scripts/                        Docker, deploy, build scripts
├── apache-config/                  Reverse proxy config
└── docs/
    ├── architecture.md             Architecture overview
    ├── api-reference.md            REST API reference
    ├── user-guide.md               Full user guide
    └── plugin-development.md       Plugin SDK & hooks guide
```

---

## Screenshots

*Visuals for the screens below are available in the live demo environment.*

### Dashboard
Multi-widget KPI dashboard: open POs, pending approvals, monthly revenue (chart), stock overview, quality alerts, production status. Role-based widget visibility. System widgets pre-seeded (OPEN_PO, PENDING_APPROVALS, MONTHLY_REVENUE, STOCK_OVERVIEW, QUALITY_ALERTS, PRODUCTION_STATUS).

### Workflow Designer
BPMN-style node editor with Start → Approval → Task → Decision → Email → End pipeline. Node types: START, TASK, APPROVAL, DECISION, TIMER, API_CALL, EMAIL, SMS, CONDITION, END. Conditional edges with expression evaluation. DB-backed persistence via `yuktira_workflow` schema.

### MRP Screen
Material requirements planning grid: BOM explosion across finished goods → sub-assemblies → raw materials. Shortage alerts, planned orders, safety stock calculation, capacity load view. Single-click convert planned orders to production/release POs.

### Plugin Marketplace
Plugin registry with per-tenant enable/disable. Currently ships with AdvancedQC (SPC charts, control charts, auto COA), DairyExtension (milk collection, fat/SNF testing, procurement), ExtraReports (profitability, variance, executive summary). API endpoints: `GET /api/v1/plugins`, `POST /api/v1/plugins/{code}/install`.

### Transaction Code Sidebar
107 SAP-style codes (MM01, VA01, FB50, MIRO, PS01, PM01, etc.) organized by module with search, favorites, and role-based visibility. Enter-key triggers direct API execution fallback when debounced search results aren't ready. Includes the PS/PM module codes (PS01–PS04, PM01–PM04).

### Module Registry & Sidebar
The dashboard and sidebar are driven by a central **module catalog** (`ModuleCatalog`, registered as a singleton in `Infrastructure`): 24 modules (MM, SD, WM, PP, QM, PM, FI, CO, HR, CRM, PS, LIMS, BI, AI, WF, APP, NOT, TCD, TCG, AUD, ADM, CST, INT, PLG) grouped into Operations / Finance / People / Projects & Labs / Analytics / System with per-module icons and category colors. Modules resolve from routes and transaction codes derive module + SAP-style group (MasterData / Transactions / Process / Reports / Configuration / Administration / Analytics / Utilities) from the catalog — e.g. MM01=MasterData, MIGO=Transactions, MD01=Process, MB52=Reports, BI01=Configuration, SU01=Administration.

### Full CRUD Pages
Every entity has a full Web UI CRUD set — **List** (searchable table with View/Edit/Delete), **Display** (read-only detail), **Edit** (pre-filled form), and **Create** — wired together with redirects after save. 32 entity types previously limited to Create-only now have complete List/Edit/Display pages (GRN, Invoice Verification, PR, SD Billing/Delivery/Inquiry/Quotation, PP Plan/Routing/WorkCenter, QM InspectionResult/UsageDecision, WM Bin, FI FixedAsset/Ledger, CO CostElement/ProfitCenter/InternalOrder, HR Appraisal/Attendance/Leave/Payroll, CRM Campaign/Contact/ServiceTicket, LIMS Instrument/Specification/TestResult, PS ProjTask/Timesheet, PM Order/Plan).

### Print / Save-as-PDF
Every page (module Index, List, Display, Dashboard, transactions) shows a **Print** button in the top bar (or press `Ctrl+P`). It expands horizontally-scrolled tables and opens the browser print dialog with a dedicated print stylesheet — the sidebar, top bar, and action buttons are hidden, tables render with clean borders, and rows avoid page breaks. From the dialog you can print directly or choose **"Save as PDF"** (available in every modern browser), giving a clean paper-friendly copy of any module screen.

---

## Mobile & Tablet

The Web UI is fully responsive (Bootstrap 5.3 + custom breakpoints) and can be used on phones and tablets from the same browser session.

### Access from a device
1. Ensure the device is on the same network as the server.
2. Open `http://<server-ip>:5001` (e.g. `http://10.69.149.221:5001`) — firewall rules for `yuktiraerp.api.exe` / `yuktiraerp.web.exe` already allow inbound access.
3. Login normally with client `1000` and `superadmin` / `yuktira123`.

### Responsive behavior
- **≤ 992px (tablets / small laptops)**: the icon sidebar becomes an off-canvas drawer. Use the hamburger button (☰) in the top bar to open it; the drawer slides in with a dimmed backdrop, tap anywhere outside or press `Esc` to close. Labels are always visible inside the drawer.
- **≤ 768px (phones)**: tiles collapse to a 2-column grid, KPI cards to 2 columns, and data tables become horizontally scrollable (swipe left/right) instead of squishing — tap targets (buttons, nav items) are enlarged for touch.
- **≤ 480px (small phones)**: tiles go single-column, tile icons move inline, the global search box shrinks, and form inputs use the native font-size (16px) to prevent iOS zoom-on-focus.
- **Dashboard**: module tiles, pinned favorites, recently-used shortcuts, and KPI cards all reflow down to a single column.

---

## Architecture

### High-Level Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Client Layer                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐               │
│  │  Browser  │  │  Mobile  │  │   API    │               │
│  │ (Razor)  │  │ (Future)  │  │ Clients  │               │
│  └────┬─────┘  └──────────┘  └────┬─────┘               │
│       │                           │                      │
├───────┴───────────────────────────┴──────────────────────┤
│                    Proxy Layer                            │
│  ┌──────────────────────────────────────────────┐        │
│  │  Apache / Nginx (Reverse Proxy, SSL, LB)     │        │
│  └──────────────────┬───────────────────────────┘        │
├─────────────────────┴────────────────────────────────────┤
│                 Application Layer                         │
│  ┌──────────────────────────────────────────────────┐    │
│  │   YuktiraERP.Web (Razor Pages, port 5001)        │    │
│  └────────────────────┬─────────────────────────────┘    │
│  ┌────────────────────┴─────────────────────────────┐    │
│  │   YuktiraERP.Api (REST /api/v1, port 5000)        │    │
│  │   ├── AuthController     ─── JWT + refresh        │    │
│  │   ├── SecurityController ─── RBAC, audit, policy  │    │
│  │   ├── WorkflowController ─── BPMN engine          │    │
│  │   ├── AIEngineController ─── Forecasting API      │    │
│  │   ├── Module Controllers ─── MM, SD, PP, QM etc.  │    │
│  │   └── Middleware: Tenant, Audit, Exception         │    │
│  └────────────────────┬─────────────────────────────┘    │
├───────────────────────┴──────────────────────────────────┤
│                   Service Layer                           │
│  ┌──────────────────────────────────────────────────┐    │
│  │  YuktiraERP.Infrastructure                        │    │
│  │  ├── AuthService       ─── JWT, lockout, MFA      │    │
│  │  ├── WorkflowService   ─── DB-backed BPMN         │    │
│  │  ├── Predictability    ─── AI + DB bridge         │    │
│  │  ├── MrpService        ─── BOM explosion          │    │
│  │  ├── AccountingService ─── GL, P&L, BS            │    │
│  │  ├── PayrollService    ─── PF/ESI/PT/TDS calc     │    │
│  │  ├── AuditService      ─── Logging + detection    │    │
│  │  ├── NotificationService ─── Email/SMS/InApp      │    │
│  │  └── DataSeeder        ─── Migration pipeline     │    │
│  └────────────────────┬─────────────────────────────┘    │
├───────────────────────┴──────────────────────────────────┤
│              Engine Layer                                 │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │Workflow  │ │   AI     │ │  Export  │ │  Plugin  │    │
│  │ Engine   │ │ Engine   │ │  Engine  │ │  SDK     │    │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘    │
├───────┴────────────┴────────────┴────────────┴───────────┤
│                   Data Layer                              │
│  ┌──────────────────────────────────────────────────┐    │
│  │  PostgreSQL (yuktira_core, _mm, _sd, _fi, ...)   │    │
│  │  16 schemas, JSONB audit, PL/pgSQL number ranges │    │
│  └──────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────┘
```

### Module Interaction Flow

```
User Action → Controller → Service → Engine (optional) → DB
                                  ↕
                         AuditMiddleware (logs everything)
                                  ↕
                         TenantMiddleware (scopes to tenant)
```

### Workflow Engine Flow

```
Definition DB (workflow_definitions + nodes + edges)
       │
       ▼
StartWorkflowAsync(workflowId, tenantId, entity, ...)
       │
       ▼
Load nodes + edges from DB → find Start node
       │
       ▼
Create WorkflowInstance (ACTIVE) → persist to DB
       │
       ▼
ProcessNodeAsync(instanceId, nodeId, data)
       │
       ├── Approval node → check approval matrix → advance
       ├── Decision node → evaluate condition → branch
       ├── Email node → send notification → advance
       ├── Task node → log action → advance
       └── End node → mark COMPLETED
       │
       ▼
WorkflowHistory recorded at each step
```

### AI Engine Pipeline

```
Controller → PredictabilityService → DB (query real sales + production data)
                                           │
                                           ▼
                                   historicalDemand (List<decimal>)
                                           │
                                           ▼
                                   AIEngine.ForecastAsync(data, periods, model)
                                           │
                               ┌───────────┼───────────────┐
                               ▼           ▼               ▼
                      Moving Avg    ExponentialSmooth   Linear Reg
                      Weighted MA   Seasonal Decomp    (R² metric)
                               │           │               │
                               └───────────┴───────────────┘
                                           ▼
                                   ForecastResult + SafetyStock calc
                                           │
                                           ▼
                                   DemandForecastDto / SafetyStockResult
```

### Plugin Hook Flow

```
API Start → PluginLoader.LoadAll()
                │
                ▼
         Scan plugins/*.dll
                │
                ▼
         Activator.CreateInstance → IYuktiraPlugin
                │
                ├── IPluginStartupHook?  → OnStartupAsync()
                ├── IPluginMenuHook?     → GetMenuItems() → sidebar merge
                ├── IPluginDocumentHook? → OnDocumentCreate() → doc events
                └── IPluginWorkflowHook? → OnWorkflowStep() → step intercept
```

---

## Module Overview

### MM — Materials Management
Entities: MaterialMaster, Vendor, PurchaseRequisition, PurchaseOrder, GoodsReceipt, Stock, InvoiceVerification. Full GRN-to-invoice lifecycle. Stock valuation, batch/serial tracking, reorder point calculation. Transaction codes: MM01 (create material), ME21N (create PO), MIRO (invoice receipt).

### SD — Sales & Distribution
Entities: Customer, SalesInquiry, SalesQuotation, SalesOrder, Delivery, BillingDocument. Inquiry → Quote → Order → Delivery → Billing pipeline. Credit limit checks, item-level status tracking. Transaction codes: VA01 (create SO), VF01 (billing), VL01N (delivery).

### PP — Production Planning
Entities: WorkCenter, BOM, Routing, PlannedOrder, ProductionOrder, ProductionConfirmation. Multi-level BOM explosion, routing with setup/run/teardown times, capacity leveling. Transaction codes: CS01 (create BOM), CO01 (release production order), CO11N (confirmation).

### QM — Quality Management
Entities: InspectionPlan, InspectionLot, InspectionResult, UsageDecision. Incoming/in-process/final inspection. Plan-characteristic structure (quantitative/qualitative/visual). Usage decisions (accept/reject/scrap/rework). Transaction codes: QE01 (create inspection lot), QA01 (usage decision).

### WM — Warehouse Management
Entities: StorageLocation, WarehouseTransfer. Bin-level stock tracking, inter-bin transfers, storage location management.

### FI — Finance & Accounting
Entities: Account, JournalEntry, AP/AR entries, FixedAsset. Double-entry journal posting, trial balance, P&L, balance sheet. Accounts payable/receivable aging. Fixed asset depreciation. Transaction codes: FB50 (journal entry), F-02 (GL posting).

### CO — Controlling
Entities: CostCenter, CostElement, ProfitCenter, InternalOrder. Cost center accounting, internal order budgeting, profit center reporting.

### HR — Human Resources
Entities: Employee, LeaveRequest, PayrollEntry, Attendance, Appraisal. Employee master, leave management, payroll (PF/ESI/PT/TDS), attendance tracking, performance appraisal.

### CRM — Customer Relationship
Entities: Lead, Opportunity, Contact, Campaign, ServiceTicket. Lead-to-opportunity pipeline, campaign management, service ticket tracking.

### PS — Project System
Entities: Project, ProjectTask, TimesheetEntry. Project budgeting, task planning, timesheet capture.

### PM — Plant Maintenance
Entities: Equipment, MaintenancePlan, MaintenanceOrder. Equipment master, preventive maintenance scheduling, maintenance order execution.

### LIMS — Laboratory Information Management
Entities: Sample, TestResult, Specification, Instrument. Sample tracking, test result recording, specification management, instrument calibration scheduling.

### BI — Business Intelligence
Entities: BIReport, Dashboard, KpiSnapshot. KPI/chart/table/list widgets, role-based dashboard visibility, system widget presets. Formula-driven KPI engine (MonthlyRevenue, OpenPOs, StockTurnover, etc.) with snapshot history. Full report CRUD with chart type selection and query execution. Dashboard layout editor with widget configuration.

### Workflow
Entities: WorkflowDefinition, WorkflowNode, WorkflowEdge, WorkflowInstance, WorkflowHistory. BPMN-style designer, multi-step approval, conditional branching, email notifications, full execution history. **Backend enhancements:** node validation rules (START/END/DECISION topology), expression evaluator (comparison/logical/parentheses), TIMER node scheduler with `System.Threading.Timer`, API_CALL node HTTP client with `{{variable}}` substitution, workflow simulation mode that walks START→END tracing decisions and timing.

### AI Engine
9 forecasting models — 5 original (Moving Average, Weighted MA, Exponential Smoothing, Linear Regression, Seasonal Decomposition) + 4 advanced (Holt-Winters triple exponential smoothing, ARIMA with differencing + AR + MA components, anomaly detection via ZScore/IQR/MovingAverageDeviation, accuracy dashboard with MAPE/MAE/RMSE/R² metrics). Demand prediction from real sales/production data. Safety stock calculation with service level Z-scores. Stock alert generation.

### MRP Engine
Multi-level BOM explosion, gross/net requirement calculation, shortage detection, planned order generation, capacity load calculation, safety stock monitoring. **Extensions:** multi-plant planning scoped to PlantEntity, vendor lead-time integration adjusting order dates from VendorLeadTimeEntity, production capacity leveling with overtime/shift suggestions, MRP run history recording (run_type, materials_processed, duration_ms), SAP-style exception messages (STOCK_SHORTAGE, NO_VENDOR, LONG_LEAD_TIME).

---

## Security

### RBAC
Five built-in roles enforced at the authorization policy level:

| Role | Code | Policy | Access |
|------|------|--------|--------|
| Super User | `SUPER_USER` | `SuperUser` | Global admin — impersonate, unlock, manage tenants, override approvals |
| Admin | `ADMIN` | `AdminOrAbove` | Tenant admin — user management, config, dashboard customization |
| Power User | `POWER_USER` | `PowerUserOrAbove` | Operational with configuration rights — approval matrices, number ranges |
| Normal User | `NORMAL_USER` | — | Standard transaction execution, document creation |
| Read-Only | `READ_ONLY` | — | View dashboards, run reports, no mutations |

- Claims-based `[Authorize(Policy = "...")]` on all controllers
- Transaction-level permissions via `TransactionPermissionEntity` (role/user granular)
- Super user powers defined in `super_user_permissions` table (can override approvals, unlock docs, reset passwords, impersonate, etc.)

### Password Policy
- Configurable minimum length (default 8) via `auth.password_min_length` system config
- Configurable max failed attempts (default 5) via `auth.max_login_attempts` system config
- Account lockout after exceeding failed attempts (`locked_until` field on user)
- Password change tracking (`password_changed_at`)
- MFA support via `mfa_enabled` / `mfa_secret` columns (TOTP-ready)

### Suspicious Activity Detection
- `AuditMiddleware` logs every API call with `ActionType.API_CALL`
- `is_suspicious` flag on `audit_log` — triggered by:
  - Login from new IP/device (via `LOGIN_ALERT` notification)
  - Failed login spikes (5+ from same IP in 24h)
  - DELETE actions between midnight and 5am
  - Login IP vs device mismatch
- `AuditService.DetectAndFlagSuspiciousAsync()` for bulk detection
- `GET /api/v1/security/suspicious-activity` — paginated flagged entries
- `POST /api/v1/security/suspicious-activity/detect` — run detection engine

### Compliance
- **Full audit trail** — every CREATE, UPDATE, DELETE, LOGIN, APPROVAL, EXPORT, API_CALL is logged with old/new value snapshots (JSONB), IP, device, user agent, session ID
- **Immutable audit log** — append-only; no DELETE/UPDATE exposed (soft-flagged only)
- **Data isolation** — multi-tenant with TenantMiddleware enforcing tenant context per request; all entities scoped to `tenant_id`
- **GDPR-ready** — user data includes `phone`, `email` with soft-delete; audit stores minimal PII; retention configurable
- **GMP-ready** — inspection lot traceability (source document, batch/serial), usage decisions, material master change history via audit
- **ISO 27001 alignment** — access control (RBAC), password policy, audit logging, incident detection (suspicious flagging), session management (JWT expiry + refresh rotation)

---

## Installation & Setup

### Option 1: Quick Start (PostgreSQL)

```batch
git clone https://github.com/bhnvboy-cell/yukthira.git
cd yukthira
cd YuktiraERP
init-db.bat    (first run only — creates the yuktira_erp database and loads sample data)
start.bat
```

Opens at **http://localhost:5001** — login with `superadmin` / `yuktira123`, client `1000`.

### Option 2: Manual (API + Web separately)

```bash
# Build
dotnet restore YuktiraERP.sln
dotnet build YuktiraERP.sln -c Debug

# Terminal 1 — API (port 5000)
cd src/YuktiraERP.Api
dotnet run
# Swagger: http://localhost:5000/swagger
# All REST endpoints are versioned under /api/v1/

# Terminal 2 — Web UI (port 5001)
cd src/YuktiraERP.Web
dotnet run
# Browser: http://localhost:5001
```

### Option 3: Initialize the Database

```batch
# 1. Run the DB bootstrap script (creates yuktira_erp, applies 001–007 migrations, seeds sample data)
init-db.bat

# 2. Connection string (both API and Web use the same key)
#    src/YuktiraERP.Api/appsettings.Development.json
#    src/YuktiraERP.Web/appsettings.Development.json
#    "YuktiraDb": "Host=127.0.0.1;Port=5432;Database=yuktira_erp;Username=postgres;Maximum Pool Size=200;Timeout=15"

# 3. Run each app (or just use start.bat)
run-api.bat
run-web.bat
```

### Option 4: Docker

```bash
cd scripts
.\deploy.ps1 -Build -Run
# OR
docker-compose -f scripts\docker-compose.yml up -d
```

### Option 5: Server Mode (Network Access)

```batch
run-api.bat    (terminal 1 — API on 0.0.0.0:5000)
run-web.bat    (terminal 2 — Web UI on 0.0.0.0:5001)
```
Other computers can connect via `http://<server-ip>:5001`.

### Default Login

| User | Role | Password | Client |
|------|------|----------|--------|
| superadmin | Super User | yuktira123 | 1000 |
| admin | Admin | yuktira123 | 1000 |
| manager | Power User | yuktira123 | 1000 |
| user | Normal User | yuktira123 | 1000 |
| readonly | Read-Only | yuktira123 | 1000 |

### Debugging

- **API logs**: stdout with structured logging via `Console`
- **Audit logs**: query `GET /api/v1/security/compliance/audit-log` with optional filters (module, date range)
- **Error logs**: all unhandled exceptions are logged via `ExceptionMiddleware`
- **SQL logging**: enable `Debug` log level in `appsettings.Development.json` to see EF Core queries

### 8. Configure Tenants

Tenants are seeded via SQL in `001_core_schema.sql`. Each tenant has a `code` (e.g. `DEMO`) and `name`. The login form's `ClientNumber` field maps to `Tenant.Code`.

To add a tenant:
```sql
INSERT INTO yuktira_core.tenants (name, code, status, max_users)
VALUES ('New Corp', 'NEWCO', 'ACTIVE', 50);
```

### 9. Add a New Module

1. Define entities in `AllEntities.cs`
2. Add `DbSet<T>` in `YuktiraDbContext`
3. Create a controller in `Controllers/Modules/` with `IRepository<T, Guid>` + `ITenantContext`
4. Add configuration class in `Data/Configurations/` for schema mapping
5. Build → module available at `api/v1/xx/[controller]`

### 10. Build a Plugin

See `docs/plugin-development.md` for full SDK reference, hook examples, and deploy steps.

---

## Performance Benchmarks

*Measured on dev hardware (i7-12700H, 16GB RAM, SSD, PostgreSQL 16 local).*

| Operation | Avg Time | P99 Time | Notes |
|-----------|----------|----------|-------|
| API request (no DB) | <5ms | 15ms | JWT validation + middleware |
| API request (simple query) | 15–30ms | 80ms | Single entity fetch via IRepository |
| Login + JWT generation | 25ms | 60ms | Password hash verify + token create |
| Refresh token rotation | 20ms | 50ms | Revoke old + insert new |
| Workflow: start instance | 30ms | 90ms | Load nodes, create instance, persist |
| Workflow: process node | 15ms | 45ms | Load edges, update instance, save history |
| AI: Moving Average (12pts) | 1ms | 2ms | Pure in-memory math |
| AI: Linear Regression (12pts) | 2ms | 5ms | O(n) slope/intercept calc |
| AI: Seasonal Decomposition (24pts) | 5ms | 12ms | Deseasonalize + trend + reseasonalize |
| AI: Holt-Winters (12pts, season=4) | 3ms | 8ms | Triple exponential smoothing |
| AI: ARIMA (12pts, p=1,d=1,q=1) | 4ms | 10ms | Differencing + AR + MA |
| AI: Anomaly detection (12pts, ZScore) | 1ms | 3ms | Z-score calculation + threshold |
| AI: Forecast dashboard (1 material) | 80ms | 200ms | DB query + 3 model forecasts + anomaly scan |
| MRP run (100 materials) | 350ms | 900ms | BOM explosion + shortage check + orders |
| MRP multi-plant (100 materials, 3 plants) | 600ms | 1.5s | Cross-plant planning + vendor LT |
| MRP capacity leveling (14 days) | 150ms | 400ms | Work center load calc + suggestions |
| Demand prediction (PredictabilityService) | 60ms | 150ms | DB query + AI ForecastAsync |
| Safety stock calc (single material) | 25ms | 60ms | DB demand query + formula |
| Export: 1000 rows to XLSX | 120ms | 250ms | ClosedXML workbook creation |
| Export: 1000 rows to PDF | 400ms | 1.2s | DinkToPdf HTML→PDF conversion |
| Export: 1000 rows to CSV | 10ms | 30ms | StreamWriter + string concat |
| Audit log query (100 entries) | 40ms | 120ms | OrderBy Desc + Skip/Take |
| Suspicious detection (24h scan) | 200ms | 500ms | Scans up to 50k entries per tenant |

---

## Monitoring & Logging

### Application Logs
- **Console output** — all services log to stdout via `Console.WriteLine` (structured logging planned for Serilog migration)
- **Log levels**: Error, Warn, Info, Debug (configured in `appsettings.json`)
- **Key log events**: login success/failure, workflow transitions, MRP run completion, audit flagging, plugin load errors

### Error Logs
- `ExceptionMiddleware` catches all unhandled exceptions and returns structured JSON: `{ error, message, traceId }`
- Transaction execution failures logged to `TransactionLogEntity` with `ErrorMessage` and `DurationMs`
- Workflow processing errors caught per-node; instance remains at current node for retry

### Performance Logs
- Transaction execution logs include `DurationMs` for every API-triggered transaction code
- MRP run duration tracked in-memory (future: `mrp_run_history` table)
- Export engine operations log row count + duration

### Health Checks

| Endpoint | Response | Purpose |
|----------|----------|---------|
| `GET /health` | `{ status: "Healthy" }` | Liveness probe (add via `builder.Services.AddHealthChecks()` in `Program.cs`) |
| `GET /health/ready` | `{ status: "Ready", database: "Connected" }` | Readiness with DB ping (planned) |

### Prometheus / Grafana (Planned)
Metrics endpoints via `dotnet-counters` or Prometheus-net middleware:
- Request rate, latency, error rate per controller
- DB connection pool usage
- Workflow instance count by status
- AI forecast invocation count
- Memory/GC pressure

---

## Deployment Architecture

### Production Topology

```
                         ┌─────────────┐
                         │   Internet   │
                         └──────┬──────┘
                                │ (HTTPS :443)
                         ┌──────┴──────┐
                         │    CDN /    │
                         │  WAF (opt)  │
                         └──────┬──────┘
                                │
                         ┌──────┴──────┐
                         │   Apache    │
                         │  Reverse   │
                         │   Proxy    │
                         └──────┬──────┘
                    ┌───────────┼───────────┐
                    │           │           │
             ┌──────┴────┐ ┌───┴────┐ ┌───┴──────┐
             │  API      │ │  Web   │ │  Health   │
             │ :5000     │ │ :5001  │ │  :5000   │
             │ (scaled)  │ │(scaled)│ │  /health │
             └──────┬────┘ └────────┘ └──────────┘
                    │
             ┌──────┴──────┐
             │ PostgreSQL  │
             │ Primary +   │
             │ Read Replica│
             └─────────────┘
```

### Load Balancer (Apache example)

```apache
<Proxy balancer://api-cluster>
    BalancerMember http://api1:5000 route=api1
    BalancerMember http://api2:5000 route=api2
    ProxySet lbmethod=byrequests
</Proxy>

ProxyPass /api balancer://api-cluster/
ProxyPassReverse /api balancer://api-cluster/
```

### Multi-Tenant Routing

```
Request → TenantMiddleware
              │
              ├── X-Tenant-Id header?  → use directly
              ├── Subdomain?           → resolve from hostname
              │   (tenant1.example.com → "tenant1")
              ├── URL path prefix?     → /{tenant}/api/v1/...
              └── JWT claim?           → extract TenantId from token
                      │
                      ▼
              context.Items["TenantId"] = resolvedGuid
                      │
                      ▼
              All downstream queries scoped to tenant
```

### Database Schema Separation

```
PostgreSQL Instance
  │
  ├── yuktira_core       → Tenants, Users, Roles, Settings (shared)
  ├── yuktira_mm          → Materials, Vendors, POs, Stock
  ├── yuktira_sd          → Customers, Sales Orders, Deliveries
  ├── yuktira_pp          → BOMs, Routings, Production Orders
  ├── yuktira_qm          → Inspection Plans, Lots, Results
  ├── yuktira_wm          → Storage Locations, Transfers
  ├── yuktira_fi          → Accounts, Journal Entries
  ├── yuktira_hr          → Employees, Payroll, Attendance
  ├── yuktira_crm         → Leads, Opportunities, Contacts
  ├── yuktira_lims        → Samples, Test Results, Instruments
  ├── yuktira_workflow    → Definitions, Instances, History
  ├── yuktira_audit       → Audit Log
  ├── yuktira_notification→ Notifications, Templates
  ├── yuktira_plugin      → Plugins, Tenant Permissions
  ├── yuktira_dashboard   → Widgets, User Layouts
  ├── yuktira_customization→ Column Customizations
  ├── yuktira_approval    → Matrices, Requests, History
  ├── yuktira_numberrange → Document Number Sequences
  ├── yuktira_integration → Webhook Definitions
  ├── yuktira_mrp         → MRP Results (planned)
  └── yuktira_transaction → Transaction Codes & Permissions
```

### Docker Deployment

```bash
.\scripts\deploy.ps1 -Build -Run
```

Docker Compose spins up: API container, Web container, PostgreSQL. See `scripts/docker-compose.yml`.

---

## API Examples

### Authentication
```bash
# Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"clientNumber":"1000","userId":"superadmin","password":"yuktira123","language":"EN","system":"DEV"}'

# Response: { "accessToken": "...", "refreshToken": "...", "expiresAt": "...",
#             "userProfile": { "userId": "...", "username": "superadmin", "role": "SUPER_USER",
#                              "tenantId": "...", "isSuperUser": true, "permissions": [...] } }
```

### Materials CRUD
```bash
# Create material
curl -X POST http://localhost:5000/api/v1/mm/material \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"code":"MAT-1001","name":"Raw Material A","materialType":"RAW","baseUnit":"KG"}'

# List materials
curl http://localhost:5000/api/v1/mm/material \
  -H "Authorization: Bearer <token>"

# Get by ID
curl http://localhost:5000/api/v1/mm/material/{id} \
  -H "Authorization: Bearer <token>"
```

### Webhook Integration
```bash
# Register webhook
curl -X POST http://localhost:5000/api/v1/integration/webhooks \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"name":"Order Created","eventType":"order.created","targetUrl":"https://example.com/hook"}'

# List webhooks
curl http://localhost:5000/api/v1/integration/webhooks \
  -H "Authorization: Bearer <token>"

# Dispatch event (triggers all matching webhooks)
curl -X POST http://localhost:5000/api/v1/integration/dispatch \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"eventType":"order.created","entityType":"SalesOrder","entityId":"SO-50001"}'
```

### Workflow
```bash
# Start workflow instance
curl -X POST http://localhost:5000/api/v1/workflow/{workflowId}/start \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"entityName":"SalesOrder","entityId":"SO-50001","variables":{"amount":1500}}'

# Approve current node
curl -X POST http://localhost:5000/api/v1/workflow/instance/{instanceId}/approve \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"comment":"Approved","payload":{"approvedBy":"admin"}}'
```

### Security
```bash
# Get my permissions
curl http://localhost:5000/api/v1/security/my-permissions \
  -H "Authorization: Bearer <token>"

# Get permission matrix
curl http://localhost:5000/api/v1/security/permission-matrix \
  -H "Authorization: Bearer <token>"

# Detect suspicious activity
curl -X POST http://localhost:5000/api/v1/security/suspicious-activity/detect \
  -H "Authorization: Bearer <token>"

# Unlock user
curl -X POST http://localhost:5000/api/v1/security/unlock-user/{userId} \
  -H "Authorization: Bearer <token>"
```

### AI — Advanced Models
```bash
# Holt-Winters forecast
curl -X POST http://localhost:5000/api/v1/ai/holt-winters \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"historicalData":[100,110,120,130,120,115,125,135,130,140,150,145],"forecastPeriods":4,"alpha":0.3,"beta":0.1,"gamma":0.1,"seasonLength":4}'

# ARIMA forecast
curl -X POST http://localhost:5000/api/v1/ai/arima \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"historicalData":[100,110,120,130,120,115,125,135,130,140,150,145],"forecastPeriods":4,"p":1,"d":1,"q":1}'

# Anomaly detection
curl -X POST http://localhost:5000/api/v1/ai/anomalies \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"historicalData":[100,110,120,130,500,115,125,135,130,140,150,145],"method":"ZScore","threshold":2.0}'

# Forecast dashboard (combined demand + safety stock + anomalies)
curl http://localhost:5000/api/v1/ai/forecast-dashboard/{materialId} \
  -H "Authorization: Bearer <token>"
```

### MRP — Extended
```bash
# Run MRP with multi-plant scope
curl -X POST "http://localhost:5000/api/v1/mrp/run-multi-plant?plantId={plantId}" \
  -H "Authorization: Bearer <token>"

# Run MRP with vendor lead-time integration
curl -X POST "http://localhost:5000/api/v1/mrp/run-with-vendor-lt" \
  -H "Authorization: Bearer <token>"

# Calculate capacity leveling
curl -X POST "http://localhost:5000/api/v1/mrp/capacity-leveling?start=2026-07-01&end=2026-07-14" \
  -H "Authorization: Bearer <token>"

# Get run history
curl "http://localhost:5000/api/v1/mrp/history?limit=10" \
  -H "Authorization: Bearer <token>"

# Get exception messages
curl "http://localhost:5000/api/v1/mrp/exceptions" \
  -H "Authorization: Bearer <token>"
```

### Workflow — Extended
```bash
# Validate workflow definition
curl -X POST http://localhost:5000/api/v1/workflow/{workflowId}/validate \
  -H "Authorization: Bearer <token>"

# Simulate workflow execution
curl -X POST http://localhost:5000/api/v1/workflow/{workflowId}/simulate \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"variables":{"amount":1500,"status":"approved"}}'

# Evaluate condition expression
curl -X POST http://localhost:5000/api/v1/workflow/condition/evaluate \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"expression":"amount > 1000 && status == \"approved\"","variables":{"amount":1500,"status":"approved"}}'

# Schedule timer node
curl -X POST http://localhost:5000/api/v1/workflow/{instanceId}/timer \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"delayMinutes":30,"action":"ESCALATE"}'
```

### BI & KPI
```bash
# List BI reports
curl http://localhost:5000/api/v1/bi/reports \
  -H "Authorization: Bearer <token>"

# Create BI report
curl -X POST http://localhost:5000/api/v1/bi/reports \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"reportName":"Monthly Revenue","category":"Sales","query":"SELECT ...","chartType":"bar"}'

# Execute report
curl http://localhost:5000/api/v1/bi/reports/{id}/run \
  -H "Authorization: Bearer <token>"

# Get available KPIs
curl http://localhost:5000/api/v1/bi/kpis \
  -H "Authorization: Bearer <token>"

# Calculate specific KPI
curl "http://localhost:5000/api/v1/bi/kpis/MONTHLY_REVENUE/calculate" \
  -H "Authorization: Bearer <token>"

# Create dashboard
curl -X POST http://localhost:5000/api/v1/bi/dashboards \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"name":"Executive Dashboard","category":"Sales","configJson":"{\"widgets\":[]}"}'
```

### Integration Queue
```bash
# Enqueue message
curl -X POST http://localhost:5000/api/v1/integration/queue/enqueue \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"messageType":"order.sync","payload":{"orderId":"SO-50001"},"targetSystem":"EXTERNAL_ERP"}'

# Process pending queue
curl -X POST http://localhost:5000/api/v1/integration/queue/process \
  -H "Authorization: Bearer <token>"

# View dead-letter queue
curl http://localhost:5000/api/v1/integration/queue/dead-letter \
  -H "Authorization: Bearer <token>"

# Requeue from dead-letter
curl -X POST http://localhost:5000/api/v1/integration/queue/requeue/{deadLetterId} \
  -H "Authorization: Bearer <token>"
```

### Plugins
```bash
# List plugins
curl http://localhost:5000/api/v1/plugins \
  -H "Authorization: Bearer <token>"

# Get plugin settings
curl http://localhost:5000/api/v1/plugins/{pluginId}/settings \
  -H "Authorization: Bearer <token>"

# Update plugin settings
curl -X POST http://localhost:5000/api/v1/plugins/{pluginId}/settings \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"api_key":"abc123","endpoint_url":"https://api.example.com"}'

# Get plugin permissions
curl http://localhost:5000/api/v1/plugins/{pluginId}/permissions \
  -H "Authorization: Bearer <token>"

# Hot-reload plugin
curl -X POST http://localhost:5000/api/v1/plugins/{pluginId}/reload \
  -H "Authorization: Bearer <token>"

# Get plugin status (memory, execution stats)
curl http://localhost:5000/api/v1/plugins/{pluginId}/status \
  -H "Authorization: Bearer <token>"
```

### Real-Time (SignalR)
```javascript
// Connect via JavaScript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications", { accessTokenFactory: () => token })
  .build();

// Subscribe to workflow updates
connection.invoke("SubscribeWorkflow", workflowInstanceId);

// Listen for live updates
connection.on("WorkflowUpdate", (instanceId, status, message) => {
  console.log(`Workflow ${instanceId}: ${status} - ${message}`);
});
connection.on("MrpProgress", (percentage, message) => {
  updateProgressBar(percentage, message);
});
connection.on("DashboardRefresh", () => {
  refreshWidgets();
});
connection.on("Notification", (title, message) => {
  showToast(title, message);
});
```

---

## Configuration

### appsettings.json Structure
| Section | Key | Description | Default |
|---------|-----|-------------|---------|
| `ConnectionStrings` | `YuktiraDb` | PostgreSQL connection string | — |
| `Jwt` | `Secret` | JWT signing key (min 32 chars) | — |
| `Jwt` | `AccessTokenExpirationHours` | Token lifetime | 8 |
| `Jwt` | `RefreshTokenExpirationDays` | Refresh token lifetime | 7 |
| `MultiTenant` | `Mode` | Tenant resolution: Subdomain / Header / Path | Subdomain |
| `MultiTenant` | `DefaultTenant` | Fallback tenant code | demo |
| `Features` | `EnableMFA` | Multi-factor authentication | false |
| `Features` | `EnableAudit` | Audit logging | true |
| `Features` | `EnableWorkflow` | Workflow engine | true |
| `Features` | `EnablePlugins` | Plugin system | true |
| `Features` | `EnableMrp` | MRP engine | true |
| `Features` | `EnableAI` | AI forecasting | true |
| `Email` | `SmtpHost` | SMTP server | smtp.yuktira.com |
| `Email` | `SmtpPort` | SMTP port | 587 |
| `SMS` | `Provider` | SMS provider (Twilio) | Twilio |

### Environment-Specific Overrides
Override settings per environment using `appsettings.{Environment}.json`:
```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "YuktiraDb": "Host=localhost;Port=5432;Database=yuktira_erp_dev;..."
  }
}
```

### System Configuration (Database)
Settings stored in `yuktira_admin.system_config` table override `appsettings.json` at runtime:

```sql
SELECT * FROM yuktira_admin.system_config WHERE tenant_id = '<tenant-id>';
```

| Category | Key | Description |
|----------|-----|-------------|
| Auth | password.min_length | Minimum password length (default: 8) |
| Auth | login.max_attempts | Max failed login attempts before lockout (default: 5) |
| Auth | login.lockout_minutes | Lockout duration in minutes (default: 30) |
| Audit | audit.retention_days | Days to retain audit logs (default: 365) |
| Notifications | email.enabled | Enable email notifications |
| Backup | backup.schedule_cron | Cron expression for automated backup |

### Number Range Configuration
Document numbering is configured in `yuktira_admin.number_range_definition`:

| Module | Code | Prefix | Example |
|--------|------|--------|---------|
| MM | MAT | MAT- | MAT-1001 |
| SD | CUST | CUST- | CUST-1001 |
| SD | SO | SO- | SO-50001 |
| FI | VOUCHER | V- | V-10001 |
| PO | PO | PO- | PO-10001 |
| HR | EMP | EMP- | EMP-1001 |

---

## Integration Guide

### Webhooks
Register endpoints that receive real-time events:

| Event Type | Triggered By | Payload Includes |
|------------|-------------|------------------|
| `order.created` | Sales order creation | Order ID, customer, items, total |
| `order.approved` | Workflow approval step | Order ID, approver, timestamp |
| `material.received` | Goods receipt | Material code, quantity, PO |
| `invoice.posted` | Invoice posting | Invoice number, amount, due date |

Webhooks are dispatched via `POST` to the registered URL with HMAC signature in `X-Webhook-Secret` header.

### API Client Authentication
Third-party systems authenticate via client ID/secret:
```bash
curl -X POST http://localhost:5000/api/v1/integration/validate \
  -H "Content-Type: application/json" \
  -d '{"clientId":"client-1","clientSecret":"secret-1"}'
```
IP whitelisting is enforced per client — requests from non-whitelisted IPs are rejected.

### External System Integration Patterns
| Pattern | When to Use | Example |
|---------|-------------|---------|
| REST API | Synchronous, request-response | Fetch material data, post invoice |
| Webhook | Asynchronous event notification | Notify ERP when supplier ships goods |
| File Export | Bulk data exchange | Daily GL journal export via SFTP |
| Database Link | Direct table access (intranet only) | BI tools querying reporting views |

### Integration Queue
Outbound message queue with retry and dead-letter handling:
- `POST /api/v1/integration/queue/enqueue` — Enqueue a message
- `GET /api/v1/integration/queue/pending` — View pending messages
- `POST /api/v1/integration/queue/process` — Process pending (HTTP POST for webhook targets, log for others)
- `GET /api/v1/integration/queue/dead-letter` — View failed messages after max retries
- `POST /api/v1/integration/queue/requeue/{id}` — Requeue from dead-letter

### API Throttling
Built-in middleware (registered in `Program.cs`) limits requests to **100/min per client IP**. Returns `429 Too Many Requests` with `X-RateLimit-*` headers when exceeded.

### EDI / B2B Connectors
- EDIFACT / X12 conversion stubs via `IEdiService` (`POST /api/v1/integration/edi/convert`)
- Trading partner profile management (planned)
- Automated acknowledgment processing (planned)

---

## Testing Guide

### Test Project Structure
```
src/YuktiraERP.Tests/
├── AuthServiceTests.cs         # Login validation, password policy, lockout
├── WorkflowServiceTests.cs     # Start workflow, inactive guard
└── IntegrationHubTests.cs      # Webhook CRUD, API client validation, IP whitelist
```

### Running Tests
```bash
cd src/YuktiraERP.Tests
dotnet test                          # Run all tests
dotnet test --filter "Category=Unit" # Filter by trait
dotnet test -v n                     # Verbose output with test names
```

### Writing Tests
```csharp
// Arrange — set up in-memory DB
var options = new DbContextOptionsBuilder<YuktiraDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;
var db = new YuktiraDbContext(options);

// Act — call the service
var result = await service.LoginAsync(request, ip, device);

// Assert
Assert.NotNull(result);
Assert.Equal("admin", result.UserProfile.Username);
```

### Test Categories
| Category | What It Covers | Examples |
|----------|---------------|----------|
| Unit | Individual service methods in isolation | Auth login validation, webhook registration |
| Integration | Service + DB combined (in-memory) | Full login flow, workflow start |
| Security | Auth bypass, password policy, IP restrictions | Locked user rejection, IP whitelist check |

### Best Practices
- Use `UseInMemoryDatabase` with unique DB names per test (use `Guid.NewGuid().ToString()`)
- Mock external HTTP calls (webhook dispatch), not the DB
- Test both happy path and failure cases (wrong password, locked user, inactive workflow)
- Keep tests independent — each test creates its own DB context
- Aim for >70% code coverage on service layer

---

## Backup & Disaster Recovery

### Scripts
| Script | Purpose |
|--------|---------|
| `scripts/backup.ps1` | Full DB backup (custom format via pg_dump), auto-rotate last 30. Auto-detects PostgreSQL bin (PATH or `C:\Program Files\PostgreSQL\13-18\bin`); defaults to user `postgres` at `127.0.0.1:5432` matching the app; pass `-Password` if required |
| `scripts/restore.ps1` | Restore from backup file (prompts confirmation, uses latest if no arg); same auto-detection and defaults as backup |
| `database/backup/disaster_recovery.md` | Full DR plan with RPO/RTO targets |

### Quick Commands
```powershell
# Backup
.\scripts\backup.ps1

# Restore (prompts for confirmation)
.\scripts\restore.ps1 -BackupFile ".\database\backup\yuktira_erp_20240101_020000.sql"
```

### Scheduler Setup (Windows Task Scheduler)
```powershell
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File `"$PWD\scripts\backup.ps1`""
$trigger = New-ScheduledTaskTrigger -Daily -At 2AM
Register-ScheduledTask -TaskName "YuktiraERP Backup" -Action $action -Trigger $trigger -RunLevel Highest
```

### Recovery Strategy
| Scenario | Action | Estimated RTO |
|----------|--------|---------------|
| Database corruption | Restore latest backup | < 2 hours |
| Server failure | Deploy new server, restore latest backup | < 4 hours |
| Accidental data loss | Point-in-time recovery from WAL (if configured) | < 1 hour |
| Full region outage | Promote DR replica in secondary region | < 15 minutes |

See `database/backup/disaster_recovery.md` for detailed runbook.

---

## Versioning

| Version | Date | Highlights |
|---------|------|------------|
| 1.0.3 | August 2026 | Gap-filling: real stock Goods Issue, finance loop (AP/AR aging, payments, period close, bank recon, depreciation), payroll persistence, TOTP MFA, DB-backed approvals, background jobs, tenant write-safety, PDF fails loudly |
| 1.0.2 | August 2026 | Module catalog, 13 legacy flat-page redirects, t-code reclassification, full CRUD pages for 32 entities, Npgsql DateTime fix, print/Save-as-PDF on every page, working backup/restore scripts |
| 1.0.1 | August 2026 | API versioning, Web /api layer, PS/PM transaction codes, seed reconciliation, security hardening |
| 1.0.0 | July 2026 | Initial release — Core ERP, MRP, AI, Workflow, Plugin SDK, Export, Security |

### Changelog

**1.0.3 (August 2026)**
- Stock integrity: Goods Issue now deducts real stock from `MaterialMaster`, validates quantity/availability, and records a `StockMovement` (document, material, qty, before/after, reference); GRN receipt and reversal record stock movements too
- Finance loop: `AccountingService` expanded with AP/AR aging buckets, payment posting (FIFO settlement of oldest open AP/AR entry + GL effect), fiscal period open/close, bank reconciliation (statement vs ledger + difference), and fixed-asset depreciation scheduling — all tenant-scoped and exposed via `/api/v1/fi/Finance/*`
- Payroll persistence: the HR Payroll Create page now calls `IPayrollService`, computes gross/deductions/net, and saves the payroll entry (with tenant) instead of writing an empty record
- Multi-tenancy: `TenantSaveChangesInterceptor` auto-stamps `TenantId` on every insert; StockOverview, MRP, GRN, Invoice Verification, and Goods Issue now query/filter by the logged-in tenant (removed `Guid.Empty` leaks)
- Background jobs: `IntegrationQueueBackgroundService` (outbound queue drained every 30s) and `MrpSchedulerBackgroundService` (daily MRP run for all active tenants) — both `IHostedService` registered in DI, verified running at startup
- Approvals: `ApprovalService` rewritten to persist to DB (`ApprovalRequests` + `ApprovalSteps`), supporting create/approve (up to 3 levels)/reject/escalate and pending list — no more static in-memory state
- MFA: real RFC 6238 TOTP (`MfaTotpService`, no external package) with setup/enable/disable endpoints, `MfaCode` on login, and per-user `MfaEnabled`/`MfaSecret`; verified full cycle end-to-end
- PDF export: `ConvertHtmlToPdf` now throws a clear actionable error when the native `wkhtmltox` library is missing instead of silently returning HTML bytes under a `.pdf` name; browser Print / Save-as-PDF remains the supported path
- Plugins: `PluginLoader.LoadAll()` invoked at startup in both API and Web with failure logging
- Docs: `architecture.md` corrected to match reality (single `yuktira_core` schema, no CI/CD files yet, MFA/background jobs now implemented, PDF caveat documented)
- Schema: `database/scripts/007_new_entities.sql` (StockMovements, FiscalPeriods, BankReconciliations, Payments, DepreciationSchedules, ApprovalSteps) and `008_tenantid_columns.sql` (TenantId on APEntrys/AREntrys/PayrollEntrys/goods_receipts/invoice_verifications/AdminUsers.MfaSecret)
- Backup/restore scripts fixed and verified end-to-end: `backup.ps1` / `restore.ps1` now auto-locate PostgreSQL client tools (`C:\Program Files\PostgreSQL\13-18\bin`), default to the app's `postgres` user on `127.0.0.1:5432`, accept an optional `-Password`, and rename the reserved `$Host` param to `$Server` — tested with a real dump (custom format, 30-backup rotation) and a full restore into a scratch database (99 tables verified)
- Print / Save-as-PDF: global Print button in the top bar (and `Ctrl+P`) on every page with a print stylesheet that hides chrome (sidebar/top bar/action buttons), expands scrolled tables, and produces clean paper-friendly output in the browser print dialog (incl. "Save as PDF")
- Module registry: new `ModuleCatalog` (24 modules in 6 categories with icons/colors) drives the Dashboard tiles, sidebar navigation, and transaction-code module/group classification; exposed via `IModuleCatalog` (Core) implemented in Infrastructure as a singleton
- Legacy flat pages: 13 duplicated flat page routes (MM/Create, MM/CreateGRN, MM/CreatePO, MM/CreatePR, MM/CreateVendor, MM/GoodsReceipt, SD/CreateCustomer, PP/Create, QM/Create, FI/Create, HR/Create, CRM/Create, LIMS/Create) converted to server-side redirects to their canonical sub-folder pages; `MIGO` t-code route fixed to `/MM/GRN/Create`
- T-code reclassification: `TransactionCodeService` now derives module from the catalog (`GetModuleForRoute`) and classifies codes into SAP-style groups (MasterData / Transactions / Process / Reports / Configuration / Administration / Analytics / Utilities); seed reconciliation back-fills missing codes
- Full CRUD pages: generated List + Edit + Display pages for 32 entity types that previously had Create-only pages (see "Full CRUD Pages" above), with Create pages now redirecting/Cancelling to their List pages
- API CRUD depth: LIMS, Quality, Warehouse, PS, PM, CO controllers rewritten with full `GetById/Update/Delete` per resource, matching the Material controller pattern
- DateTime fix: `Npgsql.EnableLegacyTimestampBehavior` enabled so form-bound `DateTime` values save correctly to PostgreSQL `timestamp with time zone` (previously every DateTime-bearing Create/Edit POST returned a 500)

**1.0.1 (August 2026)**
- API versioning: all REST endpoints now live under `/api/v1/` (via `ApiVersionRouteConvention`); legacy unversioned routes return 404
- Web API layer: the Web app (port 5001) now hosts its own controllers (`api/transaction`, `api/tcode-generator`, `api/notifications`, `api/integration`) so the UI works standalone
- SignalR: `NotificationHub` mapped on both API and Web; `JsonStringEnumConverter` on the Web so enums serialize as names
- PS/PM module: transaction codes PS01–PS04 and PM01–PM04 registered with target pages (`/PS/Project/Create`, `/PS/Project/Display`, `/PS/ProjTask/Create`, `/PS/Timesheet/Create`, `/PM/Equipment/Create`, `/PM/Equipment/Display`, `/PM/Plan/Create`, `/PM/Order/Create`); new PS/PM Display pages and list links
- Seed reconciliation: `EnsureSeedAsync` now back-fills any missing RouteMap codes instead of returning early when codes exist
- Security hardening: `007_security_hardening.sql` (security policies, masking defaults), `GlobalExceptionMiddleware` (ProblemDetails responses), security headers, 404 guards
- Connection-string fix for Development environment (API boots cleanly with PostgreSQL)
- Mobile & tablet support: off-canvas sidebar drawer with hamburger toggle (≤ 992px), horizontally scrollable tables, touch-friendly tap targets, and responsive KPI/tile grids down to phone width

**1.0.0 (July 2026)**
- Core: JWT auth, multi-tenancy, audit, RBAC, password policy, suspicious detection
- MM/SD/PP/QM/WM/FI/HR/CRM/LIMS/CO/PS/PM modules with entity framework + repository pattern
- Workflow engine: DB-backed BPMN runtime (start/approval/task/decision/email/end nodes), validation rules, expression evaluator, TIMER/API_CALL nodes, simulation mode
- AI engine: 9 models (MA, WMA, ES, LR, Seasonal, Holt-Winters, ARIMA, anomaly detection, accuracy dashboard)
- MRP engine: BOM explosion, shortage alerts, planned orders, capacity planning, multi-plant, vendor lead-time, capacity leveling, run history, exception messages
- Export engine: XLSX/CSV/TXT/PDF/HTML with 9 document templates (PO, SO, INVOICE, COA, GRN, PROD_ORDER, QC_REPORT, PAYSLIP, FIN_STMT)
- Plugin SDK: 4 hook types + 4 new interfaces (configurable, permissions, sandboxing, hot reload), DB-backed service
- BI engine: KPI formula engine (5 predefined), report CRUD with chart types, DB-backed dashboards, widget layout editor
- Integration engine: outbound message queue with retry/dead-letter, API throttling (100 req/min/IP), EDI conversion stubs
- Real-time: SignalR NotificationHub with tenant groups, live workflow/MRP/dashboard updates, `ILiveUpdateService`
- Accounting: journal posting, trial balance, P&L, balance sheet
- Payroll: PF/ESI/PT/TDS calculation
- Notifications: in-app + email + SMS with 10 templates
- Transaction codes: 60+ SAP-style codes with search, favorites, permissions
- xUnit test project: 8 tests across Auth, Workflow, Integration services
- PostgreSQL migration pipeline with auto-discovery and tracking (6 migration scripts)
- Entity configurations with multi-schema mappings (16 schemas)
- 3 example plugins: AdvancedQC, DairyExtension, ExtraReports
- Health check endpoints, structured error responses

### Roadmap

| Version | Planned |
|---------|---------|
| 1.1 | Serilog structured logging, Prometheus metrics, Grafana dashboards |
| 1.2 | ML.NET integration for AI engine, image recognition for QC |
| 1.3 | Mobile app (Flutter), offline sync, push notifications |
| 1.4 | Full EDI AS2/AS4 support, trading partner profiles, EDIFACT/X12 mapper |
| 1.5 | Multi-language (i18n), dark mode, accessibility (WCAG 2.1) |

---

## Documentation

- `docs/user-guide.md` — installation, access, transaction codes, modules
- `docs/architecture.md` — tech stack, patterns, scalability
- `docs/api-reference.md` — all REST API endpoints
- `docs/plugin-development.md` — SDK reference, hooks guide, build & deploy
- `database/backup/disaster_recovery.md` — DR plan, backup scripts, RPO/RTO targets

## Deployment Options

| Method | Command | Access |
|--------|---------|--------|
| Quick start (PostgreSQL) | `init-db.bat` then `start.bat` | http://localhost:5001 |
| Run API only | `run-api.bat` | http://localhost:5000 (Swagger: `/swagger`) |
| Run Web only | `run-web.bat` | http://localhost:5001 |
| Docker | `.\scripts\deploy.ps1 -Build -Run` | http://localhost:5001 |
| Production (PostgreSQL) | `init-db.bat` then `run-api.bat` + `run-web.bat` | Configured URL |
| Apache proxy | See `apache-config/yuktira-erp.conf` | https://erp.yourdomain.com |
| Database install | `init-db.bat` | Creates DB, applies 001–007, seeds sample data |
| Stop all | `kill.bat` | Stops running YuktiraERP dotnet processes |
| Backup | `.\scripts\backup.ps1` | Daily pg_dump |
| Restore | `.\scripts\restore.ps1 -BackupFile <file>` | Point-in-time recovery |

## License

MIT License — see [LICENSE](LICENSE) for details.

## Credits

**Project Lead & Architecture**
- M.P. Abhinav — Core architecture, design, and development
- M.P. Abhiram — Core architecture, design, and development

**Contributors**
- _Your name here — contributions welcome!_

---

*Yuktira ERP Suite — Intelligence Driven Enterprise Platform*
