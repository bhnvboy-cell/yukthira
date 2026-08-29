# YuktiraERP — User Guide

> **Version 1.0.8** | August 2026
>
> *Welcome! This guide will walk you through everything you need to know to start using YuktiraERP — even if you've never used an ERP system before.*

---

## Table of Contents

1. [Welcome — What is an ERP?](#1-welcome--what-is-an-erp)
2. [Your First Login](#2-your-first-login)
3. [Understanding the Screen](#3-understanding-the-screen)
4. [Your First Task — Create a Material](#4-your-first-task--create-a-material)
5. [Common Workflows](#5-common-workflows)
6. [All Modules Explained](#6-all-modules-explained)
7. [Transaction Codes — Quick Navigation](#7-transaction-codes--quick-navigation)
8. [Personalizing Your Experience](#8-personalizing-your-experience)
9. [Tips for New Users](#9-tips-for-new-users)
10. [Troubleshooting & Help](#10-troubleshooting--help)

---

## 1. Welcome — What is an ERP?

### What does ERP mean?
**ERP** stands for **Enterprise Resource Planning**. It's a system that helps businesses manage all their key processes in one place:

- **Buying materials** from suppliers (Procurement)
- **Making products** in the factory (Production)
- **Storing goods** in the warehouse (Inventory)
- **Selling to customers** (Sales)
- **Tracking money** (Finance)
- **Managing people** (HR)
- **Maintaining equipment** (Plant Maintenance)

### Why YuktiraERP?
YuktiraERP is **free and open-source**. You get all the power of expensive ERP systems (like SAP) without the cost. It's designed to be:

- **Easy to learn** — Modern, clean interface
- **Fast to use** — Keyboard shortcuts and quick-search
- **Fully featured** — 35 modules covering every business process
- **Customizable** — Change themes, languages, and workflows

### What you'll learn in this guide
By the end, you'll be able to:
1. Log in and navigate the system
2. Create your first records (materials, orders, etc.)
3. Understand how different modules work
4. Customize the look and feel
5. Find help when you need it

---

## 2. Your First Login

### Step 1: Open your browser
Open **Chrome**, **Firefox**, **Edge**, or **Safari** and go to:
```
http://localhost:5001
```

### Step 2: Enter your credentials
You'll see a login screen. Fill in:

| Field | What to type | Example |
|-------|-------------|---------|
| **Client Number** | Your company/tenant ID | `1000` |
| **User ID** | Your username | `superadmin` |
| **Password** | Your password | `yuktira123` |

### Step 3: Click "Sign In"
You're in! You'll see the **Dashboard** — the home screen of the system.

### First-time tips:
- **Don't share your password** — Each person should have their own account
- **Your session expires** after inactivity — A warning will appear 2 minutes before
- **If you forget your password** — Contact your system administrator

---

## 3. Understanding the Screen

When you log in, here's what you'll see:

```
┌─────────────────────────────────────────────────────┐
│  ☰  🔍 Search...    🌙  🔔  🏠  ⚙️  EN ▼  🖨  🚪  │
├──────────┬──────────────────────────────────────────┤
│          │                                          │
│ SIDEBAR  │           MAIN CONTENT AREA              │
│          │                                          │
│ ○ MM     │   ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐      │
│ ○ SD     │   │ KPI │ │ KPI │ │ KPI │ │ KPI │      │
│ ○ PP     │   └─────┘ └─────┘ └─────┘ └─────┘      │
│ ○ QM     │                                          │
│ ○ WM     │   [Tab1] [Tab2] [Tab3]                  │
│ ○ FI     │                                          │
│ ○ ...    │   ┌────────────────────────────────┐    │
│          │   │     Data Grid / Table           │    │
│          │   │     (rows and columns)          │    │
│          │   └────────────────────────────────┘    │
│──────────│                                          │
│ 🎨 Themes│                                          │
│ 👤 User  │                                          │
└──────────┴──────────────────────────────────────────┘
```

### The Top Bar (Header)
This is the strip across the top of the screen:

| Button | What it does | Shortcut |
|--------|-------------|----------|
| **☰** | Show/hide the sidebar | — |
| **🔍 Search** | Find anything (modules, T-codes, records) | `Ctrl+K` |
| **🌙** | Toggle dark mode on/off | — |
| **🔔** | View notifications | — |
| **🏠** | Go to Dashboard (home) | — |
| **⚙️** | Open Settings | — |
| **EN ▼** | Change language | — |
| **🖨** | Print the current page | `Ctrl+P` |
| **🚪** | Log out | — |

### The Sidebar (Left Panel)
The sidebar lists all **modules** organized by category:

| Color | Category | What it's for |
|-------|----------|--------------|
| 🔵 Blue | **Operations** | Buying, selling, making, storing |
| 🟢 Green | **Finance** | Money, accounting, taxes |
| 🩷 Pink | **People** | Employees, customers |
| 🟣 Purple | **Projects & Labs** | Projects, lab testing |
| 🟦 Violet | **Analytics** | Reports, dashboards, AI |
| 🟥 Red | **Compliance** | SOX audit trail |
| ⬜ Gray | **System** | Admin, workflows, settings |

**Click any module** to open it. For example, click **MM** to open Materials Management.

### The Main Content Area
This is where you work. It can show:
- **KPI Cards** — Numbers at a glance (like "Open Orders: 12")
- **Tabs** — Switch between different views (Overview, Create, List)
- **Data Grid** — A table of records you can sort, filter, and export
- **Forms** — Fill in fields to create or edit records

---

## 4. Your First Task — Create a Material

Let's create your first **Material Master** record. A material is anything your company buys, makes, or sells.

### Step 1: Navigate to Materials
1. Click **MM** in the sidebar (or type `MM02` in the search bar)
2. You'll see the Materials Management module

### Step 2: Click "Create"
Look for a **+ Create** button (usually in the top-right or under a tab). Click it.

### Step 3: Fill in the form
You'll see a multi-tab form. Start with **Tab 1 — Basic Data**:

| Field | What to enter | Example |
|-------|--------------|---------|
| **Material Code** | Unique ID for this material | `RAW-001` |
| **Description** | What is it? | `Steel Sheet 2mm` |
| **Material Type** | What kind? | `ROH` (Raw Material) |
| **Unit of Measure** | How do you count it? | `KG` (Kilograms) |

### Step 4: Fill in more tabs
Click through the other tabs and fill in what you need:

**Tab 2 — Purchasing:**
- Purchasing Group: Who buys it?
- Minimum Order Quantity: Smallest order allowed
- Lead Time: How many days to deliver?

**Tab 3 — Accounting:**
- Price Control: `S` (Standard) or `V` (Moving Average)
- Valuation Class: How to account for it
- Currency: `INR` or `USD`

**Tab 4 — Plant & Inventory:**
- Plant: Which factory/warehouse?
- Safety Stock: Minimum stock to keep
- Reorder Point: When to order more

### Step 5: Save
Click **Save** at the bottom of the form. Your material is now in the system!

### You just created your first record!
That's how every creation form works in YuktiraERP:
1. Navigate to the module
2. Click Create
3. Fill in the tabs
4. Save

---

## 5. Common Workflows

Here are the most common business processes you'll use:

### Workflow 1: Buy Something (Procurement)

```
Purchase Requisition → Purchase Order → Goods Receipt → Invoice → Payment
     (ME51N)              (ME21N)          (MIGO)        (MIRO)     (F-53)
```

**Step by step:**
1. **Create Purchase Requisition (ME51N)** — "We need 100 steel sheets"
2. **Create Purchase Order (ME21N)** — "Buy from Supplier X at $10 each"
3. **Receive Goods (MIGO)** — "100 sheets arrived, inspected, accepted"
4. **Verify Invoice (MIRO)** — "Supplier sent invoice for $1,000"
5. **Make Payment (F-53)** — "Pay the supplier"

### Workflow 2: Sell Something (Order-to-Cash)

```
Sales Order → Delivery → Billing → Payment
  (VA01)      (VL01N)   (VF01)    (F-28)
```

**Step by step:**
1. **Create Sales Order (VA01)** — "Customer wants 50 widgets"
2. **Create Delivery (VL01N)** — "Ship 50 widgets to customer"
3. **Create Invoice (VF01)** — "Bill customer $5,000"
4. **Receive Payment (F-28)** — "Customer paid"

### Workflow 3: Make Something (Production)

```
BOM → Routing → Work Center → Production Order → Confirm → Settlement
(CS01)  (CA01)      (CR01)        (CO01)         (CO11N)    (KO88)
```

**Step by step:**
1. **Create BOM (CS01)** — "Widget needs 2 screws, 1 motor"
2. **Create Routing (CA01)** — "Steps: Cut → Weld → Paint → Pack"
3. **Create Work Center (CR01)** — "Assembly Line 1, 8 hours/day"
4. **Create Production Order (CO01)** — "Make 100 widgets"
5. **Confirm (CO11N)** — "Actually made 98, 2 were defective"
6. **Settle (KO88)** — "Transfer costs to accounting"

### Workflow 4: Check Quality (Quality Management)

```
Inspection Plan → Inspection Lot → Results → Usage Decision
     (QP01)          (QA01)        (QE01)       (QA11)
```

**Step by step:**
1. **Create Inspection Plan (QP01)** — "Test dimensions, weight, appearance"
2. **Create Inspection Lot (QA01)** — "Test batch 2026-001"
3. **Record Results (QE01)** — "Dimension: 10.02mm (Pass), Weight: 5.1kg (Pass)"
4. **Usage Decision (QA11)** — "Accept 98 units, Scrap 2 units"

---

## 6. All Modules Explained

### Operations (Making, Buying, Selling, Storing)

| Module | What it does | Who uses it |
|--------|-------------|-------------|
| **MM** (Materials Management) | Buy materials, receive goods, verify invoices | Purchasing team |
| **SD** (Sales & Distribution) | Take orders, ship products, bill customers | Sales team |
| **WM** (Warehouse Management) | Store goods, pick orders, count inventory | Warehouse team |
| **PP** (Production Planning) | Plan production, create orders, track output | Production team |
| **QM** (Quality Management) | Test products, record results, make decisions | Quality team |
| **PM** (Plant Maintenance) | Maintain equipment, schedule repairs | Maintenance team |
| **CR** (Customer Complaints) | Handle returns, issue credit memos | Customer service |

### Finance (Money & Accounting)

| Module | What it does | Who uses it |
|--------|-------------|-------------|
| **FI** (Finance) | General ledger, accounts payable/receivable, assets | Accountants |
| **CO** (Controlling) | Cost centers, allocations, order settlement | Cost accountants |
| **UJ** (Universal Journal) | Single-entry bookkeeping | Finance team |
| **TX** (Tax Management) | Tax codes, returns, compliance | Tax team |
| **CN** (Consolidation) | Multi-entity financial reports | CFO/Finance |

### People (Employees & Customers)

| Module | What it does | Who uses it |
|--------|-------------|-------------|
| **HR** (Human Resources) | Employee records, payroll, time tracking | HR team |
| **CRM** (Customer Relationship) | Leads, opportunities, activities | Sales/Marketing |

### Projects & Labs

| Module | What it does | Who uses it |
|--------|-------------|-------------|
| **PS** (Project System) | Project planning, WBS, timesheets | Project managers |
| **LIMS** (Lab Information) | Test requests, samples, results | Lab technicians |

### Analytics (Reports & Intelligence)

| Module | What it does | Who uses it |
|--------|-------------|-------------|
| **BI** (Business Intelligence) | Custom reports, dashboards, charts | Analysts |
| **AI** (AI Analytics) | Forecasting, anomaly detection, OCR | Data scientists |
| **PD** (PP/DS Scheduling) | Finite capacity scheduling | Production planners |

### Compliance & System

| Module | What it does | Who uses it |
|--------|-------------|-------------|
| **SX** (SOX Compliance) | Audit trails, segregation of duties | Compliance officers |
| **WF** (Workflows) | Business process automation | Admins |
| **APP** (Approvals) | Review and approve requests | Managers |

---

## 7. Transaction Codes — Quick Navigation

**T-Codes** are shortcuts like `VA01` or `MIGO` that take you directly to a function — just like in SAP.

### How to use T-Codes

**Method 1: Search Bar**
1. Press `Ctrl+K` (or click the search bar)
2. Type the T-Code (e.g., `VA01`)
3. Press Enter — you're there!

**Method 2: T-Code Page**
1. Click **TCD** in the sidebar
2. Browse all 75 T-codes
3. Click one to go there

### Most Used T-Codes

| T-Code | What it does | When to use |
|--------|-------------|-------------|
| `MIGO` | Goods Movement | Receive or ship goods |
| `ME21N` | Create Purchase Order | Buy something |
| `VA01` | Create Sales Order | Sell something |
| `CO01` | Create Production Order | Make something |
| `QA01` | Create Inspection Lot | Test quality |
| `FB50` | Post Journal Entry | Record accounting |
| `IE01` | Equipment Master | Register equipment |
| `IW31` | Maintenance Order | Fix equipment |

### All 75 T-Codes

<details>
<summary>Click to see all T-Codes by module</summary>

**Materials Management (MM)**
- `MIGO` — Goods Movement
- `MM02` — Change Material
- `ME21N` — Create Purchase Order
- `ME51N` — Create Purchase Requisition
- `ME28` — PO Release / Approval
- `MIRO` — Invoice Verification
- `BP` — Create Business Partner
- `CRSRET` — Supplier Return Delivery
- `MRPEVT` — MRP Event Monitor

**Sales & Distribution (SD)**
- `VA01` — Create Sales Order
- `VL01N` — Create Outbound Delivery
- `VL02N` — Change Outbound Delivery / PGI
- `VF01` — Create Billing Document
- `CRRETURN` — Customer Complaint & Return
- `CRCREDIT` — Customer Credit Memo

**Production Planning (PP)**
- `CO01` — Create Production Order
- `MD61` — Planned Independent Requirements
- `MD02` — Material Requirements Planning
- `CO11N` — Production Order Confirmation
- `CS01` — Create Bill of Materials
- `CR01` — Create Work Center
- `PPDS` — PP/DS Finite Scheduling

**Quality Management (QM)**
- `QE01` — Record Inspection Lot Results
- `QE51N` — Record Inspection Results
- `QM01` — Create Quality Notification
- `QM02` — Change Quality Notification
- `QM03` — Quality Notification Tasks
- `QM11` — Record Inspection Results
- `QM12` — Manage Usage Decisions
- `ZQM1` — QM Master Data Setup
- `1FM` — QM in Procurement
- `2F9` — Quality Notification - Supplier
- `1E1` — QM in Production
- `2QP` — Quality Notification - Internal
- `2QN` — Manual Inspection
- `QMM` — Recurring Batch Inspection
- `1MP` — Outbound Delivery Inspection
- `BKR` — Customer Return Inspection
- `2FA` — Quality Notification - Customer
- `CALIB` — Calibration Inspection
- `QP01` — Create Inspection Plan
- `QA01` — Create Inspection Lot
- `QN01` — Create Quality Notification
- `QA11` — Usage Decision & Stock Posting
- `QC21` — Quality Certificate (COA)
- `CRINSPECT` — Quality Inspection - Return
- `CRUDPOST` — Post Usage Decision - Return
- `CRSUPPLY` — Supplier Complaint & Claim

**Finance (FI)**
- `FB50` — Enter G/L Account Document
- `FB60` — Vendor Invoice Entry
- `FBL1N` — Vendor Line Item Display
- `F-53` — Vendor Outgoing Payment
- `F-28` — Customer Incoming Payment
- `ABZN` — Asset Acquisition
- `UNIJRN` — Universal Journal Entry
- `CONSOL` — Consolidation Workbench
- `TAXRET` — Tax Return Filing
- `CRDEBIT` — Supplier Debit Memo

**Controlling (CO)**
- `KB11N` — Cost Center Allocation
- `KO88` — Settle Production / PM Order
- `KS01` — Create Cost Center

**Human Resources (HR)**
- `PA30` — Maintain HR Master Data

**Plant Maintenance (PM)**
- `IE01` — Equipment Master Creation
- `IW21` — Create Maintenance Notification
- `IW31` — Create Maintenance Order
- `IW41` — PM Order Confirmation
- `IW32` — Change Maintenance Order
- `IL01` — Create Functional Location

**Warehouse Management (WM)**
- `RFSCAN` — RF Scanner Menu
- `RFPICK` — RF Pick Task
- `WAVEPK` — Wave Pick Management
- `VSLOTT` — Velocity Slotting

**Compliance & AI**
- `SOXADM` — SOX Compliance Administration
- `AIOCR` — Document OCR Processing

</details>

---

## 8. Personalizing Your Experience

### Change Your Theme
Click the **colored dots** in the sidebar footer:

| Dot | Theme | Style |
|-----|-------|-------|
| 🔵 | **Modern** | Clean, blue accents, rounded |
| 🟤 | **Classical** | Traditional, formal |
| ⚪ | **Minimal** | Simple, lots of white space |
| 🟣 | **Futuristic** | Dark, neon, bold |

**Dark Mode:** Click the 🌙 moon icon in the top bar.

### Change Your Language
1. Click **EN ▼** in the top bar
2. Select your language:
   - English
   - Hindi (हिन्दी)
   - Tamil (தமிழ்)
   - Telugu (తెలుగు)
   - French (Français)
   - Spanish (Español)
3. The page refreshes in your chosen language

### Adjust Settings
1. Click ⚙️ in the top bar
2. You can change:
   - **Session Timeout** — How long before auto-logout
   - **Warning Time** — Minutes before timeout to show warning
   - **Sidebar** — Collapse or expand
   - **Animations** — Enable/disable transitions

### Add Favorites
1. Navigate to any T-Code (e.g., `VA01`)
2. Click the **star** icon
3. It appears in your Favorites list for quick access

---

## 9. Tips for New Users

### Getting Started
1. **Start with the Dashboard** — See what's happening at a glance
2. **Use the Search (Ctrl+K)** — Fastest way to find anything
3. **Follow the workflows** — See Section 5 for step-by-step guides
4. **Don't be afraid to explore** — You can't break anything by looking

### Navigating
- **Click module names** in the sidebar to open them
- **Use tabs** to switch between views (Create, List, Display)
- **Sort tables** by clicking column headers
- **Filter data** using the search/filter boxes above tables
- **Export data** using the export buttons (Excel, CSV, PDF)

### Creating Records
- **Fill required fields first** — They're usually marked with *
- **Use tabs** — Forms are organized into logical sections
- **Check auto-calculations** — Totals update automatically
- **Save often** — Don't lose your work

### Keyboard Shortcuts
| Shortcut | What it does |
|----------|-------------|
| `Ctrl+K` | Open search |
| `Ctrl+P` | Print page |
| `Tab` | Move to next field |
| `Shift+Tab` | Move to previous field |
| `Enter` | Submit/Confirm |
| `Escape` | Close dialog |

### Common Mistakes to Avoid
- **Don't skip the tabs** — Important fields are in later tabs
- **Check your numbers** — Quantities and amounts matter
- **Verify before posting** — Some actions can't be undone
- **Use the right T-Code** — Each function has its own code

---

## 10. Troubleshooting & Help

### Common Issues

**"I can't log in"**
- Check your Client Number (`1000`), User ID, and Password
- Make sure Caps Lock is off
- If locked out, wait 15 minutes or contact admin

**"The page shows an error"**
- Try refreshing the page (F5)
- Check if the server is running
- Contact your system administrator

**"I can't find a module"**
- Use the search bar (Ctrl+K)
- Check if the module is enabled for your role
- Ask admin to enable it

**"My changes didn't save"**
- Check for validation errors (red messages)
- Make sure all required fields are filled
- Try again — sometimes network issues occur

**"I'm lost!"**
- Click 🏠 to go back to the Dashboard
- Use the sidebar to navigate
- Press `Ctrl+K` and search for what you need

### Getting Help

| Need | What to do |
|------|-----------|
| **System issues** | Contact your system administrator |
| **How-to questions** | Refer to this guide or ask a colleague |
| **Feature requests** | Submit via the project's GitHub page |
| **Bug reports** | Report at github.com/bhnvboy-cell/yukthira |

### Quick Reference Card

| Task | How |
|------|-----|
| Login | Enter Client + User ID + Password |
| Navigate | Click sidebar modules or use Ctrl+K search |
| Create record | Module → Create button → Fill form → Save |
| Find a T-Code | Ctrl+K → Type code → Enter |
| Change theme | Click colored dots in sidebar footer |
| Change language | Click EN ▼ in top bar |
| Print | Ctrl+P |
| Log out | Click 🚪 in top bar |

---

## Appendix: Forms Explained

Every creation form in YuktiraERP follows the same pattern:

### Tabbed Form Layout
```
┌─────────────────────────────────────────────┐
│  [Basic Data] [Purchasing] [Accounting]     │  ← Tabs
├─────────────────────────────────────────────┤
│                                             │
│  Material Code: [__________]                │
│  Description:   [__________]                │
│  Material Type: [▼ ROH    ]                │
│  Unit of Measure: [▼ KG    ]               │
│                                             │
├─────────────────────────────────────────────┤
│  Section: General Data                      │
│  ┌─────────────────────────────────────┐   │
│  │ Field 1: [__________]               │   │
│  │ Field 2: [__________]               │   │
│  │ Field 3: [▼ Option   ]              │   │
│  └─────────────────────────────────────┘   │
│                                             │
├─────────────────────────────────────────────┤
│  [Cancel]                    [Save]         │  ← Buttons
└─────────────────────────────────────────────┘
```

### Field Types
| Field Type | What it looks like | How to use |
|-----------|-------------------|------------|
| **Text** | `[__________]` | Type your answer |
| **Number** | `[__________]` | Type a number |
| **Dropdown** | `[▼ Option   ]` | Click to select |
| **Date** | `[📅 YYYY-MM-DD]` | Pick a date |
| **Checkbox** | `[ ]` | Click to check |
| **Auto-calc** | `$1,500.00` | Calculated automatically |

### Form Buttons
| Button | What it does |
|--------|-------------|
| **Save** | Save your work and close |
| **Cancel** | Discard changes and close |
| **Back** | Go back to the previous page |
| **Export** | Download as Excel/CSV/PDF |

---

*Yuktira ERP Suite — Intelligence Driven Enterprise Platform*
*Welcome to the team! 🎉*
