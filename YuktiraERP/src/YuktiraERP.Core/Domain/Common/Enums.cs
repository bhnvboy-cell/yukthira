namespace YuktiraERP.Core.Domain.Common;

public enum RecordStatus { Active, Inactive, Blocked, Suspended }
public enum DocumentStatus { Draft, Submitted, Approved, Rejected, Completed, Cancelled }
public enum ApprovalStatus { Pending, Approved, Rejected, Escalated, Cancelled }
public enum NotificationChannel { Email, SMS, InApp }
public enum UserRole { SuperUser, Admin, PowerUser, NormalUser, ReadOnly }
public enum ModuleCode { MM, SD, PP, QM, WM, FI, HR, CRM, LIMS, BI }
public enum ActionType { Create, Update, Delete, Login, Logout, Approval, Config, Workflow, Export, Print, ApiCall }
public enum WorkflowNodeType { Start, Task, Approval, Decision, Timer, ApiCall, Email, Sms, Condition, End }
