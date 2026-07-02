# YUKTIRA ERP SUITE — Architecture Document

## Overview
Yuktira (Sanskrit: युक्ति — "logic, intelligence, strategic reasoning") is a comprehensive enterprise ERP platform built on modern, scalable architecture. It features multi-tenancy, a BPMN workflow engine, AI forecasting, MRP, plugin SDK, export engine, and real-time notifications via SignalR.

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Frontend | ASP.NET Core Razor Pages, Bootstrap 5, jQuery, Chart.js, SignalR JS client |
| API | ASP.NET Core Web API 10.0, REST, JWT |
| Backend | .NET 10, C# 13 |
| Database | PostgreSQL 16 (18 schemas), Entity Framework Core |
| Real-Time | SignalR (NotificationHub) |
| Reverse Proxy | Apache HTTP Server 2.4 |
| Container | Docker, Docker Compose |
| CI/CD | GitHub Actions (recommended) |

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
│   ├── YuktiraERP.AIEngine/       # ML forecasting (9 models)
│   ├── YuktiraERP.ExportEngine/   # XLSX/CSV/PDF/HTML export — 9 templates
│   ├── YuktiraERP.PluginSdk/      # Plugin SDK — 4 hook types, hot reload, sandboxing
│   └── plugins/                    # Example plugins (AdvancedQC, Dairy, Reports)
├── database/
│   ├── scripts/                    # Migration scripts (001–006)
│   └── backup/                     # Disaster recovery runbook
├── scripts/                        # Docker, deploy, build scripts
├── apache-config/                  # Reverse proxy config
└── docs/                           # Documentation
```

## Multi-Tenant Architecture

- **Isolation**: Shared database, `tenant_id` column on all tenant entities
- **Resolution Modes**: Subdomain (`tenant.yourcompany.com`), URL segment (`/tenant/`), HTTP header (`X-Tenant-Id`), JWT claim
- **Middleware**: `TenantMiddleware` resolves and injects tenant context per request; all downstream queries scoped to tenant
- **Tenant Data**: 16 database schemas (`yuktira_core`, `yuktira_mm`, `yuktira_sd`, `yuktira_fi`, etc.) with per-tenant row-level isolation

## Authentication Flow

1. User submits Login Request (Client Number, User ID, Password)
2. Server validates credentials, checks lockout status, enforces password policy
3. Generates JWT (access + refresh tokens); refresh token rotation with revocation
4. Cookie-based auth for Web UI, Bearer token for API
5. Optional MFA (TOTP), IP/device logging, account lockout on max failed attempts
6. Suspicious activity detection (new IP, failed login spikes, after-hours DELETE, IP/device mismatch)

## Module Architecture

Each ERP module (MM, SD, PP, QM, WM, FI, HR, CRM, LIMS, BI, CO, PS, PM):
- Has its own PostgreSQL schema (`yuktira_mm`, `yuktira_sd`, etc.)
- Uses shared services (audit, notification, workflow, approval)
- Integrates via common interfaces and events
- Supports plugin extensions via Plugin SDK
- Transaction codes (60+ SAP-style) for navigation

## Key Design Patterns

- **Domain-Driven Design**: Core domain models with rich behavior
- **Repository Pattern**: `IRepository<T, TId>` data access abstraction
- **Strategy Pattern**: Plugin engine, approval matrix, AI forecasting models
- **Observer Pattern**: Webhooks, notification triggers, SignalR hub
- **Chain of Responsibility**: Workflow engine, multi-level approval
- **Middleware Pipeline**: Tenant resolution → Audit logging → Exception handling → API throttling

## Engine Layer

### Workflow Engine
DB-backed BPMN runtime with node types: START, TASK, APPROVAL, DECISION, TIMER, API_CALL, EMAIL, SMS, CONDITION, END. Conditional edge evaluation, expression evaluator, simulation mode, full execution history.

### AI Engine
9 forecasting models: Moving Average, Weighted MA, Exponential Smoothing, Linear Regression, Seasonal Decomposition, Holt-Winters (triple exponential smoothing), ARIMA (differencing + AR + MA), anomaly detection (ZScore/IQR/MAD), accuracy dashboard (MAPE/MAE/RMSE/R²).

### MRP Engine
Multi-level BOM explosion, gross/net requirement calculation, shortage detection, planned order generation, capacity leveling, multi-plant planning, vendor lead-time integration, SAP-style exception messages, run history tracking.

### Export Engine
XLSX/CSV/TXT/PDF/HTML output with 9 document templates: PO, SO, INVOICE, COA, GRN, PROD_ORDER, QC_REPORT, PAYSLIP, FIN_STMT.

### Plugin SDK
4 hook interfaces: `IPluginStartupHook`, `IPluginMenuHook`, `IPluginDocumentHook`, `IPluginWorkflowHook`. Hot reload, sandboxed execution, per-tenant enable/disable, DB-backed settings and permissions.

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
- Health check endpoints: `GET /health`, `GET /health/ready`

## Security

- **RBAC**: Super User, Admin, Power User, Normal User, Read-Only — enforced via `[Authorize(Policy = "...")]`
- **Password Policy**: Configurable min length, max failed attempts, lockout duration, password change tracking
- **Audit Trail**: Every CREATE/UPDATE/DELETE/LOGIN/APPROVAL/EXPORT/API_CALL logged with old/new JSONB snapshots, IP, device, user agent
- **Compliance**: GDPR-ready, GMP-ready, ISO 27001 alignment, immutable audit log (append-only)
- **Suspicious Detection**: Automated flagging of anomalous activity patterns
