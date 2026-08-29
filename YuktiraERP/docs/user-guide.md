# YuktiraERP — Complete User Guide

> **Version 1.0.8** | August 2026

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Login & Authentication](#2-login--authentication)
3. [Navigating the Interface](#3-navigating-the-interface)
4. [Transaction Codes (T-Codes)](#4-transaction-codes-t-codes)
5. [Modules Overview](#5-modules-overview)
6. [Materials Management (MM)](#6-materials-management-mm)
7. [Sales & Distribution (SD)](#7-sales--distribution-sd)
8. [Production Planning (PP)](#8-production-planning-pp)
9. [Quality Management (QM)](#9-quality-management-qm)
10. [Warehouse Management (WM)](#10-warehouse-management-wm)
11. [Finance (FI)](#11-finance-fi)
12. [Controlling (CO)](#12-controlling-co)
13. [Plant Maintenance (PM)](#13-plant-maintenance-pm)
14. [Human Resources (HR)](#14-human-resources-hr)
15. [Project System (PS)](#15-project-system-ps)
16. [Customer Relationship Management (CRM)](#16-customer-relationship-management-crm)
17. [Lab Information Management (LIMS)](#17-lab-information-management-lims)
18. [Business Intelligence (BI)](#18-business-intelligence-bi)
19. [Workflow Engine](#19-workflow-engine)
20. [Integration Hub](#20-integration-hub)
21. [Settings & Personalization](#21-settings--personalization)
22. [Keyboard Shortcuts](#22-keyboard-shortcuts)
23. [Troubleshooting](#23-troubleshooting)

---

## 1. Getting Started

### System Requirements
- Modern web browser (Chrome, Firefox, Edge, Safari)
- Network access to the YuktiraERP server

### Default Credentials
| Field | Value |
|-------|-------|
| Client | `1000` |
| User ID | `superadmin` |
| Password | `yuktira123` |

### URLs
| Application | URL |
|-------------|-----|
| Web Interface | http://localhost:5001 |
| REST API | http://localhost:5000 |
| Swagger (API Docs) | http://localhost:5000/swagger |

---

## 2. Login & Authentication

### Logging In

1. Open your browser and navigate to the Web URL
2. You'll see the **Sign In** screen
3. Enter:
   - **Client Number** — `1000` (tenant ID)
   - **User ID** — your username (e.g., `superadmin`)
   - **Password** — your password
4. Click **Sign In**

### Multi-Factor Authentication (MFA)
If MFA is enabled for your account:
1. After password entry, you'll be prompted for a **TOTP code**
2. Open your authenticator app (Google Authenticator, Authy, etc.)
3. Enter the 6-digit code
4. Click **Verify**

### Account Lockout
- After **5 failed login attempts**, your account is locked for **15 minutes**
- Contact your administrator to unlock immediately via the Admin module

### Session Timeout
- Sessions expire after a configurable period (default: 30 minutes)
- A warning modal appears **2 minutes** before expiry
- Click **Continue Session** to stay logged in, or **Log Off** to exit

---

## 3. Navigating the Interface

### Layout Overview

```
┌─────────────────────────────────────────────────────┐
│  ☰  🔍 Search...    🌙  🔔  🏠  ⚙️  EN ▼  🖨  🚪  │  ← Top Bar
├──────────┬──────────────────────────────────────────┤
│          │                                          │
│ SIDEBAR  │           MAIN CONTENT AREA              │
│          │                                          │
│ ○ MM     │   KPI Cards / Tabs / Data Grid / Forms   │
│ ○ SD     │                                          │
│ ○ PP     │                                          │
│ ○ QM     │                                          │
│ ○ WM     │                                          │
│ ○ FI     │                                          │
│ ○ ...    │                                          │
│          │                                          │
│──────────│                                          │
│ 🎨 Themes│                                          │
│ 👤 User  │                                          │
└──────────┴──────────────────────────────────────────┘
```

### Top Bar (Header)
| Icon | Action | Shortcut |
|------|--------|----------|
| ☰ | Toggle sidebar | — |
| 🔍 | Global search | `Ctrl+K` |
| 🌙 | Toggle dark mode | — |
| 🔔 | Notifications | — |
| 🏠 | Dashboard | — |
| ⚙️ | Settings | — |
| EN ▼ | Language selector | — |
| 🖨 | Print page | `Ctrl+P` |
| 🚪 | Logout | — |

### Sidebar Navigation
The sidebar is organized by **category** with colored icons:

| Category | Color | Modules |
|----------|-------|---------|
| Operations | Blue | MM, SD, WM, PP, QM, PM, CR, RF, WV, VS |
| Finance | Green | FI, CO, UJ, TX, CN |
| People | Pink | HR, CRM |
| Projects & Labs | Purple | PS, LIMS |
| Analytics | Violet | BI, AI, PD |
| Compliance | Red | SX |
| System | Gray | WF, APP, NOT, TCD, TCG, AUD, ADM, CST, INT, PLG, ME |

### Dashboard
The home screen shows:
- **KPI Cards** — Key performance indicators at a glance
- **Pinned Modules** — Your frequently used modules (click star to pin)
- **Recently Used** — Quick access to modules you've visited
- **Real-Time Activity** — Live system activity feed
- **All Modules** — Full module catalog grid

### Settings Page
Access via ⚙️ in the top bar:
- **Theme** — Switch between Modern, Classical, Minimal, Futuristic
- **Session Timeout** — Adjust session duration and warning time
- **Sidebar Behavior** — Collapse/expand preferences
- **Language** — Change UI language
- **Animations** — Enable/disable UI animations

---

## 4. Transaction Codes (T-Codes)

T-Codes are **SAP-style shortcuts** to specific functions. Access them via:

### Using the T-Code Search
1. Press `Ctrl+K` or click the search bar in the top bar
2. Type a T-Code (e.g., `VA01`, `ME21N`, `MIGO`)
3. Press Enter to navigate directly

### Using the T-Code Page
1. Click **TCD** (Transaction Codes) in the sidebar
2. Browse all available T-Codes organized by module
3. Click any T-Code to navigate to its page
4. Mark frequently used T-Codes as **Favorites** (star icon)

### Adding to Favorites
1. Navigate to any T-Code page
2. Click the **star** icon in the page header
3. The T-Code appears in your Favorites list

### Common T-Codes Quick Reference

| T-Code | Function | Module |
|--------|----------|--------|
| `MIGO` | Goods Movement (GR/GI/Transfer) | MM |
| `ME21N` | Create Purchase Order | MM |
| `ME51N` | Create Purchase Requisition | MM |
| `MIRO` | Invoice Verification | MM |
| `VA01` | Create Sales Order | SD |
| `VL01N` | Create Outbound Delivery | SD |
| `VF01` | Create Billing Document | SD |
| `CO01` | Create Production Order | PP |
| `CS01` | Create Bill of Materials | PP |
| `CR01` | Create Work Center | PP |
| `QA01` | Create Inspection Lot | QM |
| `QA11` | Usage Decision & Stock Posting | QM |
| `IE01` | Equipment Master | PM |
| `IW31` | Create Maintenance Order | PM |
| `FB50` | G/L Account Document | FI |
| `FB60` | Vendor Invoice | FI |

---

## 5. Modules Overview

YuktiraERP provides **35 modules** across 7 categories:

### Operations
| Module | Description | Key Functions |
|--------|-------------|---------------|
| **MM** | Materials Management | Material master, PO, GRN, Invoice Verification |
| **SD** | Sales & Distribution | Sales orders, deliveries, billing, customer master |
| **WM** | Warehouse Management | Bins, transfer orders, wave picks, inventory counting |
| **PP** | Production Planning | BOMs, routings, work centers, production orders |
| **QM** | Quality Management | Inspection lots, results, usage decisions, notifications |
| **PM** | Plant Maintenance | Equipment, functional locations, maintenance orders |
| **CR** | Customer Complaints | Return orders, credit memos, complaint tracking |
| **RF** | RF Warehouse | Handheld RF scanning, pick tasks |
| **WV** | Wave Pick | Wave-based picking optimization |
| **VS** | Velocity Slotting | ABC analysis, dynamic bin assignment |

### Finance
| Module | Description | Key Functions |
|--------|-------------|---------------|
| **FI** | Finance | GL, AP, AR, assets, bank reconciliation |
| **CO** | Controlling | Cost centers, allocations, order settlement |
| **UJ** | Universal Journal | Single-entry bookkeeping |
| **TX** | Tax Management | Tax codes, returns, compliance |
| **CN** | Consolidation | Multi-entity financial consolidation |

### People
| Module | Description | Key Functions |
|--------|-------------|---------------|
| **HR** | Human Resources | Employee master, payroll, time evaluation |
| **CRM** | Customer Relationship | Lead management, pipeline, activities |

### Projects & Labs
| Module | Description | Key Functions |
|--------|-------------|---------------|
| **PS** | Project System | WBS, project orders, timesheets |
| **LIMS** | Lab Information Mgmt | Test requests, results, certificates |

### Analytics
| Module | Description | Key Functions |
|--------|-------------|---------------|
| **BI** | Business Intelligence | Reports, dashboards, KPIs |
| **AI** | AI Analytics | OCR, forecasting, anomaly detection |
| **PD** | PP/DS Scheduling | Finite capacity scheduling |

### Compliance
| Module | Description | Key Functions |
|--------|-------------|---------------|
| **SX** | SOX Compliance | Audit trails, segregation of duties |

### System
| Module | Description | Access Level |
|--------|-------------|--------------|
| **WF** | Workflow Designer | PowerUser+ |
| **APP** | Approvals | All users |
| **NOT** | Notifications | All users |
| **TCD** | Transaction Codes | Admin |
| **TCG** | T-Code Generator | PowerUser+ |
| **AUD** | Audit Log | Admin |
| **ADM** | Administration | Admin |
| **CST** | Customize | Admin |
| **INT** | Integration Hub | Admin |
| **PLG** | Plugins | Admin |

---

## 6. Materials Management (MM)

### Overview
MM handles the entire procurement lifecycle — from material master data through purchase orders, goods receipts, and invoice verification.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| Material Master | MM02 | MM/Material | Create/edit materials (ROH, FERT, HALB, VERP, HIBE) |
| Purchase Order | ME21N | MM/PO | Create POs with line items, auto-calc totals |
| Purchase Requisition | ME51N | MM/PR | Internal purchase requests |
| Goods Receipt | MIGO | MM/GRN | Receive goods against PO |
| Invoice Verification | MIRO | MM/Invoice | Three-way match (PO/GRN/Invoice) |
| Vendor Master | BP | MM/Vendor | Vendor master data |

### Creating a Purchase Order (ME21N)

1. Navigate to **MM > PO > Create** or type `ME21N`
2. **Tab 1 — Header:**
   - Select Vendor from dropdown
   - Enter Cost Center and GL Account
   - Add Department reference
3. **Tab 2 — Line Items:**
   - Add material lines with quantity and unit price
   - System auto-calculates: `Qty × Price − Discount = Line Total`
   - Add multiple lines as needed
4. **Tab 3 — Delivery:**
   - Set Incoterms, Tax Code, Delivery Priority
5. Click **Save**

### Goods Receipt (MIGO)

1. Navigate to **MM > GRN > Create** or type `MIGO`
2. Select movement type (GR for PO, GI, Transfer)
3. Reference the PO number
4. Enter quantities received
5. Post — stock updates automatically

### Stock Overview
Navigate to **MM > Stock** to see:
- Current stock levels by material and plant
- Stock movements history
- Valuation summary

---

## 7. Sales & Distribution (SD)

### Overview
SD manages the order-to-cash cycle — sales orders, deliveries, billing, and customer master data.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| Sales Order | VA01 | SD/SalesOrder | Create customer sales orders |
| Customer Master | XD01 | SD/Customer | Customer master data |
| Outbound Delivery | VL01N | SD/Delivery | Create delivery documents |
| Billing | VF01 | SD/Billing | Create invoices |
| Customer Complaint | CRRETURN | Transactions/Engine/CRRETURN | Return orders & credit memos |

### Creating a Sales Order (VA01)

1. Navigate to **SD > SalesOrder > Create** or type `VA01`
2. **Tab 1 — Sold-To:**
   - Select Customer
   - Set Payment Terms
   - Assign Ship-To and Bill-To addresses
3. **Tab 2 — Items:**
   - Add material lines with quantity
   - System auto-calculates line totals
   - Assign Plant and Storage Location per line
4. **Tab 3 — Organization:**
   - Set Sales Org, Distribution Channel, Division
   - Configure Incoterms
5. Click **Save**

### Delivery Process (VL01N → VL02N)

1. **VL01N** — Create outbound delivery referencing the sales order
2. **VL02N** — Pick, pack, and post Goods Issue (PGI)
3. Stock is deducted, revenue is recognized

### Billing (VF01)

1. Create billing document referencing the delivery
2. Invoice is generated with tax calculations
3. Posts to FI as Accounts Receivable

---

## 8. Production Planning (PP)

### Overview
PP manages the manufacturing lifecycle — BOMs, routings, work centers, production orders, and MRP.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| Bill of Materials | CS01 | PP/BOM | Define product structure |
| Work Center | CR01 | PP/WorkCenter | Machines, labor, capacity |
| Production Routing | CA01 | PP/Routing | Operation sequence |
| Production Order | CO01 | PP/ProductionOrder | Manufacturing orders |
| Production Plan | MD61 | PP/Plan | Planned independent requirements |
| MRP | MD02 | PP/MrpStock | Material requirements planning |
| PP/DS | PPDS | Transactions/Engine/PPDS | Finite scheduling |

### Creating a Bill of Materials (CS01)

1. Navigate to **PP > BOM > Create** or type `CS01`
2. Enter Header Material (the finished product)
3. Add component lines:
   - Material number
   - Quantity per unit
   - UOM (Unit of Measure)
4. Set valid-from date
5. Save — BOM is now available for production orders

### Creating a Work Center (CR01)

1. Navigate to **PP > WorkCenter > Create** or type `CR01`
2. Enter work center name and description
3. Assign capacity (machine hours, labor hours)
4. Set shift definitions and efficiency ratings
5. Assign cost center for cost allocation
6. Save

### Production Order Lifecycle (CO01)

1. **Create (CO01):**
   - Select material, quantity, BOM, routing
   - System calculates material requirements and planned costs
2. **Schedule:**
   - Finite scheduling considers capacity constraints
   - System proposes start/end dates
3. **Release:**
   - Order is released for shop floor execution
4. **Confirm (CO11N):**
   - Report actual quantities, scrap, activity times
   - System posts goods movements and activity costs
5. **Settle (KO88):**
   - Transfer order costs to CO objects

### MRP Run (MD02)

1. Navigate to **PP > MrpStock** or type `MD02`
2. Select material and plant
3. Run MRP — system calculates:
   - Net requirements
   - Planned orders
   - Purchase requisitions
   - Exception messages
4. Review shortage alerts and action items

---

## 9. Quality Management (QM)

### Overview
QM manages quality inspection throughout procurement, production, and sales — from inspection plans through usage decisions.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| Inspection Plan | QP01 | QM/InspectionPlan | Define inspection characteristics |
| Inspection Lot | QA01 | QM/InspectionLot | Create inspection lots |
| Inspection Results | QE01 | QM/InspectionResult | Record measurements |
| Usage Decision | QA11 | QM/UsageDecision | Accept/reject stock posting |
| Quality Notification | QM01 | QM/Notification | Defect reporting & CAPA |
| Quality Certificate | QC21 | QM/COA | Certificate of Analysis |

### Creating an Inspection Lot (QA01)

1. Navigate to **QM > InspectionLot > Create** or type `QA01`
2. **Tab 1 — Lot Data:**
   - Select Inspection Type (01-Procurement, 02-Production, etc.)
   - Enter Material, Batch, Plant
3. **Tab 2 — Inspection Parameters:**
   - Reference Inspection Plan
   - Assign Inspector
   - Set Sample Size and Scope
4. **Tab 3 — Sample & Decision:**
   - Define Sampling Procedure
   - Set Acceptance Number
   - Stock Proposal (unrestricted, blocked, scrap)
5. Save — lot is created in "Open" status

### Recording Results (QE01 / QE51N)

1. Open the inspection lot
2. Enter measured values for each characteristic
3. System evaluates: Pass/Fail based on specification limits
4. Record defects if any (defect code, root cause)
5. Save — lot status updates to "Results Recorded"

### Usage Decision (QA11)

1. Open the inspection lot with recorded results
2. **Tab 1 — Decision:**
   - Select UD Code (Accept, Reject, Rework, Scrap)
   - Enter Quality Score
3. **Tab 2 — Stock Posting:**
   - Unrestricted Stock (accepted quantity)
   - Blocked Stock (quarantine)
   - Scrap Quantity (rejected)
4. Post — stock is moved accordingly

### Quality Notifications (QM01)

For defect tracking and corrective/preventive actions:
1. Create notification with defect description
2. Assign Impact and Root Cause
3. Track corrective actions to closure
4. Link to related inspection lots

### Certificates of Analysis (QC21)

Generate COA documents for customers:
1. Reference inspection lot and results
2. System generates certificate with all test results
3. Print or export as PDF

---

## 10. Warehouse Management (WM)

### Overview
WM provides advanced warehouse operations — bin management, transfer orders, wave picking, inventory counting, and RF scanning.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| Storage Locations | OX09 | WM/StorageLocation | Define storage bins |
| Transfer Orders | — | WM/TransferOrder | Move stock between bins |
| Wave Picks | WAVEPK | Transactions/Engine/WAVEPK | Wave-based picking |
| Inventory Count | — | WM/InventoryCount | Cycle counting |
| RF Scanner | RFSCAN | Transactions/Engine/RFSCAN | Handheld operations |
| Velocity Slotting | VSLOTT | Transactions/Engine/VSLOTT | ABC bin assignment |

### Bin Management

Each storage location has bins organized by:
- **Storage Type** (bulk, rack, shelf, cold storage)
- **Section** (aisle, zone)
- **Capacity** (max weight, max volume)
- **Strategy** (FIFO, LIFO, FEFO)

### Creating a Storage Location (OX09)

1. Navigate to **WM > StorageLocation > Create**
2. **Tab 1 — General Data:**
   - Storage type, Plant, Section
3. **Tab 2 — Capacity:**
   - Max weight, max volume, bin layout
4. **Tab 3 — Settings:**
   - Putaway strategy (FIFO/LIFO/FEFO)
   - Enable batch/serial/QI flags
5. Save

### Wave Picking (WAVEPK)

1. Multiple sales order deliveries are grouped into a **wave**
2. System optimizes pick path across warehouse
3. Picker receives consolidated pick list
4. Pick, confirm, and pack in one flow
5. Post Goods Issue

### RF Scanning (RFSCAN)

For handheld device operations:
- Scan bin barcode → system shows items to pick/put
- Confirm quantities by scanning
- Real-time stock updates

### Inventory Counting

1. Create count document for a storage location
2. System generates count sheets (physical count)
3. Enter actual quantities
4. System posts differences (adjustments)
5. Revaluation if needed

---

## 11. Finance (FI)

### Overview
FI handles all financial accounting — General Ledger, Accounts Payable, Accounts Receivable, assets, and bank reconciliation.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| G/L Document | FB50 | FI/GL | Post journal entries |
| Vendor Invoice | FB60 | FI/AP | Record vendor invoices |
| Customer Invoice | FB70 | FI/AR | Record customer invoices |
| Vendor Payment | F-53 | FI/VendorPayment | Outgoing payments |
| Customer Payment | F-28 | FI/CustomerPayment | Incoming payments |
| Fixed Asset | AS01 | FI/FixedAsset | Asset master & depreciation |

### Posting a G/L Document (FB50)

1. Navigate to **FI > GL > Create** or type `FB50`
2. Enter document date and reference
3. Add line items:
   - G/L Account number
   - Debit or Credit amount
   - Cost Center (for CO assignment)
   - Tax Code
4. System validates debits = credits
5. Post — journal entry is created

### Vendor Invoice (FB60)

1. Enter vendor, invoice number, date
2. Add line items (expense accounts)
3. System calculates tax
4. Posts to AP (vendor sub-ledger) and GL

### Payment Processing

**Outgoing (F-53):**
1. Select vendor
2. System shows open invoices (oldest first — FIFO)
3. Select invoices to pay
4. System posts: Debit AP, Credit Bank

**Incoming (F-28):**
1. Select customer
2. System shows open invoices
3. Enter payment amount
4. System posts: Debit Bank, Credit AR

### Fixed Assets (AS01)

1. Create asset master (category, class, location)
2. Set depreciation method (SLM, WDV, DDB, SYD, UOP)
3. System calculates depreciation schedule
4. Month-end: post depreciation runs
5. Disposal/Transfer: system posts gain/loss to GL

---

## 12. Controlling (CO)

### Overview
CO manages internal cost accounting — cost centers, allocations, and order settlement.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| Cost Center | KS01 | CO/CostCenter | Create cost centers |
| Cost Allocation | KB11N | CO/Allocation | Distribute costs |
| Order Settlement | KO88 | CO/Settle | Settle PM/PP orders |

### Cost Center Setup (KS01)

1. Create cost center with name and description
2. Assign to organizational unit
3. Set budget and planning data
4. Define cost element structure

### Cost Allocation (KB11N)

1. Select allocation rule (proportional by headcount, area, etc.)
2. Enter the costs to distribute
3. System splits costs across receiving cost centers
4. Post allocation document

---

## 13. Plant Maintenance (PM)

### Overview
PM manages asset maintenance — equipment masters, functional locations, maintenance orders, and notifications.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| Equipment | IE01 | PM/Equipment | Equipment master data |
| Functional Location | IL01 | PM/Location | Organizational structure |
| Maintenance Notification | IW21 | PM/Notification | Report breakdowns/issues |
| Maintenance Order | IW31 | PM/Order | Plan & execute maintenance |
| Maintenance Plan | — | PM/Plan | Preventive maintenance scheduling |
| Spare Parts | — | PM/Spares | Spare part inventory |

### Equipment Master (IE01)

1. Enter equipment ID and description
2. Set Category (M=Machine, P=Plant, I=IT)
3. Assign Serial Number, Manufacturer, Model
4. Link to Functional Location
5. Set Cost Center for maintenance costs

### Maintenance Order (IW31)

1. **Tab 1 — Order Header:**
   - Select Order Type (PM01-Corrective, PM02-Preventive, PM03-Emergency, PM04-Refurbishment)
   - Reference Equipment or Functional Location
2. **Tab 2 — Operations:**
   - Define work steps
   - Assign Work Center
   - Set planned hours
3. **Tab 3 — Scheduling:**
   - Scheduled Start/Finish dates
   - Planned vs Actual hours
4. Release → Execute → Confirm (IW41) → TECO (IW32)

### Preventive Maintenance Plans

1. Create Maintenance Plan with schedule (daily/weekly/monthly)
2. Assign Task List (standard maintenance procedures)
3. System generates orders automatically based on schedule
4. Track compliance and overdue plans

---

## 14. Human Resources (HR)

### Overview
HR manages employee data, payroll, and time evaluation.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| Employee Master | PA30 | HR/Employee | Employee records |
| Payroll | — | HR/Payroll | Salary processing |

### Employee Master (PA30)

1. Create employee record with personal details
2. Assign Organizational Unit, Position, Cost Center
3. Set Employment dates, salary, benefits
4. Track qualifications and training

### Payroll Processing

1. Run payroll for a period
2. System calculates:
   - Basic salary, allowances, deductions
   - PF, ESI, PT, TDS
   - Net pay
3. Generate payslips
4. Post to FI (salary expense, liability)

---

## 15. Project System (PS)

### Overview
PS manages project planning, execution, and monitoring.

### Key Pages

| Page | T-Code | Path | Description |
|------|--------|------|-------------|
| Project | — | PS/Project | Create projects |
| Project Task | — | PS/ProjTask | Work breakdown structure |
| Timesheet | — | PS/Timesheet | Time recording |

### Creating a Project

1. Define project header (name, dates, budget)
2. Create WBS (Work Breakdown Structure) elements
3. Assign resources and costs to WBS elements
4. Track progress against plan

### Timesheet Entry

1. Select project/WBS element
2. Enter hours worked per activity
3. System posts to project and triggers CO allocation

---

## 16. Customer Relationship Management (CRM)

### Overview
CRM manages customer interactions, leads, and sales pipeline.

### Key Pages

| Page | Path | Description |
|------|------|-------------|
| Lead Management | CRM/Lead | Capture and qualify leads |
| Opportunity | CRM/Opportunity | Track sales opportunities |
| Activities | CRM/Activity | Calls, meetings, tasks |

### Lead-to-Opportunity Flow

1. **Capture Lead** — from web form, import, or manual entry
2. **Qualify** — assess fit and budget
3. **Convert to Opportunity** — track through sales stages
4. **Close Won/Lost** — record outcome

---

## 17. Lab Information Management (LIMS)

### Overview
LIMS manages laboratory testing, sample tracking, and results.

### Key Pages

| Page | Path | Description |
|------|------|-------------|
| Test Request | LIMS/Request | Create test requests |
| Sample | LIMS/Sample | Sample registration |
| Test Result | LIMS/Result | Record test results |

### Testing Workflow

1. **Create Test Request** — specify test type and material
2. **Register Sample** — assign sample ID and storage location
3. **Execute Tests** — record measurements
4. **Review & Approve** — QA review of results
5. **Issue Certificate** — generate COA if required

---

## 18. Business Intelligence (BI)

### Overview
BI provides reporting, dashboards, and KPI monitoring.

### Key Pages

| Page | Path | Description |
|------|------|-------------|
| Dashboard | BI/Dashboard | Visual dashboards |
| Reports | BI/Report | Create and run reports |
| KPIs | Kpi | KPI formula engine |

### Creating a Report

1. Navigate to **BI > Report > Create**
2. Select data source (entity or SQL view)
3. Choose columns and filters
4. Select visualization type (table, chart, pivot)
5. Save and share

### KPI Monitoring

- **Formula Engine** — Define KPIs with calculation logic
- **5 Predefined KPIs** — Revenue, Orders, Stock, Production, Quality
- **Real-time Updates** — Dashboard refreshes via SignalR

---

## 19. Workflow Engine

### Overview
Workflow manages business process automation — approvals, notifications, and routing rules.

### Key Pages

| Page | Path | Description |
|------|------|-------------|
| Workflow Designer | Workflow/Designer | Design BPMN workflows |
| Instances | Workflow/Instances | Running workflow instances |
| Approvals | Approval | Pending approval tasks |

### Workflow Designer

1. Drag-and-drop BPMN elements onto canvas
2. **Node Types:**
   - **Start** — Trigger point
   - **Task** — User action
   - **Approval** — Manager review
   - **Decision** — Conditional branching
   - **Email** — Send notification
   - **End** — Process complete
3. Connect nodes with sequence flows
4. Define conditions and expressions
5. Activate workflow

### Approval Process

1. Transaction triggers workflow (e.g., PO above threshold)
2. Approver receives notification
3. Review and Approve/Reject
4. System routes to next step or returns to originator

---

## 20. Integration Hub

### Overview
Integration Hub connects YuktiraERP with external systems via webhooks, EDI, and API clients.

### Key Pages

| Page | Path | Description |
|------|------|-------------|
| API Clients | Integration | Manage client credentials |
| Webhooks | Integration | Register event endpoints |
| EDI Partners | Integration | Trading partner profiles |
| Queue | Integration | Outbound message queue |

### Webhooks

1. Register a webhook endpoint (URL + event type)
2. When the event occurs (e.g., `order.created`), YuktiraERP sends a POST to your URL
3. Payload includes event data + HMAC signature in `X-Webhook-Secret` header
4. Retry logic: 3 attempts with exponential backoff
5. Dead-letter queue for failed deliveries

### EDI (Electronic Data Interchange)

1. Create trading partner profile (EDIFACT/X12 standard)
2. Convert documents to EDIFACT or X12 format
3. Parse incoming interchanges
4. Track acknowledgments

### API Authentication

Third-party systems authenticate via:
```bash
curl -X POST http://localhost:5000/api/v1/integration/validate \
  -H "Content-Type: application/json" \
  -d '{"clientId":"client-1","clientSecret":"secret-1"}'
```

IP whitelisting enforced per client.

---

## 21. Settings & Personalization

### Theme Selection
Choose from 4 UI themes via the **colored dots** in the sidebar footer:

| Theme | Style |
|-------|-------|
| **Modern** | Clean, blue-accented, rounded corners |
| **Classical** | Traditional, serif fonts, muted colors |
| **Minimal** | Flat, whitespace-focused, minimal borders |
| **Futuristic** | Dark gradients, neon accents, bold |

Dark mode toggles via the 🌙 icon in the top bar.

### Language Selection
Click **EN ▼** in the top bar to switch languages:

| Code | Language |
|------|----------|
| EN | English |
| HI | Hindi (हिन्दी) |
| TA | Tamil (தமிழ்) |
| TE | Telugu (తెలుగు) |
| FR | French (Français) |
| ES | Spanish (Español) |

### Session Timeout
Configure in **Settings > Session**:
- **Session Duration** — How long before auto-logout (5–120 minutes)
- **Warning Before** — Minutes before timeout to show warning (1–10 minutes)

### Sidebar Behavior
- **Collapse/Expand** — Toggle sidebar width
- **Module Grouping** — Organize by category
- **Favorites** — Pin frequently used modules

---

## 22. Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+K` | Global search (T-Code lookup) |
| `Ctrl+P` | Print current page |
| `Escape` | Close modal/dialog |
| `Tab` | Move to next form field |
| `Shift+Tab` | Move to previous form field |
| `Enter` | Submit form / Confirm action |

---

## 23. Troubleshooting

### Common Issues

**Login fails with "Invalid credentials"**
- Verify Client Number (`1000`), User ID, and Password
- Check if account is locked (5 failed attempts → 15-min lockout)
- Contact admin to reset password

**Page shows 500 error**
- Check if the database is running
- Verify the API server is running on port 5000
- Check application logs at `logs/web-*.log`

**T-Code returns 404**
- The T-Code may not be registered in the system
- Check `Transactions` page for available codes
- Contact admin to register new T-Codes

**Form submission fails**
- Check all required fields (marked with *)
- Verify numeric fields don't contain text
- Check date format (YYYY-MM-DD)
- Ensure dropdown selections are valid

**Stock shows negative**
- Check goods movement postings
- Review stock movements in MM > Stock Movements
- Verify GRN was posted correctly

**Workflow stuck**
- Check Approval page for pending tasks
- Verify workflow is active in Designer
- Contact admin if escalation is needed

### Application Logs

| Log File | Location | Content |
|----------|----------|---------|
| Web API | `logs/api-*.log` | REST API requests/responses |
| Web App | `logs/web-*.log` | UI application events |
| Database | PostgreSQL logs | SQL queries, connections |

### Health Check

Verify system health:
```
GET http://localhost:5000/health
GET http://localhost:5001/health
```

Both return JSON status with database ping.

### Backup & Restore

```powershell
# Backup database
.\scripts\backup.ps1

# Restore from backup
.\scripts\restore.ps1 -BackupFile ".\database\backup\yuktira_erp_YYYYMMDD_HHMMSS.sql"
```

---

## Appendix: All Transaction Codes (75)

### Materials Management
| T-Code | Title |
|--------|-------|
| MIGO | Goods Movement - Posting |
| MM02 | Change Material |
| ME21N | Create Purchase Order |
| ME51N | Create Purchase Requisition |
| ME28 | PO Release / Approval |
| MIRO | Invoice Verification (LIV) |
| BP | Create Business Partner |
| CRSRET | Supplier Return Delivery |
| MRPEVT | MRP Event Monitor |

### Sales & Distribution
| T-Code | Title |
|--------|-------|
| VA01 | Create Sales Order |
| VL01N | Create Outbound Delivery |
| VL02N | Change Outbound Delivery / PGI |
| VF01 | Create Billing Document |
| CRRETURN | Customer Complaint & Return |
| CRCREDIT | Customer Credit Memo |

### Production Planning
| T-Code | Title |
|--------|-------|
| CO01 | Create Production Order |
| MD61 | Planned Independent Requirements |
| MD02 | Material Requirements Planning |
| CO11N | Production Order Confirmation |
| CS01 | Create Bill of Materials |
| CR01 | Create Work Center |
| PPDS | PP/DS Finite Scheduling |

### Quality Management
| T-Code | Title |
|--------|-------|
| QE01 | Record Inspection Lot Results |
| QE51N | Record Inspection Results |
| QM01 | Create Quality Notification |
| QM02 | Change Quality Notification |
| QM03 | Quality Notification Tasks |
| QM11 | Record Inspection Results |
| QM12 | Manage Usage Decisions |
| ZQM1 | QM Master Data Setup |
| 1FM | QM in Procurement |
| 2F9 | Quality Notification - Supplier |
| 1E1 | QM in Production |
| 2QP | Quality Notification - Internal |
| 2QN | Manual Inspection |
| QMM | Recurring Batch Inspection |
| 1MP | Outbound Delivery Inspection |
| BKR | Customer Return Inspection |
| 2FA | Quality Notification - Customer |
| CALIB | Calibration Inspection |
| QP01 | Create Inspection Plan |
| QA01 | Create Inspection Lot |
| QN01 | Create Quality Notification |
| QA11 | Usage Decision & Stock Posting |
| QC21 | Quality Certificate (COA) |
| CRINSPECT | Quality Inspection - Return |
| CRUDPOST | Post Usage Decision - Return |
| CRSUPPLY | Supplier Complaint & Claim |

### Finance
| T-Code | Title |
|--------|-------|
| FB50 | Enter G/L Account Document |
| FB60 | Vendor Invoice Entry |
| FBL1N | Vendor Line Item Display |
| F-53 | Vendor Outgoing Payment |
| F-28 | Customer Incoming Payment |
| ABZN | Asset Acquisition |
| UNIJRN | Universal Journal Entry |
| CONSOL | Consolidation Workbench |
| TAXRET | Tax Return Filing |
| CRDEBIT | Supplier Debit Memo |

### Controlling
| T-Code | Title |
|--------|-------|
| KB11N | Cost Center Allocation |
| KO88 | Settle Production / PM Order |
| KS01 | Create Cost Center |

### Human Resources
| T-Code | Title |
|--------|-------|
| PA30 | Maintain HR Master Data |

### Plant Maintenance
| T-Code | Title |
|--------|-------|
| IE01 | Equipment Master Creation |
| IW21 | Create Maintenance Notification |
| IW31 | Create Maintenance Order |
| IW41 | PM Order Confirmation |
| IW32 | Change Maintenance Order |
| IL01 | Create Functional Location |

### Warehouse Management
| T-Code | Title |
|--------|-------|
| RFSCAN | RF Scanner Menu |
| RFPICK | RF Pick Task |
| WAVEPK | Wave Pick Management |
| VSLOTT | Velocity Slotting |

### Compliance & AI
| T-Code | Title |
|--------|-------|
| SOXADM | SOX Compliance Administration |
| AIOCR | Document OCR Processing |

---

*Yuktira ERP Suite — Intelligence Driven Enterprise Platform*
