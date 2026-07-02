# YUKTIRA ERP SUITE — API Reference

## Base URL

| Environment | URL |
|-------------|-----|
| Development | `http://localhost:5000/api` |
| Production  | `https://erp.yourcompany.com/api` |

## Authentication

All endpoints (except login and refresh-token) require a Bearer JWT token in the `Authorization` header:

```
Authorization: Bearer <access-token>
```

Obtain a token via `POST /api/auth/login`.

---

## Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/auth/login` | Authenticate user (returns access + refresh token) |
| POST | `/auth/logout` | Invalidate session & refresh token |
| POST | `/auth/refresh-token` | Rotate refresh token |
| GET | `/auth/user-profile` | Get current user profile |
| GET | `/auth/my-permissions` | Get current user permissions |

---

## Security

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/security/my-permissions` | Get current user's effective permissions |
| GET | `/security/permission-matrix` | Full permission matrix (Admin+) |
| GET | `/security/suspicious-activity` | Paginated flagged suspicious entries |
| POST | `/security/suspicious-activity/detect` | Run suspicious activity detection engine |
| GET | `/security/compliance/audit-log` | Query audit log with filters (module, date range) |
| POST | `/security/unlock-user/{userId}` | Unlock a locked user account (Super/Admin) |

---

## Materials Management (MM)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/mm/material` | List materials |
| GET | `/mm/material/{id}` | Get material by ID |
| POST | `/mm/material` | Create material |
| PUT | `/mm/material/{id}` | Update material |
| DELETE | `/mm/material/{id}` | Delete material |
| GET | `/mm/vendor` | List vendors |
| POST | `/mm/vendor` | Create vendor |
| GET | `/mm/purchase-requisition` | List purchase requisitions |
| POST | `/mm/purchase-order` | Create purchase order |
| GET | `/mm/purchase-order` | List purchase orders |
| GET | `/mm/goods-receipt` | List goods receipts |
| POST | `/mm/goods-receipt` | Post goods receipt |
| GET | `/mm/stock` | Get stock overview |
| GET | `/mm/invoice-verification` | List invoice verifications |
| POST | `/mm/invoice-verification` | Create invoice verification |

---

## Sales & Distribution (SD)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/sd/customer` | List customers |
| POST | `/sd/customer` | Create customer |
| GET | `/sd/sales-order` | List sales orders |
| POST | `/sd/sales-order` | Create sales order |
| PUT | `/sd/sales-order/{id}` | Change sales order |
| GET | `/sd/sales-order/{id}` | Display sales order |
| GET | `/sd/delivery` | List deliveries |
| POST | `/sd/delivery` | Create delivery |
| GET | `/sd/billing` | List billing documents |
| POST | `/sd/billing` | Create billing document |

---

## Production Planning (PP)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/pp/work-center` | List work centers |
| POST | `/pp/work-center` | Create work center |
| GET | `/pp/bom` | List BOMs |
| POST | `/pp/bom` | Create BOM |
| GET | `/pp/routing` | List routings |
| POST | `/pp/routing` | Create routing |
| GET | `/pp/planned-order` | List planned orders |
| POST | `/pp/planned-order` | Create planned order |
| GET | `/pp/production-order` | List production orders |
| POST | `/pp/production-order` | Release production order |
| POST | `/pp/production-confirmation` | Post production confirmation |

---

## Quality Management (QM)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/qm/inspection-plan` | List inspection plans |
| POST | `/qm/inspection-plan` | Create inspection plan |
| GET | `/qm/inspection-lot` | List inspection lots |
| POST | `/qm/inspection-lot` | Create inspection lot |
| GET | `/qm/inspection-result` | List inspection results |
| POST | `/qm/inspection-result` | Record inspection result |
| POST | `/qm/usage-decision` | Post usage decision (accept/reject/scrap/rework) |

---

## Warehouse (WM)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/wm/storage-location` | List storage locations |
| POST | `/wm/storage-location` | Create storage location |
| GET | `/wm/transfer` | List warehouse transfers |
| POST | `/wm/transfer` | Create warehouse transfer |

---

