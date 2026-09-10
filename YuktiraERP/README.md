<div align="center">

# YuktiraERP v2.0.0

### Open-Source Enterprise Resource Planning

**.NET 10 · PostgreSQL 18 · GraphQL · SignalR · AI/ML**

[![Tests](https://img.shields.io/badge/tests-275%20passing-brightgreen)]()
[![Version](https://img.shields.io/badge/version-2.0.0-blue)]()
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
│  │  ASP.NET Core 10 — Middleware Pipeline (18 stages)      │   │
│  │  JWT Auth → CORS → Throttling → Tenant → Currency →    │   │
│  │  Audit → Security → ...                                 │   │
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
│   │   ├── Interfaces/               IMlEngineServices, IOfflineSyncServices, IEDiServices, IEventStoreService
│   │   ├── Dtos/                     MlEngineDtos, OfflineSyncDtos, EdiDtos, EventStoreDtos
│   │   └── Enums/                    ZqmEnums, MlEngineEnums, OfflineSyncEnums, EdiEnums, EventStoreEnums
│   ├── YuktiraERP.Infrastructure/    Services, DB context, security
│   │   ├── MultiTenant/              TenantCultureMiddleware, TenantMiddleware
│   │   ├── Services/                 ZQM* (10), QualityVisionInspectionEngine, NaturalLanguageQueryEngine,
│   │   │                             OfflineQueueService, EdiAs2Handler, EventStoreService
│   │   ├── Hubs/                     NotificationHub, YuktiraNotificationHub (mobile push)
│   │   └── Data/Configurations/      QmEntityConfiguration, AllEntities
│   ├── YuktiraERP.WorkflowEngine/    FSM-based workflow runtime
│   ├── YuktiraERP.AIEngine/          OCR, predictive analytics (ML.NET ready)
│   ├── YuktiraERP.ExportEngine/      CSV, Excel, PDF generation
│   ├── YuktiraERP.PluginSdk/         Plugin interfaces, hot-loading
│   ├── YuktiraERP.Api/               REST + GraphQL + SignalR
│   │   └── Controllers/Modules/      ZqmSuiteController, etc.
│   ├── YuktiraERP.Web/               Razor Pages frontend
│   │   ├── Pages/Shared/             _Layout.cshtml, _TopNavigation.cshtml
│   │   └── wwwroot/
│   │       ├── css/                  yuktira.css, enterprise-form.css, tcode-engine.css
│   │       └── js/                   yuktira.js (YuktiraFormat), enterprise-form.js
│   └── YuktiraERP.Tests/             275 unit/integration tests
├── database/
│   └── scripts/                      043_zqm_suite.sql, 044_v2_roadmap_tables.sql
└── README.md
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

## Previous: v1.2.0 Features

### Quality Management Extensions (ZQM-01 → ZQM-10)

| TCode | Module | Description |
|-------|--------|-------------|
| ZQM01 | Auto Inspection Lot Generator | Auto-create inspection lots on GR with configurable origins |
| ZQM02 | Results Recording Workbench | ALV grid for mass results entry across inspection lots |
| ZQM03 | Usage Decision Engine | Auto-score, auto-approve, CAB integration |
| ZQM04 | UD Reversal Engine | Reverse completed UDs, stock reversion (322), audit trail |
| ZQM05 | Handling Unit Management | Nesting, split, merge, weight/volume calc |
| ZQM06 | QA Worklist Dashboard | Priority-sorted worklist with aging, overdue alerts |
| ZQM07 | Non-Conformance Manager | NCR lifecycle, disposition, CAPA linkage |
| ZQM08 | Lab Calculator | Mean, stddev, Cp, Cpk, OOT flagging |
| ZQM09 | Certificate of Analysis Generator | Auto COA from results, PDF/HTML, digital signature |
| ZQM10 | QM Pipeline Diagnostic | End-to-end QM flow validation engine |

### Tenant-Aware Currency Formatting

- **Middleware**: `TenantCultureMiddleware` resolves base currency per tenant from DB
- **Base class**: `YuktiraPageModel` exposes `FormatCurrency()`, `ActiveCurrencySymbol`, `ActiveLocale`
- **Client-side**: `YuktiraFormat.currency()`, `.number()`, `.percentage()`, `.date()` using `Intl.NumberFormat`
- **Layout injection**: `window.YuktiraConfig` with tenant currency + locale
- Zero manual `CultureInfo` setup — currency follows tenant automatically

### Screen Zoom / Density Control

- **5 zoom levels**: 80% Compact · 90% Dense · 100% Standard · 110% Large · 120% Extra Large
- **Persistence**: `localStorage` — zoom level persists across sessions
- **Engine**: CSS custom property `--erp-content-scale` + `fontSize` scaling
- **Top header**: Redesigned 38px height, solid Go execute button (#1F497D) with `↵` shortcut badge

### Top Header Layout Redesign

- **Height**: 48px → 38px (high-density ERP standard)
- **Go button**: Solid `#1F497D` with "Go" text + `↵` Enter shortcut badge
- **Alignment**: Search bar, Go button, zoom widget, and utility icons in a single flex row
- **Vertical rhythm**: Standardized across top bar → page title → action bar → data grid

---

## New in v2.0.0

### ML.NET Predictive & Computer-Vision QC Engine

| Component | Description |
|-----------|-------------|
| **QualityVisionInspectionEngine** | ML.NET image classification for surface/grain defect detection. Auto-flags non-conformance lots. |
| **NaturalLanguageQueryEngine** | Parse plain-English queries ("Show overdue invoices for Client 1000") into parameterized EF Core LINQ with multi-tenant filters. |

**Vision Inspection Pipeline:**
```
Image Input → Feature Extraction → Defect Classification → Severity Assessment → NCR Auto-Creation
                                      │
                                      ├── SurfaceScratch, Dent, Discoloration
                                      ├── Crack, ForeignParticle, GrainDefect
                                      ├── MoistureDamage, LabelMisalignment, SealFailure
                                      └── Confidence Score → Pass/Warning/Minor/Major/Critical
```

**NL Query Engine Features:**
- Entity detection: materials, vendors, customers, POs, SOs, inspection lots, stock, journals
- Operations: Select, Count, Sum, Average, Filter, GroupBy, Sort
- Filters: client number, status, plant, material, date ranges, overdue, today/week/month
- Auto-suggestions for common queries

### Mobile App Bridge & Offline Sync Engine

| Component | Description |
|-----------|-------------|
| **OfflineQueueService** | Process batch transactions queued during offline warehouse/field operations. Conflict resolution (Server/Client/Merge/Manual), idempotency validation, atomic stock balance updates. |
| **YuktiraNotificationHub** | SignalR hub for real-time mobile push: low stock alerts, production holds, quality non-conformance. |
| **MobileNotificationService** | Push notification dispatch with priority routing by tenant/material/plant. |

**Offline Transaction Types:** GR, GI, Stock Transfer, Physical Count, Inspection Result, Usage Decision

**Conflict Resolution Strategies:**
- **ServerWins** — Server state takes precedence
- **ClientWins** — Client state overwrites server
- **Merge** — Field-level merge with timestamp priority
- **ManualReview** — Flag for human resolution

### EDI B2B Transport Protocol Engine

| Component | Description |
|-----------|-------------|
| **EdiAs2Handler** | AS2/AS4 message processing with S/MIME encryption, digital signatures, MIC validation, async MDN receipts. |
| **EdiTransactionProcessor** | EDI 850 (Purchase Orders) and EDI 810 (Invoices) translation to ERP entities. |

**Supported Protocols:** AS2, AS4, FTP, HTTPS
**Security Levels:** None, Sign, Encrypt, SignAndEncrypt
**Message Types:** EDI 850, 855, 810, 856, 997

### CQRS & Event Sourcing Engine (v2.0 Architectural Parity)

| Component | Description |
|-----------|-------------|
| **IEventStoreService** | Append domain events, retrieve by aggregate/type, replay with projections. Optimistic concurrency on version. |
| **IEventProjectionService** | Project stock balances and material documents from event streams. Read model snapshots. |
| **Event Store Table** | `yuktira_sys.domain_events` — AggregateId, AggregateType, EventType, EventData (JSONB), Version, Timestamp, TenantId |

**Event Sourcing Flow:**
```
Domain Action → AppendEventAsync → Event Store (PostgreSQL JSONB)
                                       │
                                       ├── Version = latest + 1 (optimistic concurrency)
                                       ├── ReplayEventsAsync → Read Model Projections
                                       └── ReadModelSnapshots → MB51/MB52/MB5B reporting
```

**Supported Aggregates:** Material, StockBalance, PurchaseOrder, SalesOrder, GoodsReceipt, GoodsIssue, InspectionLot, UsageDecision, Vendor, Customer, JournalEntry, ProductionOrder

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
| ZQM | `/api/zqm/*` | Auto Lot Gen, Results Workbench, UD Engine, HU, Lab Calc, COA, NCR, QA Worklist, Pipeline |
| FI | `/api/fi/*` | GL, AP, AR, Tax, Currency, Bank, FB03 |
| CO | `/api/co` | Cost Center, Profit Center, Internal Order |
| PM | `/api/pm` | Equipment, Maintenance Order, Plan |
| HR | `/api/hr` | Employee, Payroll, Attendance |
| WM | `/api/wm` | Transfer, Storage Location, RF |
| ML Vision | `/api/v2/vision/*` | Image Inspection, Defect Detection, Model Training |
| NL Query | `/api/v2/nl-query/*` | Plain-English Query Execution |
| Offline Sync | `/api/v2/offline/*` | Queue Processing, Conflict Resolution |
| Event Store | `/api/v2/events/*` | CQRS Event Append, Replay, Projections |
| Mobile Push | `/api/v2/notifications/*` | Real-time Push Alerts (Stock, Production, Quality) |
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

# ZQM Suite (30+ endpoints)
POST /api/zqm/auto-lot/generate                   # Auto-create inspection lots
GET  /api/zqm/results-workbench                   # Results recording ALV
POST /api/zqm/usage-decisions/score               # Auto-score UD
POST /api/zqm/usage-decisions/reverse             # UD reversal engine
POST /api/zqm/handling-units                      # HU create/split/merge
GET  /api/zqm/qa-worklist                         # Priority worklist
POST /api/zqm/non-conformances                    # NCR lifecycle
POST /api/zqm/lab-calculator/cpk                  # Cp/Cpk calculation
POST /api/zqm/coa/generate                        # COA generation
POST /api/zqm/pipeline-diagnostic                 # QM flow validation

# V2.0: ML Vision Inspection
POST /api/v2/vision/inspect                       # Single image defect detection
POST /api/v2/vision/inspect/batch                 # Batch image inspection
POST /api/v2/vision/model/train                   # Train ML.NET model
POST /api/v2/vision/model/evaluate                # Evaluate model accuracy

# V2.0: Natural Language Query
POST /api/v2/nl-query/execute                     # Execute plain-English query
GET  /api/v2/nl-query/suggestions                 # Query auto-suggestions

# V2.0: Offline Sync
POST /api/v2/offline/process                      # Process offline queue
POST /api/v2/offline/resolve-conflict             # Resolve sync conflicts
GET  /api/v2/offline/pending                      # Get pending transactions

# V2.0: Event Sourcing
POST /api/v2/events/append                        # Append domain event
GET  /api/v2/events/{aggregateId}                 # Get events for aggregate
GET  /api/v2/events/type/{eventType}              # Get events by type
POST /api/v2/events/replay                        # Replay & project events
GET  /api/v2/events/count                         # Get event count

# V2.0: Mobile Push Notifications
POST /api/v2/notifications/low-stock              # Low stock alert
POST /api/v2/notifications/production-hold        # Production hold alert
POST /api/v2/notifications/quality-ncr            # Quality NCR alert
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
| Mobile Push | `/hubs/mobile` | `ReceiveAlert` (low stock, production hold, quality NCR) |
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
| QM | `/Transactions/Engine/ZQM01` | ZQM01 | Auto Inspection Lot Generator |
| QM | `/Transactions/Engine/ZQM02` | ZQM02 | Results Recording Workbench |
| QM | `/Transactions/Engine/ZQM03` | ZQM03 | Usage Decision Engine |
| QM | `/Transactions/Engine/ZQM04` | ZQM04 | UD Reversal Engine |
| QM | `/Transactions/Engine/ZQM05` | ZQM05 | Handling Unit Management |
| QM | `/Transactions/Engine/ZQM06` | ZQM06 | QA Worklist Dashboard |
| QM | `/Transactions/Engine/ZQM07` | ZQM07 | Non-Conformance Manager |
| QM | `/Transactions/Engine/ZQM08` | ZQM08 | Lab Calculator |
| QM | `/Transactions/Engine/ZQM09` | ZQM09 | COA Generator |
| QM | `/Transactions/Engine/ZQM10` | ZQM10 | QM Pipeline Diagnostic |
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
- ZQM suite (auto lot, results workbench, UD engine, HU, lab calc, COA, NCR, QA worklist, pipeline diagnostic)
- Currency formatting middleware
- ML.NET vision inspection engine, NL query engine
- Offline sync queue, conflict resolution, mobile notifications
- EDI AS2/AS4 transport, S/MIME, MDN receipts
- CQRS event store, projections, read model snapshots

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
| ML Vision QC | ✅ | ✅ | Limited | ❌ |
| NL Query Engine | ✅ | ❌ | ❌ | ❌ |
| Offline Sync | ✅ | ✅ | ✅ | ✅ |
| EDI B2B (AS2/AS4) | ✅ | ✅ | ✅ | ✅ |
| CQRS Event Sourcing | ✅ | ✅ | ✅ | ❌ |
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

**v2.0.0** · Built with ❤️ to democratize enterprise ERP

[GitHub](https://github.com/bhnvboy-cell/yukthira)

</div>
