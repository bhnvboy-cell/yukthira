# YuktiraERP — Feature & Capability Reference

> **Version:** 2.0 | **Platform:** .NET 10, C# 13, ASP.NET Core, PostgreSQL 18, SignalR, EF Core  
> **Architecture:** Multi-tenant, modular, event-driven ERP suite with SAP S/4HANA parity

---

## Executive Summary

| Metric | Value |
|--------|-------|
| **Business Modules** | 27 |
| **Razor Pages** | ~344 |
| **API Controllers** | ~42 (+ module sub-controllers) |
| **Entity Classes** | ~170 |
| **DbSet Properties** | 297+ |
| **Service Implementations** | 120+ |
| **Test Cases** | 275 (all passing) |
| **Localization Languages** | 6 (EN, HI, TA, TE, FR, ES) |
| **Themes** | 4 (Modern, Classic, Premium, Dark) |
| **External Connectors** | 5 (SAP S/4HANA, SAP HANA, Oracle ERP, MES, LIMS) |

---

## 1. Operations Modules

### 1.1 MM — Materials Management
| Feature | Description |
|---------|-------------|
| Material Master | Full CRUD with display/edit forms |
| Vendor Master | Vendor CRUD with contact management |
| Purchase Requisitions | Create, edit, list, convert to PO |
| Purchase Orders | Create, edit, display with line items |
| Goods Receipt (GRN) | Create, edit, list with 3-way match |
| Invoice Verification | Create, edit, list — match PO to GRN |
| MIGO Transaction | Goods movement transaction engine (SAP MIGO equivalent) |
| Stock Overview | MMBE-style hierarchical stock display |
| Batch Management | CRUD, traceability, recall management |
| Serial Number Tracking | Create, display, list serial numbers |
| ATP Check | Available-to-Promise stock reservation |
| Movement Type Engine | Validation, simulation, posting, reversal, tracing |
| Document Flow | End-to-end procurement document tracking |

**API:** 8 controllers — Material, StockOverview, MovementType, Batch, Inventory, GRN, Vendor, InvoiceVerification

### 1.2 SD — Sales & Distribution
| Feature | Description |
|---------|-------------|
| Customer Master | CRUD with list/display/edit |
| Sales Inquiries | Create, edit, list, display |
| Quotations | Create, edit, list, display with pricing |
| Sales Orders | Create, edit, list with ATP stock check |
| Deliveries | Create, edit, list with automatic stock deduction |
| Billing Documents | Create, edit, list — invoice generation |
| Full Sales Cycle | Inquiry → Quotation → Sales Order → Delivery → Billing |

**API:** 5 controllers — Customer, Inquiry, Quotation, SO, Delivery, Billing

### 1.3 WM — Warehouse Management
| Feature | Description |
|---------|-------------|
| Storage Locations | Create, display, edit |
| Bin Locations | Create, display, edit, list |
| Warehouse Transfers | Create, display, edit with cross-plant support |
| Transfer Orders | Pick/pack order creation |
| Wave Pick | Batch picking for multiple orders |
| Warehouse Movements | Inbound/outbound/transfer movements |
| Inventory Count | Cycle counting and wall-to-wall |

**API:** WarehouseController — bins, transfers, storage locations CRUD

### 1.4 PP — Production Planning
| Feature | Description |
|---------|-------------|
| Bill of Materials (BOM) | Create, display, edit |
| Work Centers | Create, display, edit, list |
| Production Routing | Create, display, edit, list with operations |
| Production Plans | Create, display, edit, list |
| Production Orders | Full lifecycle: Create → Release → Start → Confirm → Complete → TECO |
| MRP Runs | Single/multi-plant MRP, shortage detection, exception messages |
| Capacity Leveling | Finite capacity scheduling |
| Material Staging | Pre-production material preparation |
| Yield/Scrap Confirmation | Production confirmation with quantity tracking |

**API:** 2 controllers — Production (orders, BOM, routing, work centers, lifecycle), MRP (runs, shortages, exceptions, capacity)