## Finance (FI)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/fi/account` | List accounts |
| POST | `/fi/account` | Create account |
| GET | `/fi/journal-entry` | List journal entries |
| POST | `/fi/journal-entry` | Post journal entry |
| GET | `/fi/trial-balance` | Get trial balance |
| GET | `/fi/profit-loss` | Get profit & loss statement |
| GET | `/fi/balance-sheet` | Get balance sheet |
| GET | `/fi/ap-aging` | AP aging report |
| GET | `/fi/ar-aging` | AR aging report |

---

## Human Resources (HR)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/hr/employee` | List employees |
| POST | `/hr/employee` | Create employee |
| GET | `/hr/leave-request` | List leave requests |
| POST | `/hr/leave-request` | Submit leave request |
| GET | `/hr/payroll` | List payroll entries |
| POST | `/hr/payroll/run` | Run payroll calculation |
| GET | `/hr/attendance` | List attendance records |
| POST | `/hr/attendance` | Record attendance |

---

## CRM

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/crm/lead` | List leads |
| POST | `/crm/lead` | Create lead |
| GET | `/crm/opportunity` | List opportunities |
| POST | `/crm/opportunity` | Create opportunity |
| GET | `/crm/contact` | List contacts |
| POST | `/crm/contact` | Create contact |
| GET | `/crm/campaign` | List campaigns |
| POST | `/crm/campaign` | Create campaign |
| GET | `/crm/service-ticket` | List service tickets |
| POST | `/crm/service-ticket` | Create service ticket |

---

## MRP

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/mrp/run` | Run MRP calculation |
| POST | `/mrp/run-multi-plant` | Run MRP with multi-plant scope |
| POST | `/mrp/run-with-vendor-lt` | Run MRP with vendor lead-time integration |
| POST | `/mrp/capacity-leveling` | Calculate capacity leveling |
| GET | `/mrp/history` | Get MRP run history |
| GET | `/mrp/exceptions` | Get MRP exception messages |
| GET | `/mrp/shortages` | Get shortage alerts |

---

## AI Engine

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/ai/forecast` | Run forecasting (generic) |
| POST | `/ai/holt-winters` | Holt-Winters triple exponential smoothing |
| POST | `/ai/arima` | ARIMA forecast |
| POST | `/ai/anomalies` | Anomaly detection (ZScore/IQR/MAD) |
| GET | `/ai/forecast-dashboard/{materialId}` | Combined demand + safety stock + anomalies |
| GET | `/ai/demand-prediction/{materialId}` | Demand prediction |
| GET | `/ai/stock-prediction/{materialId}` | Stock prediction |

---

## Workflow

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/workflow/{workflowId}/start` | Start workflow instance |
| GET | `/workflow/{workflowId}/nodes` | Get workflow nodes |
| POST | `/workflow/{workflowId}/validate` | Validate workflow definition |
| POST | `/workflow/{workflowId}/simulate` | Simulate workflow execution |
| POST | `/workflow/instance/{instanceId}/approve` | Approve current node |
| POST | `/workflow/instance/{instanceId}/reject` | Reject current node |
| POST | `/workflow/instances/{id}/process/{nodeId}` | Process workflow node |
| POST | `/workflow/instances/{id}/complete` | Complete workflow |
| POST | `/workflow/instances/{id}/terminate` | Terminate workflow |
| POST | `/workflow/instances/{id}/timer` | Schedule timer node |
| POST | `/workflow/condition/evaluate` | Evaluate condition expression |

---

## Plugins

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/plugins` | List all plugins |
| GET | `/plugins/{pluginId}/settings` | Get plugin settings |
| POST | `/plugins/{pluginId}/settings` | Update plugin settings |
| GET | `/plugins/{pluginId}/permissions` | Get plugin permissions |
| GET | `/plugins/{pluginId}/status` | Get plugin status (memory, execution) |
| POST | `/plugins/{pluginId}/reload` | Hot-reload plugin |
| POST | `/plugins/{code}/install` | Install plugin |
| DELETE | `/plugins/{id}/uninstall` | Uninstall plugin |
| POST | `/plugins/{id}/enable` | Enable for tenant |
| POST | `/plugins/{id}/disable` | Disable for tenant |

---

## BI & KPI

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/bi/reports` | List BI reports |
| POST | `/bi/reports` | Create BI report |
| GET | `/bi/reports/{id}/run` | Execute report |
| GET | `/bi/kpis` | Get available KPIs |
| GET | `/bi/kpis/{code}/calculate` | Calculate specific KPI |
| GET | `/bi/dashboards` | List dashboards |
| POST | `/bi/dashboards` | Create dashboard |
| POST | `/bi/dashboards/{id}/layout` | Save widget layout |

