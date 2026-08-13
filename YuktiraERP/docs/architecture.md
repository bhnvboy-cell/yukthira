# YUKTIRA ERP SUITE — Architecture Document

## Overview
Yuktira (Sanskrit: युक्ति — "logic, intelligence, strategic reasoning") is a comprehensive enterprise ERP platform built on ASP.NET Core. It features multi-tenancy, a BPMN workflow engine, AI forecasting, MRP, a plugin SDK, an export engine, and real-time notifications via SignalR.

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Frontend | ASP.NET Core Razor Pages, Bootstrap 5, jQuery, Chart.js, SignalR JS client |
| API | ASP.NET Core Web API 10.0, REST, JWT |
| Backend | .NET 10, C# 13 |
| Database | PostgreSQL (single `yuktira_core` schema), Entity Framework Core |
| Real-Time | SignalR (NotificationHub) |
| Reverse Proxy | Apache HTTP Server 2.4 (apache-config/ provided) |
| Container | Docker (Dockerfile.api, Dockerfile.web, docker-compose.yml provided) |
| CI/CD | GitHub Actions (recommended — no workflow files checked in yet) |

## Solution Structure

```
YuktiraERP/
├── src/
│   ├── YuktiraERP.Core/            # Domain models, interfaces, DTOs
│   ├── YuktiraERP.Infrastructure/  # EF Core, services, multi-tenant, SignalR hub
│   ├── YuktiraERP.Api/            # REST API (port 5000), middleware
│   ├── YuktiraERP.Web/            # Razor Pages Web UI (port 5001)
│   ├── YuktiraERP.Tests/          # xUnit unit/integration tests
│   ├── YuktiraERP.WorkflowEngine/ # BPMN workflow runtime
│   ├── YuktiraERP.AIEngine/       # ML forecasting models
│   ├── YuktiraERP.ExportEngine/   # XLSX/CSV/PDF/HTML export — 9 templates
│   ├── YuktiraERP.PluginSdk/      # Plugin SDK — hooks, hot reload, sandboxing
│   └── plugins/                    # Example plugins (AdvancedQC, Dairy, Reports)
├── database/
│   ├── scripts/                    # Schema/data scripts
│   └── backup/                     # Disaster recovery runbook + scripts
├── scripts/                        # Build, backup/restore scripts
├── apache-config/                  # Reverse proxy config
└── docs/                           # Documentation
```

## Multi-Tenant Architecture

- **Isolation**: Shared database with a `tenant_id` column on tenant-scoped entities; the `TenantSaveChangesInterceptor` automatically stamps `TenantId` on insert, and queries are filtered via `ITenantContext`.
- **Resolution Modes**: HTTP header (`X-Tenant-Id`), JWT claim; subdomain/URL-segment modes are supported by `TenantMiddleware` design.
- **Middleware**: `TenantMiddleware` resolves and injects tenant context per request.
- **Data layout**: Single schema (`yuktira_core`); tables named by pluralized entity names (e.g. `MaterialMasters`).

## Authentication Flow

1. User submits Login Request (Client Number, User ID, Password)
2. Server validates credentials, checks lockout status, enforces password policy
3. Generates JWT (access + refresh tokens); refresh token rotation with revocation
4. Cookie-based auth for Web UI, Bearer token for API
5. **MFA (TOTP)** — RFC 6238 codes via authenticator app; setup/enable/disable endpoints
6. Account lockout on max failed attempts; login/IP/device logging

## Module Architecture

Each ERP module (MM, SD, PP, QM, WM, FI, HR, CRM, LIMS, BI, CO, PS, PM):
- Uses shared services (audit, notification, workflow, approval)
- Integrates via common interfaces and events
- Supports plugin extensions via Plugin SDK
- Transaction codes (SAP-style) for navigation
- Sits in the shared `yuktira_core` schema (no per-module schemas)

## Key Design Patterns

- **Domain-Driven Design**: Core domain models with rich behavior
- **Service Layer**: `I*Service` interfaces consumed by controllers/pages
- **Strategy Pattern**: Plugin engine, approval matrix, AI forecasting models
- **Observer Pattern**: Notification triggers, SignalR hub
- **Chain of Responsibility**: Workflow engine, multi-level approval
- **Middleware Pipeline**: Tenant resolution → Audit logging → Exception handling → API throttling

## Engine Layer

### Workflow Engine
DB-backed BPMN runtime with node types: START, TASK, APPROVAL, DECISION, TIMER, API_CALL, EMAIL, SMS, CONDITION, END. Conditional edge evaluation, expression evaluator, simulation mode, full execution history.

### AI Engine
Forecasting models including Moving Average, Weighted MA, Exponential Smoothing, Linear Regression, Seasonal Decomposition, Holt-Winters, and anomaly detection (ZScore/IQR/MAD), with an accuracy dashboard (MAPE/MAE/RMSE/R²).

### MRP Engine
Multi-level BOM explosion, gross/net requirement calculation, shortage detection, planned order generation, capacity leveling, multi-plant planning, vendor lead-time integration, SAP-style exception messages, run history tracking, and a daily scheduled background run for all tenants.

### Export Engine
XLSX/CSV/TXT/HTML output with 9 document templates (PO, SO, INVOICE, COA, GRN, PROD_ORDER, QC_REPORT, PAYSLIP, FIN_STMT). PDF output is available in the Web UI via browser Print / Save-as-PDF; server-side PDF (DinkToPdf) requires the native `wkhtmltox` library to be installed and reports a clear error if it is missing.

### Plugin SDK
Hook interfaces (`IPluginStartupHook`, `IPluginMenuHook`, `IPluginDocumentHook`, `IPluginWorkflowHook`), hot reload, sandboxed execution. `PluginLoader.LoadAll()` is invoked at application startup and logs any failures.

### Background Jobs
`IntegrationQueueBackgroundService` processes outbound integration messages every 30 seconds; `MrpSchedulerBackgroundService` runs MRP shortage detection once per day for all active tenants. Both are `IHostedService` implementations registered in DI.

## Real-Time (SignalR)

`NotificationHub` at `/hubs/notifications` provides:
- Live workflow updates (`WorkflowUpdate`)
- MRP progress tracking (`MrpProgress`)
- Dashboard refresh triggers (`DashboardRefresh`)
- In-app notifications (`Notification`)
- Tenant group isolation via `ITenantContext`

## Scalability

- Stateless API design for horizontal scaling
- Apache load balancing across multiple API nodes (`balancer://api-cluster`)
- PostgreSQL connection pooling
- API throttling middleware (100 req/min per client IP)

## Security

- **RBAC**: Super User, Admin, Power User, Normal User, Read-Only — enforced via `[Authorize(Policy = "...")]`
- **Password Policy**: Configurable min length, max failed attempts, lockout duration, password change tracking
- **MFA**: Optional TOTP two-factor authentication per user
- **Audit Trail**: CREATE/UPDATE/DELETE/LOGIN/APPROVAL/EXPORT/API_CALL logged with snapshots, IP, device, user agent
- **Compliance**: GDPR-ready, GMP-ready, ISO 27001 alignment, append-only audit log