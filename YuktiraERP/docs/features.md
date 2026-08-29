# YuktiraERP — Features & Capabilities

> **Version 1.0.8** | August 2026

---

## What is YuktiraERP?

YuktiraERP is a **complete, open-source Enterprise Resource Planning (ERP) system** built for modern businesses. It covers everything from managing raw materials to shipping finished products, from tracking employee payroll to analyzing financial reports — all in one platform.

**Name Origin:** *Yuktira* (Sanskrit: युक्ति) means "logic, strategy" — reflecting the intelligent, data-driven approach of this platform.

---

## Quick Overview

| Aspect | Details |
|--------|---------|
| **Total Modules** | 35 modules across 7 categories |
| **Transaction Codes** | 75 SAP-style quick-access codes |
| **UI Themes** | 4 themes (Modern, Classical, Minimal, Futuristic) + Dark Mode |
| **Languages** | 7 languages (English, Hindi, Tamil, Telugu, French, Spanish, Kannada, Malayalam) |
| **Test Coverage** | 261/261 tests passing |
| **Database** | PostgreSQL with 27 migration scripts |
| **API** | RESTful API with GraphQL support |
| **Real-time** | SignalR notifications and live updates |

---

## Core Features

### 1. Authentication & Security
- **JWT Authentication** — Secure token-based login
- **Multi-Factor Authentication (MFA)** — TOTP-based 2FA (Google Authenticator compatible)
- **Role-Based Access Control (RBAC)** — Admin, PowerUser, User roles
- **Account Lockout** — Auto-lock after 5 failed attempts (15-minute timeout)
- **Password Policy** — Configurable complexity requirements
- **Session Timeout** — SAP-style warning modal with countdown
- **Audit Trail** — Every action logged with user, timestamp, IP address
- **Suspicious Activity Detection** — Flags unusual login patterns

### 2. Multi-Tenancy
- **Tenant Isolation** — Each client has its own data space
- **Tenant Settings** — Per-tenant configuration (theme, module access)
- **Auto-Stamping** — Every record automatically tagged with tenant ID
- **Module Toggle** — Admins can enable/disable modules per tenant

### 3. Dashboard & Analytics
- **Real-Time Dashboard** — Live KPI cards, activity feed, module tiles
- **KPI Formula Engine** — 5 predefined KPIs (Revenue, Orders, Stock, Production, Quality)
- **Pinned Modules** — Quick access to frequently used modules
- **Recently Used** — Auto-tracked module history
- **BI Reports** — Create custom reports with charts (bar, line, pie, scatter)
- **Widget Layout Editor** — Drag-and-drop dashboard customization

---

## Module Features

### Operations Modules

#### Materials Management (MM)
- **Material Master** — 5 material types (ROH, FERT, HALB, VERP, HIBE), 20+ UOM options
- **Purchase Orders** — Multi-line POs with auto-calculated totals (Qty × Price − Discount)
- **Purchase Requisitions** — Internal purchase requests with approval workflow
- **Goods Receipt (MIGO)** — Receive goods against PO, validate quantities
- **Invoice Verification** — Three-way match (PO / GRN / Invoice)
- **Vendor Master** — Full vendor profiles with purchasing org, payment terms
- **Stock Overview** — Real-time stock levels, movements, valuation
- **Stock Movements** — GR, GI, Transfer, Reversal with before/after quantities

#### Sales & Distribution (SD)
- **Sales Orders** — 3-tab form (Sold-To, Items, Organization) with auto-calc
- **Customer Master** — Full customer profiles with credit limits
- **Outbound Delivery** — Pick, pack, and ship
- **Billing** — Invoice generation with tax calculations
- **Customer Complaints** — Return orders, credit memos, complaint tracking
- **Order-to-Cash** — Complete cycle from order to payment collection

#### Warehouse Management (WM)
- **Bin Management** — Storage types, sections, capacity (weight/volume)
- **Transfer Orders** — Move stock between bins
- **Wave Picking** — Group multiple orders into optimized pick waves
- **RF Scanning** — Handheld device operations for picking/putting
- **Inventory Counting** — Cycle counting with difference posting
- **Velocity Slotting** — ABC analysis for dynamic bin assignment
- **Putaway Strategies** — FIFO, LIFO, FEFO (First Expired, First Out)
- **Batch & Serial Tracking** — Per-bin batch and serial number management