### 1.5 QM — Quality Management
| Feature | Description |
|---------|-------------|
| Inspection Plans | Create, display, edit with characteristics |
| Inspection Lots | Create, display, edit, select/filter (QA32) |
| Inspection Results | Record, display, edit, list with pass/fail |
| Usage Decisions | Create, display, edit, list, reversal |
| Quality Notifications | Create, display, edit with defect coding |
| Certificate of Analysis (COA) | Create, display, edit |
| **ZQM Suite (10 sub-modules):** | |
| — Auto Lot Generator | Automatic inspection lot creation |
| — Results Workbench | SAP QE51N-style results recording |
| — Usage Decision Engine | Automated UD processing |
| — UD Reversal | Reverse usage decisions |
| — Handling Unit | Pack/handle inspection units |
| — QA Worklist | Central QA task management |
| — Non-Conformance / CAPA | Defect tracking with corrective actions |
| — Lab Calculator | Statistical calculations |
| — COA Generator | Automated certificate generation |
| — Pipeline Diagnostic | End-to-end QM trace and validation |

**API:** 2 controllers — Quality (inspection lots, plans, results, UD), ZqmSuite (all 10 sub-modules)

### 1.6 PM — Plant Maintenance
| Feature | Description |
|---------|-------------|
| Equipment Master | Create, display equipment records |
| Maintenance Plans | Create, display, edit, list with scheduling |
| Maintenance Orders | Create, display, edit, list |
| Maintenance Notifications | Defect/failure reporting |
| Maintenance Locations | Functional location tracking |
| Spare Parts | Spare part management |

**API:** PMController — equipment, plans, orders CRUD

### 1.7 MB — Material Documents (SAP TCode Equivalents)
| Feature | Description |
|---------|-------------|
| MB01 — Goods Receipt | Post goods receipt |
| MB01 — Goods Issue | Post goods issue |
| Other Goods Receipt | Receipt without PO reference |
| Transfer Posting | Plant-to-plant transfer |
| MB21 — Reservation | Create reservations |
| MB22 — Display Reservation | View/edit reservations |
| MB03 — Material Document | Display material document |
| MB51 — Document List | Material document list report |
| MB52 — Stock Overview | Warehouse stock report |
| MB5B — Historical Stock | Historical stock balances |
| MB5L — Reconciliation | Stock GL reconciliation |

### 1.8 LIMS — Laboratory Information Management
| Feature | Description |
|---------|-------------|
| Sample Management | Create, display, edit samples |
| Test Results | Record, display, edit, list results |
| Specifications | Create, display, edit, list with limits |
| Instruments | Create, display, edit, list instrument tracking |

**API:** LIMSController — samples, test results, specifications, instruments CRUD

### 1.9 PS — Project System
| Feature | Description |
|---------|-------------|
| Projects | Create, display project records |
| Project Tasks | Create, display, edit, list with breakdown |
| Timesheets | Create, display, edit, list time tracking |

**API:** PSController — projects, tasks, timesheets CRUD

### 1.10 CR — Customer Complaints & Returns
| Feature | Description |
|---------|-------------|
| Complaint/Return | Full complaint lifecycle with delivery, quality inspection, financial posting |
| Supplier Claims | Supplier-side claim tracking |
| Supplier Return | Return-to-vendor processing |
| Workflow Steps | Approval routing for complaints |

---

## 2. Finance Modules

### 2.1 FI — Finance
| Feature | Description |
|---------|-------------|
| Chart of Accounts | GL account management |
| General Ledger | GL display with line items |
| Journal Entry Posting | Create, display, edit, list with double-entry |
| Trial Balance | Period trial balance report |
| Profit & Loss | P&L statement report |
| Balance Sheet | Balance sheet report |
| Accounts Payable (AP) | AP invoice creation, payment posting, aging |
| Accounts Receivable (AR) | AR invoice creation, payment posting, aging |
| Fixed Assets | Acquisition, depreciation, disposal, transfer |
| Fiscal Period | Open/close periods |
| Bank Reconciliation | Auto-match, OFX/MT940/CSV import |
| Tax Engine | Tax codes, calculation, invoice posting, transactions |
| Multi-Currency | Exchange rates, conversion, revaluation |

**API:** 4 controllers — Finance (accounts, ledger, AP/AR, assets, bank), Tax, Currency, Bank

### 2.2 CO — Controlling
| Feature | Description |
|---------|-------------|
| Cost Centers | Create, display, edit with reports |
| Cost Elements | Create, display, edit, list |
| Profit Centers | Create, display, edit, list |
| Internal Orders | Create, display, edit, list |
| Cost Allocation | Rules, execution, utilization analysis |

**API:** COController — cost centers, elements, profit centers, orders, allocation rules/engine

---

