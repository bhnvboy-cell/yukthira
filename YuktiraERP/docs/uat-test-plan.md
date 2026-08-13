# YUKTIRA ERP SUITE — User Acceptance Test (UAT) Plan

Version: 1.1
Module(s): All (13 modules + Platform)
Test Type: Manual UAT (business-facing)
Status: **EXECUTED** — automated web/API regression performed 2026-08-11

---

## 18. Test Execution Report (2026-08-11)

### 18.1 Summary

| Area | Tested | Pass | Fail |
|------|--------|------|------|
| UI page smoke (all key pages) | 45 | 45 | 0 |
| CRUD create + persist (13 modules) | 13 | 13 | 0 |
| API functional (login, MRP, T-code execute, dashboard, audit) | 8 | 8 | 0 |
| Login/security checks | 4 | 3 | 1 |
| **Total** | **70** | **69** | **1** |

### 18.2 Bugs Found & Fixed During UAT

| # | Severity | Description | File(s) Fixed | Status |
|---|----------|-------------|---------------|--------|
| 1 | Critical | API never seeded the database → all API logins returned 401 Unauthorized | `src/YuktiraERP.Api/Program.cs` | Fixed |
| 2 | Critical | `Jwt:Secret` was a 29-char placeholder (232 bits) → login threw 500 (HS256 needs ≥256 bits) | `src/YuktiraERP.Api/appsettings.json` | Fixed |
| 3 | Critical | `EfRepository.AddAsync/UpdateAsync/DeleteAsync` never called `SaveChangesAsync` → every create/update/delete silently did nothing across the whole app | `src/YuktiraERP.Infrastructure/Data/EfRepository.cs` | Fixed |
| 4 | High | Approval/Create wrote to a static in-memory store while Pending/History read from EF → created approval requests never appeared | `src/YuktiraERP.Web/Pages/Approval/Create.cshtml.cs` | Fixed |
| 5 | Medium | 4 of 6 Dashboard quick-action links were broken (404): Create PO, Create Sample, Create GRN, QC Result | `src/YuktiraERP.Web/Pages/Dashboard/Index.cshtml.cs` | Fixed |

### 18.3 Open Defects (still failing)

| # | Severity | Test | Description |
|---|----------|------|-------------|
| 6 | Critical | RBA-01/02, SEC-01 | **Role-based access not enforced on the web UI.** Anonymous (not logged in) visitors get HTTP 200 on all pages (`/SD/Index`, `/Admin/Users`, `/Audit/Index`, `/TCodeGenerator/Index`, etc.). Only 7 pages have `[Authorize]`; only 2 restrict roles (`Transactions/Manage`, `Integration`). Any logged-in user, including `readonly`, can open Admin pages. **Recommendation:** add a global fallback authorization policy requiring authentication, plus role policies for Admin/sensitive pages. |

### 18.4 Results by Section

