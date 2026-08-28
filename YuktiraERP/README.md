# YuktiraERP

Open-source enterprise resource planning system built on .NET 10 + PostgreSQL 16, designed to match and exceed commercial ERP capabilities (SAP S/4HANA, Oracle Fusion, D365).

**Live at:** `http://localhost:5001` (Web) | `http://localhost:5000` (API)  
**Login:** `superadmin` / `yuktira123` / Client `1000`

---

## Architecture

| Layer | Technology |
|-------|-----------|
| Frontend | ASP.NET Razor Pages, Bootstrap 5, jQuery, SignalR |
| API | ASP.NET Core 10 Web API, GraphQL (HotChocolate 15) |
| Business Logic | .NET 10 class libraries (Core, Infrastructure) |
| Database | PostgreSQL 16, Entity Framework Core |
| Real-time | SignalR hubs (notifications, dashboard) |
| AI/ML | Custom AI Engine (OCR, predictive analytics, anomaly detection) |
| Export | Custom Export Engine (CSV, Excel, PDF) |
| Workflow | Custom Workflow Engine (FSM-based, 4 seeded chains) |
| Plugin System | Custom Plugin SDK with runtime loading |

## Modules (28)

| Category | Modules |
|----------|---------|
| **Operations** | MM (Materials), SD (Sales), WM (Warehouse), PP (Production), QM (Quality), PM (Maintenance), CR (Customer Complaints), RF (RF Warehouse), WV (Wave Pick), VS (Velocity Slotting) |
| **Finance** | FI (Finance), CO (Controlling), UJ (Universal Journal), TX (Tax Management), CN (Consolidation) |
| **People** | HR (Human Resources), CRM (Customer Relationship) |
| **Projects & Labs** | PS (Project System), LIMS (Lab Information Management) |
| **Analytics** | BI (Reports), AI (Analytics), PD (PP/DS Scheduling) |
| **Compliance** | SX (SOX Compliance) |
| **System** | WF (Workflows), APP (Approvals), NOT (Notifications), TCD (Transaction Codes), TCG (T-Code Generator), AUD (Audit Log), ADM (Administration), CST (Customize), INT (Integration), PLG (Plugins), ME (MRP Events) |

## Key Features

### ERP Core
- **159 movement types** across Goods Receipt, Goods Issue, Stock Transfer, and Revaluation
- **Batch & serial number** tracking with full traceability
- **Universal Journal** — FI+CO merged into single ledger (SAP ACDOCA equivalent)
- **Multi-entity consolidation** with elimination entries and currency translation
- **Localization tax engine** — country-specific tax configs with withholding tax

### Quality Management
- 11 QM process screens (ZQM1, 1FM, 2F9, 1E1, 2QP, 2QN, QMM, 1MP, BKR, 2FA, CALIB)
- Inspection lots, inspection plans, usage decisions, quality certificates
- Customer complaint & return workflow (8-step cross-functional SD-QM-MM-FI)

### Warehouse & Logistics
- **RF mobile framework** — scanner menu, pick tasks, count tasks
- **Wave pick management** — wave creation, line allocation, pick confirmation
- **Velocity slotting** — ABCD classification with automatic bin assignment
- **Bin master** management with capacity tracking

### Production
- **PP/DS finite scheduling** — capacity-constrained scheduling with load leveling
- **Event-driven MRP** — real-time material requirement triggers
- BOM management, routings, work centers, production order confirmations

### Compliance & Audit
- **SOX compliance** — duty assignments, violation tracking, immutable audit trail (SHA-256 hash chain)
- Immutable audit logs that detect tampering

### AI & Analytics
- **Document OCR** — base64 document processing with confidence scoring
- **Predictive analytics** — demand forecasting with trend/seasonality decomposition
- **Anomaly detection** — z-score based outlier identification
- Live KPI dashboard with SignalR real-time updates

### Workflow Engine
- Finite state machine with 4 seeded workflow chains:
  - **P2P** (Procure-to-Pay): PR → PO → GR → IR → Payment
  - **P2P-PROD** (Production Procurement): PR → PO → GR → IR → Production → Settlement
  - **O2C** (Order-to-Cash): SO → Delivery → Billing → Payment
  - **PM-CYCLE** (Maintenance Cycle): Notification → Order → Execution → Confirmation → Settlement

### TCode Engine
- **5-tier Fiori-style layout** renderer (toolbar, workflow progress, metadata, data grid, action bar)
- **85+ transaction codes** with real API CRUD operations
- Search, favorites, recent usage tracking
- Config-driven — any TCode rendered from JSON layout definitions