## 3. People Modules

### 3.1 HR — Human Resources
| Feature | Description |
|---------|-------------|
| Employee Master | Create, display, edit, list |
| Organizational Units | Org structure management |
| Attendance | Create, display, edit, list with reports |
| Leave Management | Create, display, edit, list |
| Payroll | Create, display, edit, list with calculation engine |
| Payroll Runs | Execute payroll with history |
| Performance Appraisals | Create, display, edit, list |
| Time Entry | Work time recording |
| Recruitment | Recruitment tracking |

**API:** HRController — employees, leave, payroll, attendance, appraisals, payroll calculation

### 3.2 CRM — Customer Relationship Management
| Feature | Description |
|---------|-------------|
| Leads | Create, display, edit with scoring |
| Opportunities | Create, display, edit with pipeline stage |
| Contacts | Create, display, edit, list |
| Accounts | Account management |
| Sales Pipeline | Pipeline creation with reporting |
| Campaigns | Create, display, edit, list |
| Service Tickets | Create, display, edit, list |

**API:** CRMController — leads, opportunities, contacts, campaigns, tickets CRUD

---

## 4. Analytics & Intelligence Modules

### 4.1 BI — Business Intelligence
| Feature | Description |
|---------|-------------|
| Ad-Hoc Reports | Create and execute raw SQL reports |
| Dashboards | Create and display KPI dashboards |
| KPI Monitoring | KPI catalog and calculation |
| Chart Rendering | Visualization of report results |

**API:** BIController — reports, dashboards, KPIs CRUD + execution

### 4.2 AI — AI & Vision
| Feature | Description |
|---------|-------------|
| AI Forecasting | Moving Average, Exponential Smoothing, Holt-Winters, ARIMA |
| Demand Prediction | Statistical demand forecasting |
| Safety Stock | Auto-calculation of safety stock levels |
| Anomaly Detection | Z-score based anomaly identification |
| Vision Inspection | Single-image quality inspection with defect classification |
| Batch Inspection | Multi-image batch processing with pass/fail summary |
| Model Training | Train vision models with labeled images |
| Model Evaluation | Accuracy, Precision, Recall, F1 scoring |
| Defect Classification | 9 defect types: scratch, dent, discoloration, crack, foreign particle, grain defect, moisture, label, seal |
| Natural Language Query | NL-to-SQL query translation with suggestions |
| Non-Conformance Auto-Creation | Auto-generate NCR from critical defects |

**API:** 3 controllers — AIEngine (forecast, anomaly), VisionInspection (inspect, batch, train, evaluate), NLQuery (execute, suggest)

### 4.3 KPI — Key Performance Indicators
| Feature | Description |
|---------|-------------|
| KPI Dashboard | Real-time KPI visualization |
| Cross-Module KPIs | Metrics from MM, SD, PP, QM, FI modules |

---

## 5. System & Infrastructure Modules

### 5.1 Admin — Administration
| Feature | Description |
|---------|-------------|
| User Management | CRUD, activate/deactivate, unlock, password reset |
| Tenant Management | Multi-tenant CRUD |
| System Configuration | Key-value configuration store |
| Plugin Management | Plugin install/uninstall UI |

**API:** 3 controllers — AdminUser, AdminTenant, AdminSystemConfig

### 5.2 Authentication & Security
| Feature | Description |
|---------|-------------|
| JWT Authentication | Token-based auth with 8-hour expiry |
| Refresh Tokens | Token rotation with device/IP tracking |
| Cookie Auth | Server-side session with sliding expiration |
| MFA/TOTP | Two-factor authentication (feature-flagged) |
| Password Policy | Configurable complexity, lockout after failed attempts |
| Account Lockout | Auto-lock with configurable timeout, auto-unlock on startup |
| Permission Matrix | Role-based module access control |
| Suspicious Activity | Detection and flagging of anomalous behavior |
| Compliance Audit | Full audit trail with CSV export |
| User Impersonation | Super-user impersonation for support |
| AES Encryption | Data encryption service with configurable key |

**API:** Auth, Security, SuperUser controllers

### 5.3 Transaction Code Engine (SAP TCode System)
| Feature | Description |
|---------|-------------|
| T-Code Registry | SAP-style transaction code management |
| T-Code Execution | Dynamic form rendering per T-code |
| Layout Registry | Custom layouts per T-code |
| Favorites & Recent | Personalized transaction shortcuts |
| Role-Based Access | Per-T-code authorization |
| T-Code Generator | Custom T-code creation with field designer |
| Workflow Integration | Advance/reject workflow per T-code |
| Custom Fields | Dynamic custom fields per T-code |
| Layout Customization | User-level column visibility/order |