---

## Integration Queue

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/integration/queue/enqueue` | Enqueue outbound message |
| GET | `/integration/queue/pending` | View pending messages |
| POST | `/integration/queue/process` | Process pending messages |
| GET | `/integration/queue/dead-letter` | View dead-letter queue |
| POST | `/integration/queue/requeue/{id}` | Requeue from dead-letter |

---

## Webhooks

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/integration/webhooks` | Register webhook |
| GET | `/integration/webhooks` | List webhooks |
| DELETE | `/integration/webhooks/{id}` | Delete webhook |
| POST | `/integration/dispatch` | Dispatch event to matching webhooks |

---

## Approvals

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/approvals/pending` | Get pending approvals |
| GET | `/approvals/{id}` | Get approval by ID |
| POST | `/approvals/{id}/approve` | Approve request |
| POST | `/approvals/{id}/reject` | Reject request |
| POST | `/approvals/{id}/escalate` | Escalate request |
| POST | `/approvals` | Create approval request |

---

## Notifications

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/notifications` | Get user notifications |
| GET | `/notifications/unread-count` | Get unread count |
| PUT | `/notifications/{id}/read` | Mark as read |
| PUT | `/notifications/read-all` | Mark all as read |
| POST | `/notifications/send` | Send notification (admin) |

---

## Audit

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/audit` | Get audit logs (filterable) |
| GET | `/audit/{id}` | Get audit log by ID |
| GET | `/audit/count` | Get log count |
| POST | `/audit/{id}/flag-suspicious` | Flag entry as suspicious |

---

## Number Ranges

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/numberranges/next/{module}/{prefix}` | Get next number in sequence |
| GET | `/numberranges/current/{module}/{prefix}` | Get current number |
| POST | `/numberranges/reset/{module}/{prefix}` | Reset number range |

---

## Export

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/export/grid` | Export grid data (XLSX/CSV/PDF/HTML) |
| POST | `/export/document` | Generate document (PO, SO, INVOICE, etc.) |

---

## Customization

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/customization/{screenName}` | Get screen layout |
| POST | `/customization/{screenName}` | Save screen layout |
| DELETE | `/customization/{screenName}/reset` | Reset to default |
| POST | `/customization/{screenName}/columns` | Add custom column |
| DELETE | `/customization/{screenName}/columns/{name}` | Remove custom column |

---

## Dashboard

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/dashboard` | Get user dashboard |
| POST | `/dashboard/layout` | Save widget layout |
| GET | `/dashboard/available` | Get available widgets |

---

## Super User

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/superuser/unlock-document/{id}` | Unlock document |
| POST | `/superuser/reset-password/{userId}` | Reset user password |
| POST | `/superuser/impersonate/{userId}` | Impersonate user |
| GET | `/superuser/audit-logs/summary` | Audit summary |
| POST | `/superuser/tenants/{id}/toggle-module/{code}` | Toggle module for tenant |

---

## Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Liveness probe |
| GET | `/health/ready` | Readiness probe (includes DB ping) |

---

## Real-Time (SignalR)

| Hub | Endpoint | Events |
|-----|----------|--------|
| NotificationHub | `/hubs/notifications` | `WorkflowUpdate`, `MrpProgress`, `DashboardRefresh`, `Notification` |

### JavaScript Client

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications", { accessTokenFactory: () => token })
  .build();

connection.on("WorkflowUpdate", (instanceId, status, message) => { ... });
connection.on("MrpProgress", (percentage, message) => { ... });
connection.on("DashboardRefresh", () => { ... });
connection.on("Notification", (title, message) => { ... });

await connection.start();
```
