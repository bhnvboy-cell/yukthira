<div align="center">

# YuktiraERP v1.1.0

### Open-Source Enterprise Resource Planning

**.NET 10 · PostgreSQL 18 · GraphQL · SignalR · AI/ML**

[![Tests](https://img.shields.io/badge/tests-275%20passing-brightgreen)]()
[![Version](https://img.shields.io/badge/version-1.1.0-blue)]()
[![License](https://img.shields.io/badge/license-open%20source-green)]()

> **99%+ cost savings vs SAP S/4HANA · 90%+ vs Dynamics 365**

</div>

---

## Quick Start

```bash
# Clone
git clone https://github.com/bhnvboy-cell/yukthira.git
cd YuktiraERP

# Database (PostgreSQL 18)
createdb yuktira_erp

# Build & Run
dotnet restore
dotnet build
dotnet run --project src/YuktiraERP.Api --urls http://localhost:5000 &
dotnet run --project src/YuktiraERP.Web --urls http://localhost:5001

# Open browser
# http://localhost:5001
# Login: superadmin / yuktira123 / Client: 1000
```

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                       CLIENT LAYER                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │  Web Browser  │  │  Mobile/RF   │  │  External Systems    │  │
│  │  (Razor Pages)│  │  (SignalR)   │  │  (SAP, Oracle, MES)  │  │
│  └──────┬───────┘  └──────┬───────┘  └────────────┬─────────┘  │
└─────────┼─────────────────┼───────────────────────┼─────────────┘
          │                 │                       │
┌─────────┼─────────────────┼───────────────────────┼─────────────┐
│         ▼                 ▼                       ▼             │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  ASP.NET Core 10 — Middleware Pipeline (17 stages)      │   │
│  │  JWT Auth → CORS → Throttling → Tenant → Audit → ...   │   │
│  └──────────────────────────────────────────────────────────┘   │
│         │                                                        │
│  ┌──────┴──────────────────────────────────────────────────┐    │
│  │  SERVICE LAYER (70+ registrations, 75+ service files)  │    │
│  │  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────────┐    │    │
│  │  │ REST │ │GraphQL│ │SignalR│ │  AI  │ │ Workflow │    │    │
│  │  │ API  │ │  15   │ │  2   │ │ Eng  │ │  Engine  │    │    │
│  │  │55 ctrl│ │types │ │ hubs │ │      │ │  (FSM)   │    │    │
│  │  └──────┘ └──────┘ └──────┘ └──────┘ └──────────┘    │    │
│  └────────────────────┬───────────────────────────────────┘    │
│                       │                                        │
│  ┌────────────────────┼───────────────────────────────────┐    │
│  │  DATA LAYER (191 entities, PostgreSQL 18)              │    │
│  │  EF Core · Multi-Tenant · Auto-Audit · Batch/Serial   │    │
│  └────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

### Solution Structure

```
YuktiraERP/
├── src/
│   ├── YuktiraERP.Core/              Domain models, interfaces, DTOs
│   ├── YuktiraERP.Infrastructure/    Services, DB context, security
│   ├── YuktiraERP.WorkflowEngine/    FSM-based workflow runtime
│   ├── YuktiraERP.AIEngine/          OCR, predictive analytics
│   ├── YuktiraERP.ExportEngine/      CSV, Excel, PDF generation
│   ├── YuktiraERP.PluginSdk/         Plugin interfaces, hot-loading
│   ├── YuktiraERP.Api/               REST + GraphQL + SignalR
│   ├── YuktiraERP.Web/               Razor Pages frontend
│   └── YuktiraERP.Tests/             275 unit/integration tests
├── database/
│   └── scripts/                      SQL migration scripts
└── report.md                         Development progress report
```

---

## Modules (28)

| Category | Modules |
|----------|---------|
| **Operations** | MM · SD · WM · PP · QM · PM · CR · RF · WV · VS |
| **Finance** | FI · CO · UJ · TX · CN |
| **People** | HR · CRM |
| **Projects & Labs** | PS · LIMS |
| **Analytics** | BI · AI · PD |
| **Compliance** | SX |
| **System** | WF · APP · NOT · TCD · TCG · AUD · ADM · CST · INT · PLG · ME |

---

## E2E Pipeline (SAP Transaction Codes)

### Procure-to-Pay (P2P)

```
ME21N ──► MIGO (101) ──► QA11 ──► MIRO
Create PO   Goods Rcp    Usage Dec   Invoice
   │            │            │           │
   └────────────┴────────────┴───────────┘
              Movement Types: 101, 102, 103
```

### Order-to-Cash (O2C)

```
VA01 ──► VL01N ──► QC21 ──► VL02N (PGI) ──► VF01
Create SO  Delivery  COA     Goods Issue    Billing
   │          │        │          │             │
   └──────────┴────────┴──────────┴─────────────┘
              Movement Types: 601, 602
```

### Quality Management (QM)

```
MIGO (101) ──► Inspection Lot ──► QA32 ──► QA11 ──► 321 ──► Unrestricted
    │              │                │          │         │
    │              │                │          │         └── Quality Release
    │              │                │          └── Usage Decision
    │              │                └── Results Recording
    │              └── Auto-created for QM-enabled materials
    └── Goods Receipt with Movement Type 101
```

### UD Reversal (ZQIC Equivalent)

```
QA11 (Completed) ──► ReverseUD ──► Movement Type 322 ──► InInspection
     │                  │                │                    │
     │                  │                │                    └── Lot status reset
     │                  │                └── Stock: Unrestricted → QualityInspection
     │                  └── Full audit trail + reason
     └── Usage Decision reversal engine
```

### E2E Pipeline Diagnostic

```
PO ──► GR(101) ──► Inspection Lot ──► UD ──► Release(321) ──► SO ──► Delivery ──► Billing ──► FI
 │         │              │             │           │           │          │            │         │
 └─────────┴──────────────┴─────────────┴───────────┴───────────┴──────────┴────────────┴─────────┘
                                    11-Step Automated Verification Engine
```

---

## API Reference

### REST API (55 Controllers)

| Module | Route | Operations |
|--------|-------|-----------|
| MM | `/api/mm/*` | Material, Vendor, PR, PO, GRN, Batch, Stock, MMBE |
| SD | `/api/sd/*` | Customer, SO, Delivery, Billing, VF03 Pricing |
| PP | `/api/pp` | Production Order, BOM, Routing |
| QM | `/api/qm` | Inspection Lot, Notification, Usage Decision, UD Reversal |
| FI | `/api/fi/*` | GL, AP, AR, Tax, Currency, Bank, FB03 |
| CO | `/api/co` | Cost Center, Profit Center, Internal Order |
| PM | `/api/pm` | Equipment, Maintenance Order, Plan |
| HR | `/api/hr` | Employee, Payroll, Attendance |
| WM | `/api/wm` | Transfer, Storage Location, RF |
| Pipeline | `/api/PipelineDiagnostic/*` | E2E Diagnostic Engine |

### New API Endpoints

```bash
# E2E Pipeline Diagnostic (11-step verification)
POST /api/PipelineDiagnostic/execute              # Full pipeline run
POST /api/PipelineDiagnostic/validate/po           # Validate PO
POST /api/PipelineDiagnostic/validate/gr           # Validate GR (101)
POST /api/PipelineDiagnostic/validate/inspection-lot # Validate Inspection Lot
POST /api/PipelineDiagnostic/validate/usage-decision # Validate UD
POST /api/PipelineDiagnostic/validate/quality-release # Validate Release (321)
POST /api/PipelineDiagnostic/validate/delivery     # Validate Delivery
POST /api/PipelineDiagnostic/validate/billing      # Validate Billing (VF01)
POST /api/PipelineDiagnostic/validate/fi-ledger    # Validate FI Ledger (FB03)
POST /api/PipelineDiagnostic/validate/stock-integrity # Validate Stock

# UD Reversal
POST /api/qm/usage-decisions/reverse              # Reverse UD (322)

# Stock Overview (MMBE)
POST /api/mm/stock-overview/mmbe                  # Hierarchical stock view

# Movement Types
POST /api/mm/movement-types/post                  # Post movement with validation
POST /api/mm/movement-types/validate              # Validate before posting
```

### GraphQL

**Endpoint:** `POST /api/graphql`

```graphql
query {
  dashboard {
    kpis { name value unit trend }
    inventory { totalMaterials totalStockValue lowStockCount }
    sales { totalOrders totalRevenue pendingOrders }
    quality { totalLots passRate }
    financial { totalDebits totalCredits netBalance }
  }
}
```

### SignalR Hubs

| Hub | URL | Events |
|-----|-----|--------|
| Notifications | `/hubs/notifications` | `ReceiveNotification` |
| Dashboard | `/hubs/dashboard` | `DashboardUpdate`, `StockChange`, `OrderUpdate`, `SoxViolation` |

---

## Web UI Pages (Razor Pages)

| Module | Page | TCode | Description |
|--------|------|-------|-------------|
| MM | `/MM/StockOverview` | MMBE | SAP-style hierarchical stock overview |
| QM | `/QM/Inspection/QA32` | QA32 | Inspection lot selection screen |
| QM | `/QM/Inspection/QA33` | QA33 | Inspection results recording |
| QM | `/QM/Inspection/ReverseUD` | ZQIC | UD Reversal & Stock Reversion |
| QM | `/Quality/PipelineDiagnostic` | — | E2E Pipeline Diagnostic Engine |
| SD | `/SD/Billing/VF03` | VF03 | Billing document pricing conditions |

---

## Security

| Feature | Implementation |
|---------|---------------|
| JWT Authentication | Symmetric key, no clock skew |
| Cookie Auth (Web) | 8h expiry, HttpOnly, SameSite=Lax |
| MFA | TOTP (Google Authenticator) |
| Password Policy | Configurable min length, max attempts, lockout |
| Security Headers | X-Content-Type, X-Frame-Options, Referrer-Policy |
| API Rate Limiting | 100 req/min (configurable) |
| SOX Compliance | Segregation of duties, immutable audit trail (SHA-256) |
| Multi-Tenant | Tenant isolation via interceptor |

### Roles

```
SUPER_USER → ADMIN → POWER_USER → READ_ONLY
```

---

## Plugin SDK

```csharp
public class MyPlugin : IYuktiraPlugin, IPluginMenuHook
{
    public string Id => "my-plugin";
    public string Name => "My Plugin";
    public string Version => "1.0.0";

    public IEnumerable<PluginMenuItem> GetMenuItems(PluginContext ctx)
    {
        yield return new PluginMenuItem { Code = "MYMOD", Name = "My Module", Route = "/MyModule" };
    }
}
```

Drop DLL in `plugins/` → auto-loaded at startup.

### Built-in Connectors

SAP S/4HANA · SAP HANA · Oracle ERP · MES · LIMS

---

## Movement Types Supported

| MT | Description | Usage |
|----|-------------|-------|
| 101 | Goods Receipt | PO receipt into inventory/QI |
| 102 | GR Reversal | Reverse goods receipt |
| 103 | GR Blocked Stock | Receipt into GR blocked stock |
| 110 | Conditional GR Release | Release from GR blocked |
| 321 | Quality Release | QI → Unrestricted |
| 322 | UD Reversal | Unrestricted/Blocked → QI |
| 350 | Custom Transfer | Custom UD reversal transfers |
| 601 | Goods Issue | Outbound delivery |
| 602 | GI Reversal | Reverse goods issue |
| 261 | GI Production | Issue to production order |
| 101 | GR Production | Receipt from production |

---

## Performance

| Metric | Value |
|--------|-------|
| Requests/sec (API) | 1,200+ |
| GraphQL queries/sec | 600+ |
| SignalR connections | 500+ concurrent |
| API P95 latency | < 150ms |
| Memory (idle) | 150MB |

---

## Test Coverage

**275/275 tests passing** across 20+ categories:

- QC, PM, PP, Procurement, Sales, Cross-module
- Customer complaint & return (12 tests)
- Universal journal, SOX compliance, RF warehouse
- Wave pick, velocity slotting, PP/DS scheduling
- MRP events, consolidation, localization tax
- AI document OCR, predictive analytics
- Pricing engine (13 tests)
- E2E pipeline validation

```bash
dotnet test src/YuktiraERP.Tests
```

---

## Commercial ERP Comparison

| Feature | YuktiraERP | SAP S/4HANA | Oracle Fusion | D365 |
|---------|:----------:|:-----------:|:-------------:|:----:|
| License Cost | **Free** | $$$$$ | $$$$$ | $$$$ |
| Source Code | Open | Closed | Closed | Closed |
| 191 Entities | ✅ | ✅ | ✅ | ✅ |
| Universal Journal | ✅ | ✅ | ✅ | ✅ |
| SOX Compliance | ✅ | ✅ | ✅ | ✅ |
| Wave Pick | ✅ | ✅ | ✅ | ✅ |
| PP/DS Scheduling | ✅ | ✅ | ✅ | ✅ |
| Event-Driven MRP | ✅ | ✅ | ✅ | ✅ |
| GraphQL API | ✅ | ❌ | ✅ | ✅ |
| Real-time Dashboard | ✅ | ✅ | ✅ | ✅ |
| Custom Workflows | ✅ | ✅ | ✅ | ✅ |
| Plugin System | ✅ | ✅ | ✅ | ✅ |
| Mobile RF | ✅ | ✅ | ✅ | ✅ |
| AI/ML Built-in | ✅ | Limited | ✅ | ✅ |
| E2E Pipeline Diagnostic | ✅ | ❌ | ❌ | ❌ |
| UD Reversal Engine | ✅ | ✅ | ✅ | ✅ |
| **TCO (5 years)** | **$0** | **$2M-10M** | **$1M-5M** | **$500K-2M** |

---

## Environment

- .NET 10 SDK
- PostgreSQL 18
- Connection: `Host=localhost;Database=yuktira_erp;Username=postgres;Password=(trust)`

---

## License

Open Source — Free for commercial and personal use.

---

<div align="center">

**v1.1.0** · Built with ❤️ to democratize enterprise ERP

[GitHub](https://github.com/bhnvboy-cell/yukthira)

</div>