**API:** TransactionController, TCodeEngineController, TCodeGeneratorController

### 5.4 Workflow Engine
| Feature | Description |
|---------|-------------|
| Workflow Designer | Visual chain designer with nodes and edges |
| Step Validation | Pre-execution step validation |
| Step Execution | Forward/reject routing |
| Progress Tracking | Instance-level progress monitoring |
| Instance Monitoring | Active workflow instance list |

**API:** WorkflowController — chains, steps, instances, progress

### 5.5 Approval Workflow
| Feature | Description |
|---------|-------------|
| Pending Approvals | Role-based approval queue |
| Approve/Reject | Approval actions with comments |
| Escalation | Escalation to higher authority |
| Approval History | Full approval audit trail |

**API:** ApprovalsController — pending, approve, reject, escalate

### 5.6 Notifications
| Feature | Description |
|---------|-------------|
| In-App Inbox | Notification center with read/unread |
| Unread Count | Real-time badge counter |
| Mark as Read | Individual and bulk mark-read |
| Email Delivery | SMTP-based email sending |
| SMS Delivery | Twilio-based SMS sending |
| Delivery Logs | Multi-channel delivery tracking |

**API:** NotificationsController, NotificationDeliveryController

### 5.7 Audit
| Feature | Description |
|---------|-------------|
| Audit Trail | Create/update/delete/api call logging |
| Module Filtering | Filter by module, user, date, action type |
| Suspicious Activity | Flag and review suspicious events |
| Log Count | Real-time log metrics |

**API:** AuditController — logs, count, flag

### 5.8 Integration Hub
| Feature | Description |
|---------|-------------|
| Connector Registry | Pluggable connector system (SAP, Oracle, MES, LIMS) |
| Connections | CRUD with test connection and execute |
| Webhooks | CRUD with event types and delivery logs |
| Data Mapping | CRUD mapping rules for data transformation |
| API Clients | CRUD with IP whitelisting |
| Sync Jobs | CRUD with run and log tracking |
| Integration Queue | Outbound queue with dead-letter support |
| Background Processing | 30-second queue processor |

**API:** IntegrationController — connectors, webhooks, mappings, clients, sync jobs, queue

### 5.9 EDI & B2B
| Feature | Description |
|---------|-------------|
| Trading Partners | CRUD with standard, version, auth config |
| EDIFACT Conversion | PO (850), Invoice (810) to EDIFACT |
| X12 Conversion | PO (850), Invoice (810) to X12 |
| EDIFACT/X12 Parsing | Inbound message parsing |
| AS2 Transport | S/MIME sign/encrypt/decrypt, MDN generation |
| Acknowledgment Tracking | 997/MDN logs with filtering and pagination |

**API:** EdiController — partners, convert, parse, acknowledge; IntegrationController (EDI endpoints)

### 5.10 Plugins
| Feature | Description |
|---------|-------------|
| Plugin Registry | Discovery and registration |
| Install/Uninstall | Plugin lifecycle management |
| Per-Tenant Enable | Tenant-level plugin activation |
| Plugin Settings | Settings CRUD per plugin |
| Hot Reload | Runtime plugin reload |
| Sandboxed Execution | Memory/time limits per plugin |

**API:** PluginsController — all plugin operations

### 5.11 Customization
| Feature | Description |
|---------|-------------|
| Screen Layout | User-level layout customization |
| Column Management | Add/remove/reorder columns |
| Per-User Storage | Layout persistence per user/tenant |

**API:** CustomizationController — layouts, columns

### 5.12 Dashboard
| Feature | Description |
|---------|-------------|
| Widget Dashboard | Configurable module tile dashboard |
| Widget Layout | Drag-and-drop layout persistence |
| Quick Actions | Shortcut links to common transactions |

**API:** DashboardController — widgets, layout

### 5.13 Export Engine
| Feature | Description |
|---------|-------------|
| Multi-Format Export | CSV, XLSX, PDF, HTML |
| Template Documents | PDF generation from templates |

**API:** ExportController — grid export, document generation