### Frontend
- Dynamic sidebar navigation generated from module catalog
- Theme engine (modern, classic, premium, dark)
- 6-language i18n support (English, Hindi, Tamil, Telugu, French, Spanish)
- PWA support with service worker
- Inline cell editing for tables

### API & Integration
- REST API with JWT authentication
- **GraphQL endpoint** at `/api/graphql` (HotChocolate 15)
- **SignalR hubs** for real-time notifications and dashboard updates
- Prometheus metrics at `/metrics`
- Health checks at `/health`
- Swagger/OpenAPI documentation

## GraphQL API

Available at `http://localhost:5000/api/graphql`:

```graphql
# Query all materials with filtering and sorting
query {
  materials(where: { materialName: { contains: "Steel" } }, orderByName: ASC) {
    id materialCode materialName uom price
  }
}

# Get dashboard summary with all KPIs
query {
  dashboard {
    kpis { name value unit trend }
    inventory { totalMaterials totalStockValue lowStockCount }
    sales { totalOrders totalRevenue pendingOrders }
    production { totalOrders inProgressOrders completedOrders }
    quality { totalLots passRate }
    financial { totalDebits totalCredits netBalance }
  }
}

# Real-time stock alerts
query {
  stockAlerts { materialName currentStock minStock maxStock alertLevel }
}
```

## SignalR Hubs

| Hub | URL | Purpose |
|-----|-----|---------|
| Notifications | `/hubs/notifications` | Push notifications, alerts |
| Dashboard | `/hubs/dashboard` | Live KPI updates, stock changes, order updates, quality alerts, SOX violations, anomaly detection |

## Project Structure

```
YuktiraERP/
├── src/
│   ├── YuktiraERP.Core/           # Domain models, interfaces, DTOs
│   ├── YuktiraERP.Infrastructure/ # Services, DB context, seed data
│   ├── YuktiraERP.WorkflowEngine/ # FSM-based workflow execution
│   ├── YuktiraERP.AIEngine/       # OCR, predictive, anomaly detection
│   ├── YuktiraERP.ExportEngine/   # CSV, Excel, PDF generation
│   ├── YuktiraERP.PluginSdk/      # Runtime plugin loading
│   ├── YuktiraERP.Api/            # REST API, GraphQL, SignalR hubs
│   ├── YuktiraERP.Web/            # Razor Pages frontend
│   └── YuktiraERP.Tests/          # 261 unit tests
```

## Quick Start

```bash
# Restore and build
dotnet restore
dotnet build

# Run API (port 5000)
dotnet run --project src/YuktiraERP.Api

# Run Web (port 5001)
dotnet run --project src/YuktiraERP.Web

# Run tests (261 passing)
dotnet test src/YuktiraERP.Tests
```

## Environment

- .NET 10 SDK
- PostgreSQL 16
- Connection string: `Host=localhost;Database=yuktira_erp;Username=postgres;Password=postgres`

## Commercial ERP Comparison

| Feature | YuktiraERP | SAP S/4HANA | Oracle Fusion | D365 |
|---------|:----------:|:-----------:|:-------------:|:----:|
| License Cost | Free | $$$$$ | $$$$$ | $$$$ |
| Source Code | Open | Closed | Closed | Closed |
| Universal Journal | ✅ | ✅ | ✅ | ✅ |
| SOX Compliance | ✅ | ✅ | ✅ | ✅ |
| Wave Pick | ✅ | ✅ | ✅ | ✅ |
| PP/DS Scheduling | ✅ | ✅ | ✅ | ✅ |
| Event-Driven MRP | ✅ | ✅ | ✅ | ✅ |
| Consolidation | ✅ | ✅ | ✅ | ✅ |
| GraphQL API | ✅ | ❌ | ✅ | ✅ |
| Real-time Dashboard | ✅ | ✅ | ✅ | ✅ |
| Custom Workflows | ✅ | ✅ | ✅ | ✅ |
| Plugin System | ✅ | ✅ | ✅ | ✅ |
| i18n | 6 langs | 40+ | 30+ | 40+ |
| Mobile RF | ✅ | ✅ | ✅ | ✅ |
| AI/ML Built-in | ✅ | Limited | ✅ | ✅ |

**Cost savings: 99%+ vs SAP/Oracle, 90%+ vs D365**

## Test Coverage

**261/261 tests passing** across:
- QC, PM, PP, Procurement, Sales, Cross-module scenarios
- Customer complaint & return workflow (12 tests)
- Universal journal, SOX compliance, RF warehouse
- Wave pick, velocity slotting, PP/DS scheduling
- MRP events, consolidation, localization tax
- AI document OCR, predictive analytics
- Edge cases and integration tests

## License

Open Source — Free for commercial and personal use.