#### Production Planning (PP)
- **Bill of Materials (BOM)** — Multi-level product structure with alternatives
- **Work Centers** — Machines, labor, capacity with shift definitions
- **Production Routings** — Operation sequences with standard times
- **Production Orders** — Full lifecycle: Create → Schedule → Release → Confirm → Settle
- **Order Confirmation (CO11N)** — Report actual quantities, scrap, activity times
- **MRP Engine** — Material Requirements Planning with BOM explosion
- **PP/DS Scheduling** — Finite capacity scheduling
- **Planned Independent Requirements** — Demand planning

#### Quality Management (QM)
- **Inspection Plans** — Define characteristics, tolerances, sampling procedures
- **Inspection Lots** — Types 01–09 (Procurement, Production, etc.)
- **Inspection Results** — Record measurements with 15 unit types
- **Usage Decisions** — Accept / Reject / Rework / Scrap with stock posting
- **Quality Notifications** — Defect tracking, root cause analysis, CAPA
- **Certificates of Analysis (COA)** — Generate quality certificates for customers
- **Calibration Management** — Equipment calibration schedules

#### Plant Maintenance (PM)
- **Equipment Master** — Categories (Machine, Plant, IT), serial numbers, manufacturers
- **Functional Locations** — Organizational structure for assets
- **Maintenance Orders** — Types: PM01 (Corrective), PM02 (Preventive), PM03 (Emergency), PM04 (Refurbishment)
- **Maintenance Plans** — Preventive maintenance scheduling (daily/weekly/monthly)
- **Spare Parts** — Spare part inventory management
- **Maintenance Notifications** — Report breakdowns and issues
- **Order Confirmation** — Track actual vs planned hours

### Finance Modules

#### Finance (FI)
- **General Ledger** — Journal entries, trial balance, P&L, balance sheet
- **Accounts Payable** — Vendor invoices, outgoing payments, aging reports
- **Accounts Receivable** — Customer invoices, incoming payments, dunning
- **Fixed Assets** — Depreciation (SLM, WDV, DDB, SYD, UOP), disposal, transfer
- **Bank Reconciliation** — Statement vs ledger matching
- **Tax Engine** — GST (0/5/12/18/28), VAT, TDS with auto-calculation
- **Multi-Currency** — Exchange rates, conversion, revaluation (USD, EUR, INR, GBP)
- **Fiscal Periods** — Period open/close controls

#### Controlling (CO)
- **Cost Centers** — Create, budget, allocate
- **Cost Allocation** — Proportional split by headcount, area, etc.
- **Order Settlement** — Settle PM/PP orders to CO objects

#### Universal Journal (UJ)
- **Single-Entry Bookkeeping** — Unified view of all financial postings

#### Tax Management (TX)
- **Tax Codes** — Pre-seeded GST, VAT, TDS codes
- **Tax Returns** — Filing and compliance

#### Consolidation (CN)
- **Multi-Entity** — Consolidate financials across entities

### People Modules

#### Human Resources (HR)
- **Employee Master** — Personal details, org assignment, salary
- **Payroll** — PF, ESI, PT, TDS calculation, payslip generation
- **Time Evaluation** — Attendance and leave tracking

#### Customer Relationship Management (CRM)
- **Lead Management** — Capture and qualify leads
- **Opportunity Tracking** — Sales pipeline stages
- **Activities** — Calls, meetings, tasks

### Projects & Labs

#### Project System (PS)
- **Project Planning** — WBS (Work Breakdown Structure)
- **Project Tasks** — Activity definitions and assignments
- **Timesheet** — Time recording against projects

#### Lab Information Management (LIMS)
- **Test Requests** — Create and track test requests
- **Sample Management** — Registration, storage, tracking
- **Test Results** — Record and review results

### Analytics Modules

#### Business Intelligence (BI)
- **Report Builder** — Drag-and-drop report creation
- **Chart Types** — Bar, line, pie, scatter, table
- **Dashboards** — Real-time data visualization

#### AI Analytics
- **Forecasting** — 9 models (MA, WMA, ES, LR, Seasonal, Holt-Winters, ARIMA)
- **Anomaly Detection** — Identify unusual patterns
- **Document OCR** — Extract data from scanned documents