### 5.14 Localization
| Feature | Description |
|---------|-------------|
| Multi-Language | 6 supported languages |
| Per-Tenant Translations | Tenant-specific translation overrides |
| Translation CRUD | Create, update, delete translations |

**API:** LanguageController — languages, translations

### 5.15 Number Ranges
| Feature | Description |
|---------|-------------|
| Auto-Incrementing | Per module/prefix per tenant |
| Document Numbering | PO, SO, GRN, etc. |

**API:** NumberRangesController — get next, reset

---

## 6. Cross-Cutting Capabilities

### 6.1 Multi-Tenancy
- Subdomain-based tenant resolution
- Row-level tenant isolation via `TenantSaveChangesInterceptor`
- Per-tenant configuration, features, and module enablement
- Per-tenant localization and theme

### 6.2 Real-Time Communication (SignalR)
| Hub | Purpose |
|-----|---------|
| `/hubs/notifications` | In-app notification push |
| `/hubs/mobile` | Mobile push: stock alerts, production holds, quality NCRs |
| Dashboard Hub | Real-time KPI push (stock, production, quality, anomaly) |

### 6.3 Background Services
| Service | Interval | Purpose |
|---------|----------|---------|
| MessageBusConsumerService | Continuous | Event consumption from in-memory bus |
| IntegrationQueueBackgroundService | 30 seconds | Outbound integration processing |
| MrpSchedulerBackgroundService | 24 hours | Daily MRP shortage detection |

### 6.4 Security
| Feature | Detail |
|---------|--------|
| Security Headers | X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy |
| CSRF Protection | Anti-forgery tokens on all forms |
| Session Management | SAP-style timeout warning with configurable idle timeout |
| SOX Compliance | Segregation of Duties, violation detection, immutable audit trails |
| Tenant Isolation | Row-level security via EF interceptor |

### 6.5 UI/UX
| Feature | Detail |
|---------|--------|
| 4 Theme Options | Modern, Classic, Premium, Dark |
| Screen Zoom | 80%–120% density control (Compact/Dense/Standard/Large/ExtraLarge) |
| Global Search | Ctrl+K search with debounced results |
| Responsive Layout | Mobile-first with sidebar drawer |
| Accessibility | Skip-to-content, ARIA labels, focus-visible |
| Print Support | Ctrl+P print styles |
| Session Warning | SAP-style countdown modal with continue/logoff |

### 6.6 External Connectors
| Connector | Status |
|-----------|--------|
| SAP S/4HANA | Registered (disabled by default) |
| SAP HANA | Registered (disabled by default) |
| Oracle ERP | Registered (disabled by default) |
| MES | Registered (disabled by default) |
| LIMS | Registered (disabled by default) |

---

## 7. Technology Stack

| Layer | Technology |
|-------|-----------|
| **Runtime** | .NET 10, C# 13 |
| **Web** | ASP.NET Core Razor Pages + Controllers |
| **Database** | PostgreSQL 18 (InMemory fallback for tests) |
| **ORM** | Entity Framework Core |
| **Auth** | JWT Bearer + Cookie Authentication |
| **Real-Time** | SignalR |
| **Logging** | Serilog (console + file rolling) |
| **Caching** | MemoryCache + Redis (graceful fallback) |
| **Export** | ClosedXML (XLSX), iTextSharp (PDF) |
| **Frontend** | Bootstrap 5.3, jQuery 3.7, Bootstrap Icons |
| **Search** | Ctrl+K global search |
| **Testing** | xUnit, 275 test cases |
| **CI/CD** | GitHub Actions compatible |

---

## 8. Project Structure

```
YuktiraERP/
├── src/
│   ├── YuktiraERP.Core/              — Domain models, interfaces, DTOs, enums
│   ├── YuktiraERP.Infrastructure/    — EF Core, services, migrations, security
│   ├── YuktiraERP.Web/              — Razor Pages UI + middleware
│   ├── YuktiraERP.Api/              — REST API controllers
│   ├── YuktiraERP.AIEngine/         — AI/ML engine services
│   ├── YuktiraERP.WorkflowEngine/   — BPMN workflow runtime
│   ├── YuktiraERP.PluginSdk/        — Plugin system SDK
│   ├── YuktiraERP.ExportEngine/     — Multi-format export engine
│   └── YuktiraERP.Tests/            — 275 xUnit test cases
├── database/
│   └── scripts/                     — Seed scripts
└── feature.md                       — This document
```