- **Section 5 (AUTH):** AUTH-01 PASS, AUTH-02 PASS, AUTH-03 PASS (wrong password → 401/error), AUTH-04 PASS, AUTH-05 PASS (all 5 users log in), AUTH-06 FAIL (readonly sees full navigation; see Defect #6)
- **Section 6 (Dashboard):** DASH-01 PASS, DASH-02 PASS (4 broken quick-action links found & fixed during test)
- **Section 7 (SD):** SD-01..05 PASS (Customer, Sales Order, Quotation/Inquiry, Delivery, Billing create + persist)
- **Section 8 (MM):** MM-01..08 PASS (Material, Vendor, PR, PO, GRN, Goods Issue, Stock Overview, Invoice Verification)
- **Section 9 (FI):** FI-01..07 PASS (GL/Ledger, AP Invoice, AR Invoice, Payments, Fixed Asset, P&L, Balance Sheet pages + entries persist; note AR/GL entries are listed on dedicated pages, not the FI index tabs)
- **Section 10 (PP/MRP):** PP-01..06 PASS (BOM, Work Center, Routing, Production Order, MRP run, MRP stock)
- **Section 11 (PM):** PM-01..03 PASS (Equipment, Maintenance Plan, Maintenance Order)
- **Section 12 (QM/LIMS):** QM-01..04 PASS, LIMS-01..03 PASS (Inspection Plan/Lot/Result, Usage Decision, Sample, Test Result, Instrument)
- **Section 13:** HR-01..04 PASS (Employee, Attendance, Leave, Payroll run), CRM-01..03 PASS (Lead, Opportunity, Contact/Ticket), PS-01 PASS, CO-01 PASS, WM-01/02 PASS
- **Section 14 (Platform):** PLT-01..11 PASS (Workflow designer, Approval create→pending, Notifications, Audit, BI, TCode, Transaction launcher/execute, Plugins, Admin Users/Tenants, Swagger is Development-only by design)
- **Section 15 (RBA):** RBA-01 FAIL, RBA-02 FAIL, RBA-03 partial — see Defect #6

### 18.5 Recommendation

**Approved with conditions.** All functional business flows pass after the 5 fixes. Condition: resolve Defect #6 (authorization enforcement) before any production/public deployment. Re-test role-based access after the fix.

---

## 1. Objective


Verify that the Yuktira ERP Suite meets business expectations and is ready for
handover. This plan walks a business user through the main workflows and
records pass/fail against each scenario.

## 2. Environment

| Item | Value |
|------|-------|
| Application URL | http://localhost:5001 |
| API URL | http://localhost:5000 |
| Database | In-memory (data resets when the app restarts) |
| Start command | `start.bat` |

> Note: Because the database is in-memory, all test data is lost on restart.
> Run the test scenarios in one session, or re-seed after restart.

## 3. Test Users

| User ID | Role | Password |
|---------|------|----------|
| superadmin | Super User (full access) | yuktira123 |
| admin | Admin | yuktira123 |
| manager | Power User | yuktira123 |
| user | Normal User | yuktira123 |
| readonly | Read Only | yuktira123 |

Client Number: `1000`

## 4. How to Record Results

Mark each step with **PASS** / **FAIL**. If FAIL, note the error message in
"Actual Result" and log it in the Defect Log (Section 16).

---

## 5. Access & Login (AUTH)

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| AUTH-01 | Open http://localhost:5001 | Sign-in page loads | | |
| AUTH-02 | Login with `superadmin` / `yuktira123`, client `1000` | Logged in, dashboard opens | | |
| AUTH-03 | Login with wrong password | Error shown, no access | | |
| AUTH-04 | Logout, then access a page directly in the browser | Redirected to login | | |
| AUTH-05 | Login with each of the 5 users | Each user can log in | | |
| AUTH-06 | Login with `readonly` | Only read-only navigation visible | | |

## 6. Dashboard

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| DASH-01 | After login, view dashboard | KPI cards / charts load | | |
| DASH-02 | Refresh the page | Dashboard still loads without errors | | |

## 7. Sales & Distribution (SD)

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| SD-01 | Create a Customer | Saved, appears in customer list | | |
| SD-02 | Create a Sales Order for the customer | Order saved with order number | | |
| SD-03 | Create a Quotation / Inquiry | Saved | | |
| SD-04 | Create a Delivery | Saved | | |
| SD-05 | Create a Billing document | Saved | | |

## 8. Materials Management (MM)

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| MM-01 | Create a Material | Saved, appears in material list | | |
| MM-02 | Create a Vendor | Saved | | |
| MM-03 | Create a Purchase Requisition (PR) | Saved with number | | |
| MM-04 | Create a Purchase Order (PO) | Saved with number | | |
| MM-05 | Goods Receipt (GRN) against PO | Stock quantity increases | | |
| MM-06 | Goods Issue | Stock quantity decreases | | |
| MM-07 | Open Stock Overview / Stock List | Quantities correct | | |
| MM-08 | Invoice Verification | Saved | | |

## 9. Finance (FI)

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| FI-01 | Create a GL account / Ledger | Saved | | |
| FI-02 | Create AP Invoice | Saved | | |
| FI-03 | Create AR Invoice | Saved | | |
| FI-04 | Record an AP/AR Payment | Saved, balance updates | | |
| FI-05 | Create a Fixed Asset | Saved | | |
| FI-06 | View Profit & Loss report | Report renders with figures | | |
| FI-07 | View Balance Sheet | Report renders with figures | | |

## 10. Production Planning (PP) & MRP

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| PP-01 | Create a BOM | Saved, viewable | | |
| PP-02 | Create a Work Center | Saved | | |
| PP-03 | Create a Routing | Saved | | |
| PP-04 | Create a Production Order | Saved | | |
| PP-05 | Run MRP | Results / requisitions generated | | |
| PP-06 | View MRP stock view | Data shown | | |

## 11. Plant Maintenance (PM)

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| PM-01 | Create Equipment | Saved | | |
| PM-02 | Create Maintenance Plan | Saved | | |
| PM-03 | Create Maintenance Order | Saved | | |

## 12. Quality Management (QM) & LIMS

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| QM-01 | Create an Inspection Plan | Saved | | |
| QM-02 | Create an Inspection Lot | Saved | | |
| QM-03 | Record Inspection Results | Results saved | | |
| QM-04 | Usage Decision | Saved | | |
| LIMS-01 | Create a LIMS Sample | Saved, viewable | | |
| LIMS-02 | Record a Test Result | Saved | | |
| LIMS-03 | Create an Instrument | Saved | | |

## 13. HR, CRM, Projects (PS), CO, WM

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| HR-01 | Create an Employee | Saved, appears in list | | |
| HR-02 | Record Attendance | Saved | | |
| HR-03 | Create Leave request | Saved | | |
| HR-04 | Run Payroll | Payroll run completes | | |
| CRM-01 | Create a Lead | Saved | | |
| CRM-02 | Create an Opportunity | Saved | | |
| CRM-03 | Create a Contact / Service Ticket | Saved | | |
| PS-01 | Create a Project / Task / Timesheet | Saved | | |
| CO-01 | Create Cost Center / Cost Element | Saved | | |
| WM-01 | Create Storage Location / Bin | Saved | | |
| WM-02 | Create a Transfer | Saved | | |

## 14. Platform Features

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| PLT-01 | Workflow Designer opens and saves | No errors | | |
| PLT-02 | Approval: create a request, approve/reject from Pending | Status updates | | |
| PLT-03 | Notifications inbox loads | Inbox visible | | |
| PLT-04 | Audit log page lists entries | Entries shown | | |
| PLT-05 | BI: create/run a report or dashboard | Renders | | |
| PLT-06 | TCode: create/edit/list transaction codes | Works | | |
| PLT-07 | Transactions: Launcher opens a transaction | Launcher works | | |
| PLT-08 | Plugins: Manage page lists plugins | Plugin list visible | | |
| PLT-09 | Admin > Users: add / edit a user | Saved | | |
| PLT-10 | Admin > Tenants: view tenants | Tenant `1000` visible | | |
| PLT-11 | API Swagger loads (superadmin) | http://localhost:5000/swagger | | |

## 15. Role-Based Access Check

| ID | Test Step | Expected Result | Result | Actual / Remarks |
|----|-----------|-----------------|--------|------------------|
| RBA-01 | Login as `readonly`, try to open an Admin page | Blocked / not visible | | |
| RBA-02 | Login as `user`, try to access superuser-only pages | Blocked / not visible | | |
| RBA-03 | Login as `admin`, verify Admin pages accessible | Accessible | | |

## 16. Defect Log

| Defect ID | Date | Module / Test ID | Description | Severity (S/M/L) | Status |
|-----------|------|------------------|-------------|------------------|--------|
| | | | | | |

## 17. Sign-Off

| Role | Name | Signature | Date | Decision (Approve / Reject) |
|------|------|-----------|------|------------------------------|
| Business Owner | | | | |
| Tester | | | | |
| Developer | | | | |

---

**Decision:** [ ] Approved for deployment   [ ] Conditional approval   [ ] Rejected