#### PP/DS Scheduling
- **Finite Scheduling** — Capacity-constrained production scheduling

### Compliance

#### SOX Compliance
- **Audit Trail** — Complete transaction history
- **Segregation of Duties** — Prevent conflicts of interest

### System Modules

#### Workflow Engine
- **BPMN Designer** — Visual workflow builder
- **Node Types** — Start, Task, Approval, Decision, Email, Timer, API Call, End
- **Approval Chains** — Multi-level approvals with escalation
- **Simulation Mode** — Test workflows before activation

#### Integration Hub
- **Webhooks** — Real-time event notifications (HMAC-signed)
- **EDI** — EDIFACT D96A / X12 4010 conversion
- **API Clients** — Third-party authentication with IP whitelisting
- **Message Queue** — Outbound queue with retry and dead-letter handling
- **API Throttling** — 100 requests/min per client IP

#### Transaction Code Engine
- **75 T-Codes** — SAP-style quick navigation
- **Favorites** — Mark frequently used codes
- **Search** — Global search with Ctrl+K
- **AI Generator** — Create custom T-codes with AI assistance

---

## User Interface Features

### Themes
| Theme | Style |
|-------|-------|
| **Modern** | Clean, blue-accented, rounded corners |
| **Classical** | Traditional, serif fonts, muted colors |
| **Minimal** | Flat, whitespace-focused, minimal borders |
| **Futuristic** | Dark gradients, neon accents, bold |
| **Dark Mode** | Toggle via moon icon, auto-detects OS preference |

### Localization (7 Languages)
- English, Hindi (हिन्दी), Tamil (தமிழ்), Telugu (తెలుగు), French (Français), Spanish (Español), Kannada (ಕನ್ನಡ), Malayalam (മലയാളം)
- 75+ translated UI strings
- Cookie-based language switching

### Responsive Design
- Mobile & tablet support with off-canvas sidebar
- Touch-friendly tap targets
- Horizontally scrollable tables on small screens

### Print & Export
- **Print** — Ctrl+P for clean, paper-friendly output
- **Export** — XLSX, CSV, TXT, PDF, HTML
- **9 Document Templates** — PO, SO, Invoice, COA, GRN, Production Order, QC Report, Payslip, Financial Statement

### Real-Time Features
- **SignalR Notifications** — Live in-app notifications
- **Live Dashboard** — Real-time activity feed
- **WebSocket Updates** — Instant data refresh

---

## Developer & Integration Features

### REST API
- **Versioned** — All endpoints under `/api/v1/`
- **Swagger UI** — Interactive API documentation at `/swagger`
- **JWT Auth** — Token-based API authentication
- **GraphQL** — 24 entity queries + dashboard aggregation

### Plugin SDK
- **4 Hook Types** — Extend system behavior
- **Hot Reload** — Update plugins without restart
- **Sandboxing** — Safe plugin execution
- **3 Example Plugins** — AdvancedQC, DairyExtension, ExtraReports

### Database
- **PostgreSQL** — Enterprise-grade RDBMS
- **27 Migration Scripts** — Versioned schema updates
- **Auto-Discovery** — Migrations applied automatically on startup
- **Backup/Restore** — Automated scripts with 30-backup rotation

### Observability
- **Health Checks** — `/health` endpoint with database ping
- **Serilog** — Structured logging to console + daily rolling files
- **Prometheus Metrics** — `/metrics` for Grafana dashboards

---

## What Makes YuktiraERP Special?

| Feature | YuktiraERP | Commercial ERPs |
|---------|-----------|-----------------|
| **Open Source** | ✅ MIT License | ❌ Proprietary |
| **Cost** | Free | $10K–$500K/year |
| **Customization** | Full source code access | Limited to config |
| **Modern UI** | 4 themes, dark mode, responsive | Often outdated |
| **SAP Compatibility** | 75 SAP-style T-codes | Native SAP |
| **AI Integration** | Built-in forecasting, OCR | Expensive add-ons |
| **Multi-Tenancy** | Built-in | Often extra |
| **API First** | REST + GraphQL | Varies |
| **Test Coverage** | 261 tests | Varies |

---

*Yuktira ERP Suite — Intelligence Driven Enterprise Platform*
