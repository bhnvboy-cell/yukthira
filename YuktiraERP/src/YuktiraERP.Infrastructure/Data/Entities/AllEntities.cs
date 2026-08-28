using System.ComponentModel.DataAnnotations;
namespace YuktiraERP.Infrastructure.Data.Entities;

public abstract class EntityBase { [Key] public Guid Id { get; set; } = Guid.NewGuid(); public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? UpdatedAt { get; set; } }

// MM
public class MaterialMasterEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "RAW"; public string UOM { get; set; } = "EA"; public decimal Stock { get; set; } public decimal Price { get; set; } public string Status { get; set; } = "Active"; }
public class VendorEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string TaxId { get; set; } = ""; public string PaymentTerms { get; set; } = "Net 30"; public string Phone { get; set; } = ""; public string Status { get; set; } = "Active"; }
public partial class PurchaseOrderEntity : EntityBase { public Guid TenantId { get; set; } public string PoNumber { get; set; } = ""; public DateTime Date { get; set; } public string VendorName { get; set; } = ""; public string VendorCode { get; set; } = ""; public string ItemName { get; set; } = ""; public string Quantity { get; set; } = ""; public decimal Amount { get; set; } public string Status { get; set; } = "Pending"; public string DepartmentKey { get; set; } = ""; public string CostCenter { get; set; } = ""; public decimal TotalAmount { get; set; } public int ItemCount { get; set; } public string PaymentTerms { get; set; } = "Net 30"; public string Incoterms { get; set; } = ""; public string ReleaseStatus { get; set; } = ""; public List<PurchaseOrderItemEntity> Items { get; set; } = new(); }
public partial class PurchaseRequisitionEntity : EntityBase { public Guid TenantId { get; set; } public string PrNumber { get; set; } = ""; public DateTime Date { get; set; } public string Requestor { get; set; } = ""; public string ItemName { get; set; } = ""; public string Quantity { get; set; } = ""; public decimal Amount { get; set; } public string Status { get; set; } = "Pending"; public string DepartmentKey { get; set; } = ""; public string CostCenter { get; set; } = ""; public decimal TotalAmount { get; set; } public int ItemCount { get; set; } public string ReleaseStatus { get; set; } = ""; public string ConvertedPoNumber { get; set; } = ""; public List<PurchaseRequisitionItemEntity> Items { get; set; } = new(); }
public class GoodsReceiptEntity : EntityBase { public Guid TenantId { get; set; } public string GrnNumber { get; set; } = ""; public DateTime Date { get; set; } public string PoNumber { get; set; } = ""; public string MaterialName { get; set; } = ""; public string QtyReceived { get; set; } = ""; public string QtyAccepted { get; set; } = ""; public string Status { get; set; } = "Pending"; }
public class StockItemEntity : EntityBase { public Guid TenantId { get; set; } public string Bin { get; set; } = ""; public string MaterialName { get; set; } = ""; public string Lot { get; set; } = ""; public decimal Quantity { get; set; } public string UOM { get; set; } = "EA"; public decimal Value { get; set; } public decimal MinStock { get; set; } public decimal MaxStock { get; set; } }
public class InvoiceVerificationEntity : EntityBase { public string InvoiceNumber { get; set; } = ""; public DateTime Date { get; set; } public string PoNumber { get; set; } = ""; public string VendorName { get; set; } = ""; public decimal Amount { get; set; } public decimal MatchedAmount { get; set; } public string Status { get; set; } = "Pending"; public Guid TenantId { get; set; } }

// SD
public class CustomerEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public decimal CreditLimit { get; set; } public string PaymentTerms { get; set; } = "Net 30"; public string Phone { get; set; } = ""; public string Status { get; set; } = "Active"; }
public class SalesOrderEntity : EntityBase { public string OrderNumber { get; set; } = ""; public string CustomerName { get; set; } = ""; public DateTime OrderDate { get; set; } public int ItemCount { get; set; } public decimal Amount { get; set; } public string Status { get; set; } = "Pending"; public List<SalesOrderLineEntity> Lines { get; set; } = new(); }
public partial class SalesOrderLineEntity : EntityBase { public Guid SalesOrderId { get; set; } public string MaterialName { get; set; } = ""; public decimal Quantity { get; set; } public string UOM { get; set; } = "EA"; public decimal UnitPrice { get; set; } public decimal TotalPrice { get; set; } }
public class InquiryEntity : EntityBase { public string InquiryNumber { get; set; } = ""; public DateTime Date { get; set; } public string CustomerName { get; set; } = ""; public string Description { get; set; } = ""; public string Status { get; set; } = "Open"; }
public class QuotationEntity : EntityBase { public string QuoteNumber { get; set; } = ""; public DateTime Date { get; set; } public string CustomerName { get; set; } = ""; public decimal Amount { get; set; } public DateTime ValidUntil { get; set; } public string Status { get; set; } = "Draft"; }
public partial class DeliveryEntity : EntityBase { public string DeliveryNumber { get; set; } = ""; public DateTime Date { get; set; } public string SoNumber { get; set; } = ""; public string CustomerName { get; set; } = ""; public string Status { get; set; } = "Picked"; }
public class BillingDocumentEntity : EntityBase { public Guid TenantId { get; set; } public string DocumentNumber { get; set; } = ""; public DateTime Date { get; set; } public string SoNumber { get; set; } = ""; public string CustomerName { get; set; } = ""; public decimal Amount { get; set; } public string Status { get; set; } = "Unpaid"; }

// PP
public class ProductionPlanEntity : EntityBase { public Guid TenantId { get; set; } public string PlanId { get; set; } = ""; public string ProductName { get; set; } = ""; public decimal Quantity { get; set; } public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public string Status { get; set; } = "Planned"; }
public class BillOfMaterialEntity : EntityBase { public Guid TenantId { get; set; } public string BomId { get; set; } = ""; public string ProductName { get; set; } = ""; public string ComponentName { get; set; } = ""; public decimal Quantity { get; set; } public string UOM { get; set; } = "EA"; public string Status { get; set; } = "Active"; }
public class ProductionRoutingEntity : EntityBase { public Guid TenantId { get; set; } public string RoutingId { get; set; } = ""; public string ProductName { get; set; } = ""; public int OperationNo { get; set; } public string WorkCenter { get; set; } = ""; public decimal SetupTimeHrs { get; set; } public decimal RunTimeHrs { get; set; } public string Status { get; set; } = "Active"; }
public class WorkCenterEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Department { get; set; } = ""; public decimal CapacityPerShift { get; set; } public string Status { get; set; } = "Active"; }
public class ProductionOrderEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "PLANNED";
    public Guid? BOMId { get; set; }
    public Guid? RoutingId { get; set; }
    public string? BatchNo { get; set; }
    public decimal ScrapQty { get; set; } = 0;
    public decimal YieldQty { get; set; } = 0;
    public decimal ActualCost { get; set; } = 0;
    public decimal PlannedCost { get; set; } = 0;
    public DateTime? ReleasedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? TecodAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? ReleaseBy { get; set; }
    public string? ConfirmBy { get; set; }

    private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
    {
        ["PLANNED"] = new() { "RELEASED", "CANCELLED" },
        ["RELEASED"] = new() { "IN_PROGRESS", "CANCELLED" },
        ["IN_PROGRESS"] = new() { "COMPLETED", "CANCELLED" },
        ["COMPLETED"] = new() { "TECO" },
        ["TECO"] = new(),
        ["CANCELLED"] = new()
    };

    public bool CanTransitionTo(string newStatus)
    {
        if (!ValidTransitions.TryGetValue(Status, out var allowed))
            return false;
        return allowed.Contains(newStatus);
    }

    public void TransitionTo(string newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Invalid transition from {Status} to {newStatus}");
        Status = newStatus;
    }
}

public class ProductionOrderItemEntity : EntityBase
{
    public Guid ProductionOrderId { get; set; }
    public string MaterialName { get; set; } = "";
    public decimal RequiredQty { get; set; }
    public decimal IssuedQty { get; set; } = 0;
    public decimal ScrapQty { get; set; } = 0;
    public string UOM { get; set; } = "EA";
    public string Status { get; set; } = "PLANNED";
}

public class MaterialStagingEntity : EntityBase
{
    public Guid ProductionOrderId { get; set; }
    public string MaterialName { get; set; } = "";
    public decimal RequiredQty { get; set; }
    public decimal StagedQty { get; set; } = 0;
    public string Status { get; set; } = "PENDING";
    public string? Notes { get; set; }
}

// QM
public class InspectionLotEntity : EntityBase { public string LotNumber { get; set; } = ""; public string MaterialName { get; set; } = ""; public string Quantity { get; set; } = ""; public int Inspected { get; set; } public int Passed { get; set; } public int Failed { get; set; } public string Status { get; set; } = "Pending"; }
public class InspectionPlanEntity : EntityBase { public string PlanId { get; set; } = ""; public string MaterialName { get; set; } = ""; public string Characteristic { get; set; } = ""; public string Method { get; set; } = ""; public string Frequency { get; set; } = ""; public string Status { get; set; } = "Active"; }
public class InspectionResultEntity : EntityBase { public string ResultId { get; set; } = ""; public string LotNumber { get; set; } = ""; public string Characteristic { get; set; } = ""; public string Result { get; set; } = ""; public string Specification { get; set; } = ""; public string Status { get; set; } = "Passed"; }
public class UsageDecisionEntity : EntityBase { public string DecisionId { get; set; } = ""; public string LotNumber { get; set; } = ""; public string MaterialName { get; set; } = ""; public string Decision { get; set; } = "Accept"; public string Notes { get; set; } = ""; public DateTime DecisionDate { get; set; } }

public class QualityNotificationEntity : EntityBase
{
    public string NotificationNumber { get; set; } = "";
    public string NotificationType { get; set; } = "Q1";
    public string Description { get; set; } = "";
    public string LongText { get; set; } = "";
    public string Plant { get; set; } = "";
    public string ReferenceDocument { get; set; } = "";
    public string ReferenceDocType { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Batch { get; set; } = "";
    public string PartnerId { get; set; } = "";
    public string PartnerName { get; set; } = "";
    public string SubjectCoding { get; set; } = "";
    public string DefectLocation { get; set; } = "";
    public string DefectCode { get; set; } = "";
    public string DefectType { get; set; } = "";
    public string CauseCode { get; set; } = "";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "NEW";
    public string CreatedBy { get; set; } = "";
    public DateTime? CompletedAt { get; set; }
}

public class QualityNotificationTaskEntity : EntityBase
{
    public Guid NotificationId { get; set; }
    public string TaskNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public string UserResponsible { get; set; } = "";
    public string CompletionText { get; set; } = "";
    public string Status { get; set; } = "OPEN";
    public DateTime? CompletedAt { get; set; }
}

public class InspectionResultDetailEntity : EntityBase
{
    public string LotNumber { get; set; } = "";
    public string Plant { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string InspectionLotOrigin { get; set; } = "";
    public string ReportType { get; set; } = "";
    public string DefectCodeGroup { get; set; } = "";
    public string DefectCode { get; set; } = "";
    public string DefectDescription { get; set; } = "";
    public string DefectCategory { get; set; } = "";
    public int Quantity { get; set; }
    public int DefectiveQuantity { get; set; }
    public string ResultStatus { get; set; } = "RECORDED";
    public string RecordedBy { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public class UsageDecisionDetailEntity : EntityBase
{
    public string LotNumber { get; set; } = "";
    public string Plant { get; set; } = "";
    public string InspectionLotOrigin { get; set; } = "";
    public string ResultRecordingStatus { get; set; } = "";
    public string UDCode { get; set; } = "";
    public string UDDescription { get; set; } = "";
    public string StockProposal { get; set; } = "";
    public string CertificateReceived { get; set; } = "No";
    public string CertificateNumber { get; set; } = "";
    public DateTime? CertificateDate { get; set; }
    public string Status { get; set; } = "OPEN";
    public string DecidedBy { get; set; } = "";
    public DateTime? DecisionDate { get; set; }
}

public class QMMasterDataEntity : EntityBase
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string InspectionType { get; set; } = "";
    public string InspectionLotOrigin { get; set; } = "";
    public string InspectionScope { get; set; } = "";
    public string InspectionProcedure { get; set; } = "";
    public string SampleProcedure { get; set; } = "";
    public string DynModificationKey { get; set; } = "";
    public string QMControlKey { get; set; } = "";
    public string CatalogType { get; set; } = "";
    public string DefectCatalog { get; set; } = "";
    public string DefectCodeGroup { get; set; } = "";
    public string UDCatalog { get; set; } = "";
    public string UDCodeGroup { get; set; } = "";
    public int Frequency { get; set; }
    public string FrequencyUnit { get; set; } = "Days";
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = "";
}

public class QMInspectionConfigEntity : EntityBase
{
    public string ConfigName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string InspectionType { get; set; } = "";
    public string MaterialGroup { get; set; } = "";
    public string VendorCode { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string POReference { get; set; } = "";
    public string DeliveryReference { get; set; } = "";
    public string ProductionOrderReference { get; set; } = "";
    public string SampleSize { get; set; } = "";
    public string InspectionLevel { get; set; } = "";
    public string Status { get; set; } = "ACTIVE";
    public string CreatedBy { get; set; } = "";
}

// WM
public class WarehouseTransferEntity : EntityBase { public string TransferId { get; set; } = ""; public DateTime Date { get; set; } public string MaterialName { get; set; } = ""; public string FromBin { get; set; } = ""; public string ToBin { get; set; } = ""; public decimal Quantity { get; set; } public string Status { get; set; } = "Pending"; }
public class StorageLocationEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "General"; public decimal Capacity { get; set; } public string Status { get; set; } = "Active"; }

// FI
public class JournalEntryEntity : EntityBase { public string DocumentNumber { get; set; } = ""; public DateTime EntryDate { get; set; } public string Account { get; set; } = ""; public decimal? Debit { get; set; } public decimal? Credit { get; set; } public string Reference { get; set; } = ""; }
public class APEntryEntity : EntityBase { public Guid TenantId { get; set; } public string DocumentNumber { get; set; } = ""; public DateTime Date { get; set; } public string VendorName { get; set; } = ""; public decimal Amount { get; set; } public decimal PaidAmount { get; set; } public string Status { get; set; } = "Open"; }
public partial class AREntryEntity : EntityBase { public Guid TenantId { get; set; } public string DocumentNumber { get; set; } = ""; public DateTime Date { get; set; } public string CustomerName { get; set; } = ""; public decimal Amount { get; set; } public decimal ReceivedAmount { get; set; } public string Status { get; set; } = "Open"; }
public partial class FixedAssetEntity : EntityBase { public string AssetCode { get; set; } = ""; public string AssetName { get; set; } = ""; public string Category { get; set; } = "Equipment"; public DateTime PurchaseDate { get; set; } public decimal Cost { get; set; } public decimal SalvageValue { get; set; } public int UsefulLifeYears { get; set; } public string Status { get; set; } = "Active"; }

// HR
public class EmployeeEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Department { get; set; } = ""; public string Designation { get; set; } = ""; public string Mobile { get; set; } = ""; public string Status { get; set; } = "Active"; }
public class LeaveRequestEntity : EntityBase { public string LeaveId { get; set; } = ""; public string EmployeeName { get; set; } = ""; public string LeaveType { get; set; } = ""; public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public string Status { get; set; } = "Pending"; }
public class PayrollEntryEntity : EntityBase { public Guid TenantId { get; set; } public string PayrollId { get; set; } = ""; public string EmployeeName { get; set; } = ""; public string Period { get; set; } = ""; public decimal GrossPay { get; set; } public decimal Deductions { get; set; } public decimal NetPay { get; set; } public string Status { get; set; } = "Draft"; }
public class AttendanceEntity : EntityBase { public string AttendanceId { get; set; } = ""; public string EmployeeCode { get; set; } = ""; public string EmployeeName { get; set; } = ""; public DateTime Date { get; set; } public string Status { get; set; } = "Present"; }
public class AppraisalEntity : EntityBase { public string AppraisalId { get; set; } = ""; public string EmployeeCode { get; set; } = ""; public string EmployeeName { get; set; } = ""; public string Period { get; set; } = ""; public int Rating { get; set; } public string Comments { get; set; } = ""; public string Status { get; set; } = "Pending"; }

// CRM
public class LeadEntity : EntityBase { public string LeadId { get; set; } = ""; public string Company { get; set; } = ""; public string Contact { get; set; } = ""; public string Source { get; set; } = ""; public decimal Value { get; set; } public string Status { get; set; } = "New"; }
public class OpportunityEntity : EntityBase { public string OppId { get; set; } = ""; public string OpportunityName { get; set; } = ""; public string Company { get; set; } = ""; public decimal Value { get; set; } public string Stage { get; set; } = ""; public string Status { get; set; } = "Open"; }
public class ContactEntity : EntityBase { public string ContactId { get; set; } = ""; public string Name { get; set; } = ""; public string Email { get; set; } = ""; public string Phone { get; set; } = ""; public string Company { get; set; } = ""; public string Status { get; set; } = "Active"; }
public class CampaignEntity : EntityBase { public string CampaignId { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "Email"; public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public decimal Budget { get; set; } public string Status { get; set; } = "Draft"; }
public class ServiceTicketEntity : EntityBase { public string TicketId { get; set; } = ""; public string CustomerName { get; set; } = ""; public string Subject { get; set; } = ""; public string Priority { get; set; } = "Medium"; public string Status { get; set; } = "Open"; public DateTime CreatedDate { get; set; } }

// LIMS
public class SampleEntity : EntityBase { public string SampleId { get; set; } = ""; public string MaterialName { get; set; } = ""; public string Source { get; set; } = ""; public DateTime CollectionDate { get; set; } public int TestCount { get; set; } public string Status { get; set; } = "Submitted"; }
public class TestResultEntity : EntityBase { public string ResultId { get; set; } = ""; public string SampleId { get; set; } = ""; public string TestName { get; set; } = ""; public string Result { get; set; } = ""; public string Specification { get; set; } = ""; public string Status { get; set; } = "Passed"; }
public class SpecificationEntity : EntityBase { public string SpecId { get; set; } = ""; public string MaterialName { get; set; } = ""; public string Characteristic { get; set; } = ""; public string MinValue { get; set; } = ""; public string MaxValue { get; set; } = ""; public string Status { get; set; } = "Active"; }
public class InstrumentEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "Analytical"; public DateTime LastCalibration { get; set; } public DateTime NextCalibration { get; set; } public string Status { get; set; } = "Operational"; }

// BI
public class BIReportEntity : EntityBase { public string ReportId { get; set; } = ""; public string ReportName { get; set; } = ""; public string Category { get; set; } = ""; public string Format { get; set; } = "PDF"; public string Query { get; set; } = ""; public string ChartType { get; set; } = "bar"; public string FilterJson { get; set; } = "{}"; public DateTime LastRun { get; set; } public string CreatedBy { get; set; } = ""; }
public class DashboardEntity : EntityBase { public string DashboardId { get; set; } = ""; public string Name { get; set; } = ""; public string Category { get; set; } = "Sales"; public Guid? UserId { get; set; } public Guid? TenantId { get; set; } public string ConfigJson { get; set; } = "{}"; public string Status { get; set; } = "Active"; }
public class KpiSnapshotEntity : EntityBase { public Guid TenantId { get; set; } public string KpiCode { get; set; } = ""; public decimal Value { get; set; } public DateTime SnapshotAt { get; set; } }

// CO - Controlling
public class CostCenterEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Manager { get; set; } = ""; public string Department { get; set; } = ""; public decimal PlannedBudget { get; set; } public string Status { get; set; } = "Active"; }
public class CostElementEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "Primary"; public string Category { get; set; } = "Material"; public string Status { get; set; } = "Active"; }
public class ProfitCenterEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Manager { get; set; } = ""; public string Department { get; set; } = ""; public string Status { get; set; } = "Active"; }
public class InternalOrderEntity : EntityBase { public string OrderNumber { get; set; } = ""; public string Name { get; set; } = ""; public string CostCenter { get; set; } = ""; public decimal PlannedCost { get; set; } public decimal ActualCost { get; set; } public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public string Status { get; set; } = "Planned"; }
// PS - Project System
public class ProjectEntity : EntityBase { public string ProjectCode { get; set; } = ""; public string Name { get; set; } = ""; public string Manager { get; set; } = ""; public string Department { get; set; } = ""; public decimal Budget { get; set; } public decimal Spent { get; set; } public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public string Status { get; set; } = "Planned"; }
public class ProjectTaskEntity : EntityBase { public string TaskCode { get; set; } = ""; public string Name { get; set; } = ""; public string ProjectCode { get; set; } = ""; public string AssignedTo { get; set; } = ""; public decimal PlannedHours { get; set; } public decimal ActualHours { get; set; } public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public string Status { get; set; } = "Not Started"; }
public class TimesheetEntryEntity : EntityBase { public string EmployeeName { get; set; } = ""; public string ProjectCode { get; set; } = ""; public string TaskCode { get; set; } = ""; public DateTime Date { get; set; } public decimal Hours { get; set; } public string Description { get; set; } = ""; public string Status { get; set; } = "Submitted"; }
// PM - Plant Maintenance
public class EquipmentEntity : EntityBase { public string EquipmentCode { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = "Machine"; public string Location { get; set; } = ""; public string Department { get; set; } = ""; public string Status { get; set; } = "Operational"; }
public class MaintenancePlanEntity : EntityBase { public string PlanCode { get; set; } = ""; public string Name { get; set; } = ""; public string EquipmentCode { get; set; } = ""; public string Frequency { get; set; } = "Monthly"; public string TaskDescription { get; set; } = ""; public decimal EstimatedHours { get; set; } public string Status { get; set; } = "Active"; }
public class MaintenanceOrderEntity : EntityBase { public string OrderNumber { get; set; } = ""; public string EquipmentCode { get; set; } = ""; public string Description { get; set; } = ""; public string Priority { get; set; } = "Medium"; public DateTime ScheduledDate { get; set; } public DateTime CompletedDate { get; set; } public decimal Cost { get; set; } public string Status { get; set; } = "Open"; }

// Audit
public class AuditLogEntity : EntityBase { public Guid? TenantId { get; set; } public Guid? UserId { get; set; } public string UserName { get; set; } = ""; public string ModuleName { get; set; } = ""; public string EntityName { get; set; } = ""; public string ActionType { get; set; } = ""; public string Description { get; set; } = ""; public string OldValues { get; set; } = ""; public string NewValues { get; set; } = ""; public string IpAddress { get; set; } = ""; public string DeviceInfo { get; set; } = ""; public string UserAgent { get; set; } = ""; public string SessionId { get; set; } = ""; public bool IsFlagged { get; set; } public DateTime Timestamp { get; set; } = DateTime.UtcNow; }

// Notifications
public class NotificationEntity : EntityBase { public Guid? UserId { get; set; } public string Channel { get; set; } = ""; public string Title { get; set; } = ""; public string Message { get; set; } = ""; public string LinkUrl { get; set; } = ""; public bool IsRead { get; set; } }

// Accounting
public class AccountEntity : EntityBase { public string AccountCode { get; set; } = ""; public string AccountName { get; set; } = ""; public string Type { get; set; } = "Asset"; public string Category { get; set; } = "Current"; public decimal Balance { get; set; } public bool IsActive { get; set; } = true; }
public class GeneralLedgerEntryEntity : EntityBase { public Guid TenantId { get; set; } public string DocumentNumber { get; set; } = ""; public DateTime EntryDate { get; set; } public string AccountCode { get; set; } = ""; public string AccountName { get; set; } = ""; public decimal Debit { get; set; } public decimal Credit { get; set; } public string Reference { get; set; } = ""; public string Description { get; set; } = ""; public string Period { get; set; } = ""; public bool IsPosted { get; set; } }
// Tax engine
public class TaxCodeEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public decimal Rate { get; set; } public string TaxType { get; set; } = "GST"; public string TaxAccountCode { get; set; } = "2300"; public bool IsCompound { get; set; } public bool IsActive { get; set; } = true; }
public class TaxTransactionEntity : EntityBase { public Guid TenantId { get; set; } public string DocumentNumber { get; set; } = ""; public string DocumentType { get; set; } = ""; public string PartyName { get; set; } = ""; public string TaxCode { get; set; } = ""; public string TaxName { get; set; } = ""; public decimal Rate { get; set; } public decimal NetAmount { get; set; } public decimal TaxAmount { get; set; } public decimal GrossAmount { get; set; } public DateTime Date { get; set; } public string Status { get; set; } = "Posted"; }
// Multi-currency
public class CurrencyEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Symbol { get; set; } = ""; public bool IsBase { get; set; } public int DecimalPlaces { get; set; } = 2; public bool IsActive { get; set; } = true; }
public class ExchangeRateEntity : EntityBase { public Guid TenantId { get; set; } public string FromCurrency { get; set; } = ""; public string ToCurrency { get; set; } = ""; public decimal Rate { get; set; } public DateTime EffectiveFrom { get; set; } public DateTime? EffectiveTo { get; set; } public string Source { get; set; } = "Manual"; }
// Delivery log (email / SMS)
public class MessageDeliveryEntity : EntityBase { public Guid TenantId { get; set; } public string Channel { get; set; } = ""; public string ToAddress { get; set; } = ""; public string Subject { get; set; } = ""; public string Body { get; set; } = ""; public string Status { get; set; } = "Sent"; public string ErrorMessage { get; set; } = ""; public string Provider { get; set; } = ""; public DateTime SentAt { get; set; } }
// CO - Cost allocation
public class CostAllocationRuleEntity : EntityBase { public Guid TenantId { get; set; } public string Name { get; set; } = ""; public string CostElementCode { get; set; } = ""; public string AllocationType { get; set; } = "Proportional"; public string Basis { get; set; } = "Headcount"; public bool IsActive { get; set; } = true; }
public class CostAllocationRunEntity : EntityBase { public Guid TenantId { get; set; } public string Period { get; set; } = ""; public decimal TotalAllocated { get; set; } public string Status { get; set; } = "Completed"; public DateTime RunAt { get; set; } public string CreatedBy { get; set; } = ""; }
public class CostAllocationDetailEntity : EntityBase { public Guid TenantId { get; set; } public Guid RunId { get; set; } public string CostCenterCode { get; set; } = ""; public string CostCenterName { get; set; } = ""; public string CostElementCode { get; set; } = ""; public decimal Amount { get; set; } public decimal SharePercent { get; set; } public string Basis { get; set; } = ""; }
// i18n - Localization
public class LanguageEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public bool IsActive { get; set; } = true; public bool IsDefault { get; set; } }
public class TranslationEntity : EntityBase { public Guid TenantId { get; set; } public string LanguageCode { get; set; } = ""; public string Key { get; set; } = ""; public string Value { get; set; } = ""; }

// Cross-cutting
public class ApprovalRequestEntity : EntityBase { public Guid TenantId { get; set; } public string RequestId { get; set; } = ""; public string Type { get; set; } = ""; public string Subject { get; set; } = ""; public string Requestor { get; set; } = ""; public DateTime RequestDate { get; set; } public decimal? Amount { get; set; } public string Status { get; set; } = "Pending"; }
public class CustomFieldEntity : EntityBase { public string Module { get; set; } = ""; public string EntityName { get; set; } = ""; public string FieldName { get; set; } = ""; public string FieldType { get; set; } = "Text"; public bool IsRequired { get; set; } public string DefaultValue { get; set; } = ""; }
public class AdminUserEntity : EntityBase { public string UserId { get; set; } = ""; public string UserName { get; set; } = ""; public string Email { get; set; } = ""; public string PasswordHash { get; set; } = ""; public string Role { get; set; } = "READ_ONLY"; public bool IsActive { get; set; } = true; public bool IsSuperUser { get; set; } public int FailedLoginAttempts { get; set; } public DateTime? LockedUntil { get; set; } public DateTime? LastLoginAt { get; set; } public DateTime? PasswordChangedAt { get; set; } public bool MfaEnabled { get; set; } public string MfaSecret { get; set; } = ""; }
public class TenantEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Status { get; set; } = "ACTIVE"; public int MaxUsers { get; set; } = 100; }
  public class RefreshTokenEntity : EntityBase { public Guid UserId { get; set; } public string Token { get; set; } = ""; public DateTime ExpiresAt { get; set; } public bool IsRevoked { get; set; } public string ReplacedByToken { get; set; } = ""; public string DeviceInfo { get; set; } = ""; public string IpAddress { get; set; } = ""; public Guid? TenantId { get; set; } }
public class TenantSettingEntity : EntityBase { public string TenantCode { get; set; } = ""; public string Name { get; set; } = ""; public string Subdomain { get; set; } = ""; public string Status { get; set; } = "Active"; }
public class SystemConfigEntity : EntityBase { public string Key { get; set; } = ""; public string Value { get; set; } = ""; public string Description { get; set; } = ""; public string Module { get; set; } = "Global"; }

// Integration
public class WebhookEntity : EntityBase { public Guid TenantId { get; set; } public string Name { get; set; } = ""; public string EventType { get; set; } = ""; public string TargetUrl { get; set; } = ""; public string SecretKey { get; set; } = ""; public bool IsActive { get; set; } = true; public int RetryCount { get; set; } public DateTime? LastTriggeredAt { get; set; } }
public class EdiTradingPartnerEntity : EntityBase { public Guid TenantId { get; set; } public string PartnerCode { get; set; } = ""; public string PartnerName { get; set; } = ""; public string Standard { get; set; } = "EDIFACT"; public string Version { get; set; } = "D96A"; public string SenderId { get; set; } = ""; public string ReceiverId { get; set; } = ""; public string SenderQualifier { get; set; } = "ZZ"; public string ReceiverQualifier { get; set; } = "ZZ"; public string TestIndicator { get; set; } = "T"; public string EndpointUrl { get; set; } = ""; public string AuthType { get; set; } = "None"; public string AuthConfigJson { get; set; } = "{}"; public string DocumentTypes { get; set; } = "PO,INVOICE,GRN"; public bool IsActive { get; set; } = true; }
public class EdiAcknowledgmentEntity : EntityBase { public Guid TenantId { get; set; } public Guid PartnerId { get; set; } public string PartnerCode { get; set; } = ""; public string Direction { get; set; } = "Outbound"; public string InterchangeId { get; set; } = ""; public string MessageRef { get; set; } = ""; public string DocumentType { get; set; } = ""; public string AckCode { get; set; } = "Accepted"; public string Description { get; set; } = ""; public string RawAck { get; set; } = ""; public DateTime ReceivedAt { get; set; } }
public class WebhookDeliveryLogEntity : EntityBase { public Guid TenantId { get; set; } public Guid WebhookId { get; set; } public string EventType { get; set; } = ""; public string TargetUrl { get; set; } = ""; public int StatusCode { get; set; } public string ResponseBody { get; set; } = ""; public string ErrorMessage { get; set; } = ""; public bool IsSuccess { get; set; } public DateTime AttemptedAt { get; set; } }
public class ApiClientEntity : EntityBase { public Guid TenantId { get; set; } public string ClientId { get; set; } = ""; public string ClientSecret { get; set; } = ""; public string Name { get; set; } = ""; public string[] AllowedIpAddresses { get; set; } = Array.Empty<string>(); public bool IsActive { get; set; } = true; }
public class IntegrationQueueEntity : EntityBase { public Guid TenantId { get; set; } public string MessageType { get; set; } = ""; public string Payload { get; set; } = "{}"; public string Status { get; set; } = "Pending"; public int RetryCount { get; set; } public int MaxRetries { get; set; } = 3; public string LastError { get; set; } = ""; public DateTime? NextRetryAt { get; set; } public string Direction { get; set; } = "Outbound"; public string TargetSystem { get; set; } = ""; }
public class IntegrationDeadLetterEntity : EntityBase { public Guid TenantId { get; set; } public Guid OriginalQueueId { get; set; } public string MessageType { get; set; } = ""; public string Payload { get; set; } = "{}"; public string ErrorMessage { get; set; } = ""; public int RetryAttempts { get; set; } public DateTime FailedAt { get; set; } }
public class IntegrationConnectionEntity : EntityBase { public Guid TenantId { get; set; } public string ConnectorType { get; set; } = ""; public string Name { get; set; } = ""; public string BaseUrl { get; set; } = ""; public string AuthType { get; set; } = "None"; public string AuthConfigJson { get; set; } = "{}"; public string AdditionalConfigJson { get; set; } = "{}"; public bool IsActive { get; set; } = true; public int TimeoutSeconds { get; set; } = 30; public DateTime? LastTestedAt { get; set; } public string LastTestResult { get; set; } = ""; }
public class SyncJobEntity : EntityBase { public Guid TenantId { get; set; } public string Name { get; set; } = ""; public string ConnectorType { get; set; } = ""; public Guid ConnectionId { get; set; } public string Direction { get; set; } = "Pull"; public string EntityType { get; set; } = ""; public string ScheduleType { get; set; } = "Manual"; public string CronExpression { get; set; } = ""; public string MappingJson { get; set; } = "{}"; public string ConflictResolution { get; set; } = "SourceWins"; public bool IsActive { get; set; } = true; public DateTime? LastRunAt { get; set; } public string LastRunResult { get; set; } = ""; }
public class SyncLogEntity : EntityBase { public Guid TenantId { get; set; } public Guid SyncJobId { get; set; } public string Direction { get; set; } = ""; public int TotalRecords { get; set; } public int SuccessCount { get; set; } public int ErrorCount { get; set; } public int ConflictCount { get; set; } public string ErrorDetail { get; set; } = ""; public DateTime StartedAt { get; set; } public DateTime? CompletedAt { get; set; } }
public class MappingRuleEntity : EntityBase { public Guid TenantId { get; set; } public string Name { get; set; } = ""; public string SourceSystem { get; set; } = ""; public string TargetSystem { get; set; } = ""; public string SourceEntity { get; set; } = ""; public string TargetEntity { get; set; } = ""; public string FieldMappingsJson { get; set; } = "[]"; public string TransformationScript { get; set; } = ""; public bool IsActive { get; set; } = true; }
public class PluginEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Version { get; set; } = "1.0"; public string Description { get; set; } = ""; public string AssemblyPath { get; set; } = ""; public bool IsCore { get; set; } public bool IsEnabledGlobal { get; set; } = true; public bool IsEnabledForTenant { get; set; } public string Dependencies { get; set; } = "[]"; public string MinVersion { get; set; } = ""; public string MaxVersion { get; set; } = ""; public string IconClass { get; set; } = ""; public string ConfigJson { get; set; } = "{}"; }
public class PluginSettingEntity : EntityBase { public Guid PluginId { get; set; } public Guid TenantId { get; set; } public string Key { get; set; } = ""; public string Value { get; set; } = ""; }
public class PluginTenantPermissionEntity : EntityBase { public Guid PluginId { get; set; } public Guid TenantId { get; set; } public bool IsEnabled { get; set; } }
public class NumberRangeDefinitionEntity : EntityBase { public Guid TenantId { get; set; } public string Module { get; set; } = ""; public string Prefix { get; set; } = ""; public string Code { get; set; } = ""; public string Name { get; set; } = ""; public long NextNumber { get; set; } }
// T-Code Generator
public class TCodeDefinitionEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Module { get; set; } = ""; public string Description { get; set; } = ""; public bool HasWorkflow { get; set; } public bool HasNumberRange { get; set; } public string Prefix { get; set; } = ""; public string Status { get; set; } = "Active"; public string CreatedBy { get; set; } = ""; }
public class TCodeFieldEntity : EntityBase { public Guid TenantId { get; set; } public Guid TCodeId { get; set; } public string FieldName { get; set; } = ""; public string FieldLabel { get; set; } = ""; public string DataType { get; set; } = "TEXT"; public bool IsRequired { get; set; } public bool IsVisible { get; set; } = true; public bool IsSystem { get; set; } public string DefaultValue { get; set; } = ""; public string ValidationRuleJson { get; set; } = "{}"; public string ConditionalVisibilityJson { get; set; } = "{}"; public int OrderIndex { get; set; } public int Width { get; set; } = 200; public bool IsFrozen { get; set; } public string SectionName { get; set; } = "General"; }
public class TCodeDataEntity : EntityBase { public Guid TenantId { get; set; } public Guid TCodeId { get; set; } public string RecordId { get; set; } = ""; public string DataJson { get; set; } = "{}"; public string Status { get; set; } = "Draft"; public string WorkflowNode { get; set; } = ""; public Guid? CreatedBy { get; set; } public Guid? UpdatedBy { get; set; } }
// Customization
public class CustomizationTCodeFieldEntity : EntityBase { public Guid TenantId { get; set; } public string TCode { get; set; } = ""; public string FieldName { get; set; } = ""; public string FieldLabel { get; set; } = ""; public string DataType { get; set; } = "TEXT"; public bool IsRequired { get; set; } public bool IsVisible { get; set; } = true; public string DefaultValue { get; set; } = ""; public string ValidationRuleJson { get; set; } = "{}"; public string ConditionalVisibilityJson { get; set; } = "{}"; }
public class CustomizationTCodeLayoutEntity : EntityBase { public Guid TenantId { get; set; } public string TCode { get; set; } = ""; public string FieldName { get; set; } = ""; public string SectionName { get; set; } = "General"; public int OrderIndex { get; set; } public int Width { get; set; } = 200; public bool IsFrozen { get; set; } }
// Transaction Codes
public class MigrationEntity : EntityBase { public string Name { get; set; } = ""; public DateTime AppliedAt { get; set; } = DateTime.UtcNow; }
public class TransactionCodeEntity : EntityBase { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Description { get; set; } = ""; public string Module { get; set; } = ""; public string GroupName { get; set; } = "Transactions"; public string Route { get; set; } = ""; public string Icon { get; set; } = "bi-asterisk"; public int SortOrder { get; set; } public string Status { get; set; } = "Active"; public bool IsSystem { get; set; } public string RequiredRole { get; set; } = ""; public string Params { get; set; } = "{}"; }
public class TransactionPermissionEntity : EntityBase { public Guid TransactionCodeId { get; set; } public string PrincipalType { get; set; } = "Role"; public string PrincipalValue { get; set; } = ""; public bool CanAccess { get; set; } = true; public bool IsFavorite { get; set; } }
public class TransactionLogEntity : EntityBase { public Guid TransactionCodeId { get; set; } public string TransactionCode { get; set; } = ""; public Guid? UserId { get; set; } public string UserName { get; set; } = ""; public Guid? TenantId { get; set; } public string Status { get; set; } = "Success"; public string IpAddress { get; set; } = ""; public long DurationMs { get; set; } public string? ErrorMessage { get; set; } public string? RequestData { get; set; } public string? ResponseData { get; set; } }

// Workflow
public class WorkflowDefinitionEntity : EntityBase { public Guid TenantId { get; set; } public string Name { get; set; } = ""; public string Code { get; set; } = ""; public string Module { get; set; } = ""; public string Description { get; set; } = ""; public bool IsActive { get; set; } = true; public int Version { get; set; } = 1; public string BpmnXml { get; set; } = ""; public Guid CreatedBy { get; set; } }
public class WorkflowNodeEntity : EntityBase { public Guid WorkflowId { get; set; } public string NodeType { get; set; } = ""; public string Label { get; set; } = ""; public string Description { get; set; } = ""; public string ConfigJson { get; set; } = "{}"; public double PositionX { get; set; } public double PositionY { get; set; } }
public class WorkflowEdgeEntity : EntityBase { public Guid WorkflowId { get; set; } public Guid FromNodeId { get; set; } public Guid ToNodeId { get; set; } public string ConditionExpression { get; set; } = ""; public string Label { get; set; } = ""; public int SequenceOrder { get; set; } public string BranchType { get; set; } = "SEQUENTIAL"; }
public class WorkflowInstanceEntity : EntityBase { public Guid WorkflowId { get; set; } public Guid TenantId { get; set; } public string EntityName { get; set; } = ""; public string EntityId { get; set; } = ""; public Guid? CurrentNodeId { get; set; } public string Status { get; set; } = "ACTIVE"; public string Variables { get; set; } = "{}"; public Guid StartedBy { get; set; } public DateTime? CompletedAt { get; set; } public string ActiveTokens { get; set; } = "[]"; }
public class WorkflowHistoryEntity : EntityBase { public Guid WorkflowInstanceId { get; set; } public Guid? NodeId { get; set; } public string Action { get; set; } = ""; public Guid? ActorId { get; set; } public string Comment { get; set; } = ""; public string Payload { get; set; } = "{}"; }

// MRP Extensions
public class MrpRunHistoryEntity : EntityBase { public Guid TenantId { get; set; } public string RunType { get; set; } = ""; public DateTime RunAt { get; set; } public string Status { get; set; } = "Completed"; public int MaterialsProcessed { get; set; } public int SuggestionsGenerated { get; set; } public int ExceptionMessages { get; set; } public string Parameters { get; set; } = "{}"; public long DurationMs { get; set; } }
public class MrpExceptionMessageEntity : EntityBase { public Guid TenantId { get; set; } public Guid? RunHistoryId { get; set; } public string MaterialCode { get; set; } = ""; public string MaterialName { get; set; } = ""; public string ExceptionType { get; set; } = ""; public string Message { get; set; } = ""; public string Severity { get; set; } = "Error"; public string SuggestedAction { get; set; } = ""; }
public class PlantEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Location { get; set; } = ""; public bool IsActive { get; set; } = true; }
public class VendorLeadTimeEntity : EntityBase { public Guid TenantId { get; set; } public Guid VendorId { get; set; } public string MaterialCode { get; set; } = ""; public int LeadTimeDays { get; set; } public decimal Reliability { get; set; } = 1.0m; }
public class MrpCapacityLevelEntity : EntityBase { public Guid TenantId { get; set; } public Guid? RunHistoryId { get; set; } public string WorkCenterCode { get; set; } = ""; public decimal AvailableHours { get; set; } public decimal RequiredHours { get; set; } public decimal LoadPercent { get; set; } public string LevelingSuggestion { get; set; } = ""; }

// Batch & Serial Lifecycle Management (MM-BATCH)
public class BatchEntity : EntityBase { public Guid TenantId { get; set; } public string BatchNumber { get; set; } = ""; public Guid MaterialId { get; set; } public string MaterialName { get; set; } = ""; public DateTime ManufacturingDate { get; set; } public DateTime? ExpiryDate { get; set; } public int? ShelfLifeDays { get; set; } public string Status { get; set; } = "ACTIVE"; public decimal Quantity { get; set; } public decimal QuantityConsumed { get; set; } public string UnitOfMeasure { get; set; } = "EA"; public Guid? StorageLocationId { get; set; } public string StorageLocationName { get; set; } = ""; public Guid? SupplierId { get; set; } public string SupplierName { get; set; } = ""; public string CertificateOfAnalysis { get; set; } = ""; public string Notes { get; set; } = ""; }
public class SerialNumberEntity : EntityBase { public Guid TenantId { get; set; } public string SerialNumber { get; set; } = ""; public Guid MaterialId { get; set; } public string MaterialName { get; set; } = ""; public Guid? BatchId { get; set; } public string BatchNumber { get; set; } = ""; public DateTime ManufacturingDate { get; set; } public DateTime? WarrantyExpiryDate { get; set; } public string Status { get; set; } = "ACTIVE"; public Guid? CurrentOwnerId { get; set; } public string CurrentOwnerName { get; set; } = ""; public string POReference { get; set; } = ""; public string Notes { get; set; } = ""; }
public class BatchMovementEntity : EntityBase { public Guid TenantId { get; set; } public Guid BatchId { get; set; } public string BatchNumber { get; set; } = ""; public string MovementType { get; set; } = ""; public decimal Quantity { get; set; } public string FromLocation { get; set; } = ""; public string ToLocation { get; set; } = ""; public string DocumentNumber { get; set; } = ""; public DateTime MovementDate { get; set; } public Guid UserId { get; set; } public string UserName { get; set; } = ""; public string Notes { get; set; } = ""; }
public class RecallEntity : EntityBase { public Guid TenantId { get; set; } public string RecallNumber { get; set; } = ""; public string Reason { get; set; } = ""; public string AffectedBatchIds { get; set; } = "[]"; public string AffectedBatchNumbers { get; set; } = ""; public Guid InitiatedBy { get; set; } public string InitiatedByName { get; set; } = ""; public DateTime InitiatedDate { get; set; } public string Status { get; set; } = "OPEN"; public string ResolutionNotes { get; set; } = ""; public DateTime? ResolvedDate { get; set; } }

// Stock Movements (audit trail of every stock change)
public class StockMovementEntity : EntityBase { public Guid TenantId { get; set; } public string DocumentNumber { get; set; } = ""; public string MaterialName { get; set; } = ""; public string MovementType { get; set; } = ""; public decimal Quantity { get; set; } public decimal StockBefore { get; set; } public decimal StockAfter { get; set; } public string Reference { get; set; } = ""; public string Status { get; set; } = "Posted"; }

// ATP/CTP - Stock Reservations and Allocations
public class StockReservationEntity : EntityBase { public Guid MaterialId { get; set; } public string MaterialName { get; set; } = ""; public decimal Quantity { get; set; } public Guid OrderId { get; set; } public string Status { get; set; } = "Active"; public DateTime ReservedAt { get; set; } public DateTime? ReleasedAt { get; set; } }
public class StockAllocationEntity : EntityBase { public Guid MaterialId { get; set; } public string MaterialName { get; set; } = ""; public decimal Quantity { get; set; } public string AllocationType { get; set; } = ""; public string Reference { get; set; } = ""; public string Status { get; set; } = "Allocated"; public DateTime AllocatedAt { get; set; } }

// Bank Statement Import
public class BankStatementEntity : EntityBase { public Guid TenantId { get; set; } public string StatementNumber { get; set; } = ""; public Guid AccountId { get; set; } public DateTime StatementDate { get; set; } public DateTime ImportDate { get; set; } public string Source { get; set; } = "MANUAL"; public string Status { get; set; } = "PENDING"; public decimal TotalDebits { get; set; } public decimal TotalCredits { get; set; } }
public class BankStatementLineEntity : EntityBase { public Guid StatementId { get; set; } public DateTime TransactionDate { get; set; } public DateTime? ValueDate { get; set; } public string Description { get; set; } = ""; public string Reference { get; set; } = ""; public decimal Debit { get; set; } public decimal Credit { get; set; } public decimal Balance { get; set; } public Guid? MatchedPaymentId { get; set; } public Guid? MatchedJournalId { get; set; } public string Status { get; set; } = "UNMATCHED"; }

// Finance loop additions
public class FiscalPeriodEntity : EntityBase { public Guid TenantId { get; set; } public string Period { get; set; } = ""; public string FiscalYear { get; set; } = ""; public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public string Status { get; set; } = "Open"; public DateTime? ClosedAt { get; set; } public string ClosedBy { get; set; } = ""; }
public class BankReconciliationEntity : EntityBase { public Guid TenantId { get; set; } public string AccountCode { get; set; } = ""; public string AccountName { get; set; } = ""; public DateTime StatementDate { get; set; } public decimal StatementBalance { get; set; } public decimal LedgerBalance { get; set; } public decimal Difference { get; set; } public string Status { get; set; } = "Draft"; public string Notes { get; set; } = ""; }
public class PaymentEntity : EntityBase { public Guid TenantId { get; set; } public string PaymentNumber { get; set; } = ""; public DateTime Date { get; set; } public string PartyName { get; set; } = ""; public string Type { get; set; } = "Payment"; public string Reference { get; set; } = ""; public decimal Amount { get; set; } public string Method { get; set; } = "Bank Transfer"; public string Status { get; set; } = "Posted"; }
public class DepreciationScheduleEntity : EntityBase { public Guid TenantId { get; set; } public Guid AssetId { get; set; } public string AssetCode { get; set; } = ""; public string AssetName { get; set; } = ""; public string Period { get; set; } = ""; public decimal DepreciationAmount { get; set; } public decimal AccumulatedDepreciation { get; set; } public decimal BookValue { get; set; } public string Status { get; set; } = "Posted"; }
public class ApprovalStepEntity : EntityBase { public Guid TenantId { get; set; } public Guid ApprovalRequestId { get; set; } public int Level { get; set; } public string ApproverName { get; set; } = ""; public string ApproverUserId { get; set; } = ""; public string Status { get; set; } = "Pending"; public string Comments { get; set; } = ""; public DateTime? ActionedAt { get; set; } }

// Movement Type Registry (MIGO)
public class MovementTypeEntity : EntityBase { public Guid TenantId { get; set; } public int MovementType { get; set; } public string SpecialStockIndicator { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string LongDescription { get; set; } = string.Empty; public string Category { get; set; } = string.Empty; public int? ReversalMovementType { get; set; } public string AllowedStockTypes { get; set; } = string.Empty; public string PostingRules { get; set; } = string.Empty; public bool InventoryUpdate { get; set; } = true; public bool ConsumptionUpdate { get; set; } = false; public bool ValueUpdate { get; set; } = true; public bool QuantityUpdate { get; set; } = true; public bool AutoBatchCreate { get; set; } = false; public bool QualityInspectionRequired { get; set; } = false; public bool RequiresReference { get; set; } = false; public bool AllowsNegativeStock { get; set; } = false; public string WorkflowTemplate { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public string Module { get; set; } = string.Empty; public string TransactionCode { get; set; } = string.Empty; }
public class MovementTypeCategoryEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public int SortOrder { get; set; } public bool IsActive { get; set; } = true; }
public class MovementTypeStockTypeEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public class MovementTypePostingRuleEntity : EntityBase { public Guid TenantId { get; set; } public int MovementType { get; set; } public string RuleName { get; set; } = string.Empty; public string DebitAccount { get; set; } = string.Empty; public string CreditAccount { get; set; } = string.Empty; public string Condition { get; set; } = string.Empty; public int Priority { get; set; } public bool IsActive { get; set; } = true; }
public class MovementTypeIntegrationEntity : EntityBase { public Guid TenantId { get; set; } public int MovementType { get; set; } public string TargetModule { get; set; } = string.Empty; public string EventType { get; set; } = string.Empty; public string WebhookUrl { get; set; } = string.Empty; public bool IsEnabled { get; set; } = true; }
public class MovementDocumentEntity : EntityBase { public Guid TenantId { get; set; } public string DocumentNumber { get; set; } = string.Empty; public int MovementType { get; set; } public string MovementTypeDescription { get; set; } = string.Empty; public string SpecialStockIndicator { get; set; } = string.Empty; public string PostingDate { get; set; } = string.Empty; public string DocumentDate { get; set; } = string.Empty; public string Reference { get; set; } = string.Empty; public string HeaderText { get; set; } = string.Empty; public string Status { get; set; } = "POSTED"; public bool IsReversal { get; set; } public Guid? ReversalOfDocumentId { get; set; } public string PostedBy { get; set; } = string.Empty; public string PostedAt { get; set; } = string.Empty; public string Plant { get; set; } = string.Empty; public string StorageLocation { get; set; } = string.Empty; public decimal TotalQuantity { get; set; } public string UserId { get; set; } = string.Empty; }
public class MovementDocumentLineEntity : EntityBase { public Guid TenantId { get; set; } public Guid MovementDocumentId { get; set; } public int LineNumber { get; set; } public string MaterialCode { get; set; } = string.Empty; public string MaterialName { get; set; } = string.Empty; public decimal Quantity { get; set; } public string UOM { get; set; } = "EA"; public decimal UnitPrice { get; set; } public decimal TotalPrice => Quantity * UnitPrice; public string Plant { get; set; } = string.Empty; public string StorageLocation { get; set; } = string.Empty; public string BatchNo { get; set; } = string.Empty; public string SerialNumber { get; set; } = string.Empty; public string VendorCode { get; set; } = string.Empty; public string VendorName { get; set; } = string.Empty; public string CustomerCode { get; set; } = string.Empty; public string CustomerName { get; set; } = string.Empty; public string ProductionOrderNo { get; set; } = string.Empty; public string PurchaseOrderNo { get; set; } = string.Empty; public string SalesOrderNo { get; set; } = string.Empty; public string CostCenter { get; set; } = string.Empty; public string ProfitCenter { get; set; } = string.Empty; public string GLAccount { get; set; } = string.Empty; public string MovementType { get; set; } = string.Empty; public string SpecialStockIndicator { get; set; } = string.Empty; public string StockType { get; set; } = "FREE"; public string BatchStatus { get; set; } = string.Empty; public string QualityInspectionLot { get; set; } = string.Empty; public decimal Weight { get; set; } public string WeightUnit { get; set; } = string.Empty; public string Volume { get; set; } = string.Empty; public string VolumeUnit { get; set; } = string.Empty; public string ItemText { get; set; } = string.Empty; public string UnloadingPoint { get; set; } = string.Empty; public string ShelfLifeDate { get; set; } = string.Empty; public string ManufactureDate { get; set; } = string.Empty; public bool IsReversal { get; set; } public string Status { get; set; } = "POSTED"; }
public class MovementTypeWorkflowEntity : EntityBase { public Guid TenantId { get; set; } public int MovementType { get; set; } public string StepName { get; set; } = string.Empty; public int StepOrder { get; set; } public string StepType { get; set; } = string.Empty; public string Configuration { get; set; } = string.Empty; public bool IsRequired { get; set; } = true; public bool IsActive { get; set; } = true; }

// Procure-to-Pay Line Items
public class PurchaseRequisitionItemEntity : EntityBase { public Guid TenantId { get; set; } public Guid PurchaseRequisitionId { get; set; } public int LineNumber { get; set; } public string MaterialName { get; set; } = string.Empty; public string MaterialCode { get; set; } = string.Empty; public decimal Quantity { get; set; } public string UOM { get; set; } = "EA"; public decimal UnitPrice { get; set; } public decimal TotalPrice { get; set; } public string Plant { get; set; } = string.Empty; public string StorageLocation { get; set; } = string.Empty; public string DeliveryDate { get; set; } = string.Empty; public string Status { get; set; } = "OPEN"; public string DepartmentKey { get; set; } = string.Empty; public string CostCenter { get; set; } = string.Empty; public string Remarks { get; set; } = string.Empty; }
public class PurchaseOrderItemEntity : EntityBase { public Guid TenantId { get; set; } public Guid PurchaseOrderId { get; set; } public int LineNumber { get; set; } public string MaterialName { get; set; } = string.Empty; public string MaterialCode { get; set; } = string.Empty; public decimal Quantity { get; set; } public string UOM { get; set; } = "EA"; public decimal UnitPrice { get; set; } public decimal TotalPrice { get; set; } public string Plant { get; set; } = string.Empty; public string StorageLocation { get; set; } = string.Empty; public string DeliveryDate { get; set; } = string.Empty; public decimal ReceivedQty { get; set; } public decimal InvoicedQty { get; set; } public string Status { get; set; } = "OPEN"; public string DepartmentKey { get; set; } = string.Empty; public string CostCenter { get; set; } = string.Empty; public string BatchNo { get; set; } = string.Empty; }

// Department Key
public class DepartmentKeyEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string CostCenterDefault { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }

// Release Strategy
public class ReleaseStrategyEntity : EntityBase { public Guid TenantId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string DocumentType { get; set; } = string.Empty; public decimal MinAmount { get; set; } public decimal MaxAmount { get; set; } public string Plant { get; set; } = string.Empty; public string DepartmentKey { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public class ReleaseCodeEntity : EntityBase { public Guid TenantId { get; set; } public Guid ReleaseStrategyId { get; set; } public int Level { get; set; } public string Code { get; set; } = string.Empty; public string ApproverRole { get; set; } = string.Empty; public string ApproverUserId { get; set; } = string.Empty; public bool IsRequired { get; set; } = true; }

// ══════════════════════════════════════════════════════════════════════════════
// Customer Complaint & Return with Supplier Pass-Through Claim (SD-QM-MM-FI)
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// CR-01/CR-06: Master entity for end-to-end customer complaint lifecycle.
/// Bridges SD (Return Order), QM (Complaint Notification), MM (Supplier Claim), FI (Credit/Debit Memo).
/// </summary>
public class CustomerComplaintReturnEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string ComplaintNumber { get; set; } = "";
    public string ComplaintType { get; set; } = "Q1";
    public string ReturnType { get; set; } = "RE";
    public string SalesOrderNumber { get; set; } = "";
    public string ReturnOrderNumber { get; set; } = "";
    public string DeliveryNumber { get; set; } = "";
    public string CreditMemoNumber { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal ReturnQuantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public decimal ReturnAmount { get; set; }
    public string BatchNumber { get; set; } = "";
    public string DefectCode { get; set; } = "";
    public string DefectDescription { get; set; } = "";
    public string DefectCategory { get; set; } = "";
    public string RootCause { get; set; } = "";
    public string RootCauseCode { get; set; } = "";
    public string SupplierBatchNumber { get; set; } = "";
    public string SupplierVendorCode { get; set; } = "";
    public string SupplierVendorName { get; set; } = "";
    public string PurchaseOrderReference { get; set; } = "";
    public string InspectionLotNumber { get; set; } = "";
    public string UsageDecision { get; set; } = "";
    public string StockProposal { get; set; } = "";
    public string QualityNotificationNumber { get; set; } = "";
    public string SupplierClaimNumber { get; set; } = "";
    public string SupplierReturnDeliveryNumber { get; set; } = "";
    public string SupplierDebitMemoNumber { get; set; } = "";
    public decimal CreditMemoAmount { get; set; }
    public decimal SupplierClaimAmount { get; set; }
    public decimal RecoveryAmount { get; set; }
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string CostCenter { get; set; } = "";
    public string ProfitCenter { get; set; } = "";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "CREATED";
    public string CurrentStep { get; set; } = "";
    public string AssignedTo { get; set; } = "";
    public DateTime? ComplaintDate { get; set; }
    public DateTime? ReturnReceivedDate { get; set; }
    public DateTime? InspectionCompletedDate { get; set; }
    public DateTime? CreditMemoIssuedDate { get; set; }
    public DateTime? SupplierClaimCreatedDate { get; set; }
    public DateTime? SupplierReturnDate { get; set; }
    public DateTime? RecoveryCompletedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string Notes { get; set; } = "";
    public string ResolutionNotes { get; set; } = "";
}

/// <summary>
/// CR-02: Return delivery and goods receipt tracking with movement types.
/// </summary>
public class ReturnDeliveryEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid ComplaintReturnId { get; set; }
    public string ReturnOrderNumber { get; set; } = "";
    public string DeliveryNumber { get; set; } = "";
    public string MaterialDocumentNumber { get; set; } = "";
    public int MovementType { get; set; }
    public string MovementTypeDescription { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public string BatchNumber { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string StockType { get; set; } = "QI";
    public string StockTypeDescription { get; set; } = "Quality Inspection";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string PostingDate { get; set; } = "";
    public string DocumentDate { get; set; } = "";
    public string Reference { get; set; } = "";
    public string HeaderText { get; set; } = "";
    public string Status { get; set; } = "CREATED";
    public string PostedBy { get; set; } = "";
    public DateTime? PostedAt { get; set; }
    public string Notes { get; set; } = "";
}

/// <summary>
/// CR-03/CR-04: Quality inspection tracking for returned goods.
/// </summary>
public class QualityInspectionReturnEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid ComplaintReturnId { get; set; }
    public string InspectionLotNumber { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string SupplierBatchNumber { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public string Plant { get; set; } = "";
    public string InspectionType { get; set; } = "RETURN";
    public string InspectionLotOrigin { get; set; } = "08";
    public string Characteristic { get; set; } = "";
    public string Specification { get; set; } = "";
    public string ResultValue { get; set; } = "";
    public string ResultValuation { get; set; } = "";
    public string DefectCodeGroup { get; set; } = "";
    public string DefectCode { get; set; } = "";
    public string DefectDescription { get; set; } = "";
    public string DefectCategory { get; set; } = "";
    public string RootCause { get; set; } = "";
    public string RootCauseCode { get; set; } = "";
    public string UsageDecision { get; set; } = "";
    public string UsageDecisionCode { get; set; } = "";
    public string StockProposal { get; set; } = "";
    public string TargetStockType { get; set; } = "";
    public string Status { get; set; } = "OPEN";
    public string RecordedBy { get; set; } = "";
    public DateTime? RecordedAt { get; set; }
    public string DecidedBy { get; set; } = "";
    public DateTime? DecisionDate { get; set; }
    public string Notes { get; set; } = "";
}

/// <summary>
/// CR-05/CR-08: Financial postings for customer credit and supplier debit memos.
/// </summary>
public class ComplaintFinancialPostingEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid ComplaintReturnId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string PostingType { get; set; } = "";
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string PartyCode { get; set; } = "";
    public string PartyName { get; set; } = "";
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Reference { get; set; } = "";
    public string Description { get; set; } = "";
    public string CostCenter { get; set; } = "";
    public string ProfitCenter { get; set; } = "";
    public string GLAccount { get; set; } = "";
    public string PostingDate { get; set; } = "";
    public string DocumentDate { get; set; } = "";
    public string Period { get; set; } = "";
    public string FiscalYear { get; set; } = "";
    public string Status { get; set; } = "POSTED";
    public string PostedBy { get; set; } = "";
    public DateTime? PostedAt { get; set; }
    public string Notes { get; set; } = "";
}

/// <summary>
/// CR-06: Supplier complaint and claim tracking.
/// </summary>
public class SupplierClaimEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid ComplaintReturnId { get; set; }
    public string SupplierClaimNumber { get; set; } = "";
    public string SupplierComplaintType { get; set; } = "Q2";
    public string VendorCode { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string SupplierBatchNumber { get; set; } = "";
    public string PurchaseOrderNumber { get; set; } = "";
    public string GoodsReceiptNumber { get; set; } = "";
    public decimal ClaimQuantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal ClaimAmount { get; set; }
    public decimal UnitPrice { get; set; } = 0;
    public string DefectCode { get; set; } = "";
    public string DefectDescription { get; set; } = "";
    public string DefectCategory { get; set; } = "";
    public string RootCause { get; set; } = "";
    public string RootCauseCode { get; set; } = "";
    public string CustomerComplaintReference { get; set; } = "";
    public string CustomerComplaintNumber { get; set; } = "";
    public string CustomerReturnNumber { get; set; } = "";
    public string QualityNotificationNumber { get; set; } = "";
    public string SupplierReturnDeliveryNumber { get; set; } = "";
    public string SupplierReturnMaterialDocument { get; set; } = "";
    public string DebitMemoNumber { get; set; } = "";
    public int SupplierReturnMovementType { get; set; }
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string CostCenter { get; set; } = "";
    public string ProfitCenter { get; set; } = "";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "CREATED";
    public string CurrentStep { get; set; } = "";
    public string AssignedTo { get; set; } = "";
    public DateTime? ClaimCreatedDate { get; set; }
    public DateTime? SupplierNotifiedDate { get; set; }
    public DateTime? SupplierReturnDate { get; set; }
    public DateTime? RecoveryCompletedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string Notes { get; set; } = "";
    public string ResolutionNotes { get; set; } = "";
}

/// <summary>
/// CR-07: Supplier return delivery tracking with movement type 122/161.
/// </summary>
public class SupplierReturnDeliveryEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid SupplierClaimId { get; set; }
    public string SupplierReturnNumber { get; set; } = "";
    public string DeliveryNumber { get; set; } = "";
    public string MaterialDocumentNumber { get; set; } = "";
    public int MovementType { get; set; }
    public string MovementTypeDescription { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public string BatchNumber { get; set; } = "";
    public string VendorCode { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string PurchaseOrderNumber { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string StockType { get; set; } = "BLOCKED";
    public string PostingDate { get; set; } = "";
    public string DocumentDate { get; set; } = "";
    public string Reference { get; set; } = "";
    public string HeaderText { get; set; } = "";
    public string Status { get; set; } = "CREATED";
    public string PostedBy { get; set; } = "";
    public DateTime? PostedAt { get; set; }
    public string Notes { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 1.2: Segregation of Duties (SOX) & Immutable Audit Trails
// ══════════════════════════════════════════════════════════════════════════════

public class SoxDutyEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string DutyCode { get; set; } = "";
    public string DutyName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Module { get; set; } = "";
    public string TransactionCode { get; set; } = "";
    public string ActionType { get; set; } = "";
    public int MinApprovers { get; set; } = 1;
    public string RequiredRoles { get; set; } = "[]";
    public string ConflictDuties { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class SoxAssignmentEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Role { get; set; } = "";
    public string DutyCode { get; set; } = "";
    public string DutyName { get; set; } = "";
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string AssignedBy { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = "";
}

public class SoxViolationEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string ViolationType { get; set; } = "";
    public string DutyCode1 { get; set; } = "";
    public string DutyCode2 { get; set; } = "";
    public string TransactionCode { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string DetectedBy { get; set; } = "";
    public string Severity { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public string ResolutionNotes { get; set; } = "";
    public string ResolvedBy { get; set; } = "";
    public DateTime? ResolvedAt { get; set; }
    public string Description { get; set; } = "";
}

public class ImmutableAuditTrailEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public long SequenceNumber { get; set; }
    public string TableName { get; set; } = "";
    public string RecordId { get; set; } = "";
    public string ActionType { get; set; } = "";
    public string OldValues { get; set; } = "{}";
    public string NewValues { get; set; } = "{}";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string UserIp { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string PreviousHash { get; set; } = "";
    public string CurrentHash { get; set; } = "";
    public string TransactionCode { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Module { get; set; } = "";
    public string SubModule { get; set; } = "";
    public bool IsImmutable { get; set; } = true;
    public string WitnessSignature { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 1.1: Universal Journal Table (FI + CO Merge - like SAP ACDOCA)
// ══════════════════════════════════════════════════════════════════════════════

public class UniversalJournalEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public int FiscalYear { get; set; }
    public int Period { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public DateTime PostingDate { get; set; }
    public int LineNumber { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string AccountType { get; set; } = "";
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public decimal AmountLC { get; set; }
    public string CostCenter { get; set; } = "";
    public string CostElement { get; set; } = "";
    public string ProfitCenter { get; set; } = "";
    public string InternalOrder { get; set; } = "";
    public string BusinessArea { get; set; } = "";
    public string Plant { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string VendorCode { get; set; } = "";
    public string ProjectCode { get; set; } = "";
    public string TaskCode { get; set; } = "";
    public string TaxCode { get; set; } = "";
    public decimal TaxAmount { get; set; }
    public bool IntercompanyIndicator { get; set; }
    public string TradingPartner { get; set; } = "";
    public string ReversalDocument { get; set; } = "";
    public bool IsReversal { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime PostedAt { get; set; }
    public string Hash { get; set; } = "";
    public string Status { get; set; } = "Posted";
    public string Reference { get; set; } = "";
    public string Description { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 1.3: Mobile RF Framework for Warehouse Execution
// ══════════════════════════════════════════════════════════════════════════════

public class RFSessionEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string SessionId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string TerminalId { get; set; } = "";
    public string Plant { get; set; } = "";
    public string Warehouse { get; set; } = "";
    public string CurrentBin { get; set; } = "";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = "Active";
    public string DeviceType { get; set; } = "Mobile";
    public string IpAddress { get; set; } = "";
    public string FirmwareVersion { get; set; } = "";
    public int TransactionCount { get; set; }
}

public class RFTransactionEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid SessionId { get; set; }
    public int TransactionId { get; set; }
    public string TransactionType { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public string FromBin { get; set; } = "";
    public string ToBin { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string DocumentReference { get; set; } = "";
    public DateTime ScanTimestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public long DurationMs { get; set; }
}

public class RFMenuItemEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string MenuCode { get; set; } = "";
    public string MenuName { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconClass { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public string RequiredPermission { get; set; } = "";
    public int SequenceOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ParentMenuId { get; set; }
    public string Parameters { get; set; } = "{}";
    public string ValidationRules { get; set; } = "{}";
}

public class RFPickTaskEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string WaveNumber { get; set; } = "";
    public string TaskId { get; set; } = "";
    public int TaskLine { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string SourceBin { get; set; } = "";
    public string DestinationBin { get; set; } = "";
    public decimal RequiredQty { get; set; }
    public decimal PickedQty { get; set; }
    public string BatchNumber { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string UnitOfMeasure { get; set; } = "EA";
    public int Priority { get; set; } = 5;
    public int SequenceOrder { get; set; }
    public string AssignedTo { get; set; } = "";
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Open";
    public string PickMethod { get; set; } = "Single";
    public bool ScanRequired { get; set; } = true;
    public int ScanCount { get; set; }
    public string Notes { get; set; } = "";
}

public class RFCountTaskEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string CycleCountId { get; set; } = "";
    public string TaskId { get; set; } = "";
    public string Bin { get; set; } = "";
    public string StorageType { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercent { get; set; }
    public string CountedBy { get; set; } = "";
    public DateTime? CountedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public bool RequiresRecount { get; set; }
    public string Notes { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 2.1: Wave Pick & Velocity-Based Bin Slotting
// ══════════════════════════════════════════════════════════════════════════════

public class WavePickEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string WaveNumber { get; set; } = "";
    public string WaveName { get; set; } = "";
    public string Description { get; set; } = "";
    public string WaveType { get; set; } = "Standard";
    public DateTime? PlannedPickDate { get; set; }
    public DateTime? ActualPickDate { get; set; }
    public string Warehouse { get; set; } = "";
    public string Plant { get; set; } = "";
    public int TotalLines { get; set; }
    public decimal TotalQuantity { get; set; }
    public int AssignedPickers { get; set; }
    public int ActivePickers { get; set; }
    public string Status { get; set; } = "Planned";
    public int Priority { get; set; } = 5;
    public DateTime? ReleaseTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string Strategy { get; set; } = "Zone";
    public string Notes { get; set; } = "";
}

public class WavePickLineEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid WaveId { get; set; }
    public int LineNumber { get; set; }
    public string SalesOrderNumber { get; set; } = "";
    public string DeliveryNumber { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string SourceBin { get; set; } = "";
    public string DestinationBin { get; set; } = "";
    public decimal RequiredQty { get; set; }
    public decimal PickedQty { get; set; }
    public decimal ShortQty { get; set; }
    public string BatchNumber { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string UOM { get; set; } = "EA";
    public decimal Weight { get; set; }
    public decimal Volume { get; set; }
    public string Zone { get; set; } = "";
    public string Aisle { get; set; } = "";
    public string Rack { get; set; } = "";
    public int PickSequence { get; set; }
    public string Status { get; set; } = "Pending";
    public string PickedBy { get; set; } = "";
    public DateTime? PickedAt { get; set; }
    public string Notes { get; set; } = "";
}

public class VelocitySlottingEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string Warehouse { get; set; } = "";
    public string VelocityClass { get; set; } = "C";
    public decimal ConsumptionQty30Day { get; set; }
    public decimal ConsumptionQty90Day { get; set; }
    public decimal ConsumptionQty365Day { get; set; }
    public decimal ConsumptionValue30Day { get; set; }
    public decimal ConsumptionValue90Day { get; set; }
    public string CurrentBin { get; set; } = "";
    public string CurrentZone { get; set; } = "";
    public string RecommendedBin { get; set; } = "";
    public string RecommendedZone { get; set; } = "";
    public string SlottingRule { get; set; } = "";
    public int PickFrequency { get; set; }
    public decimal PickDensity { get; set; }
    public DateTime? LastPickedAt { get; set; }
    public DateTime? LastReceivedAt { get; set; }
    public string OptimalSlots { get; set; } = "[]";
    public string Status { get; set; } = "Active";
    public DateTime? CalculatedAt { get; set; }
    public string Notes { get; set; } = "";
}

public class BinMasterEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string BinCode { get; set; } = "";
    public string Warehouse { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Aisle { get; set; } = "";
    public string Rack { get; set; } = "";
    public string Level { get; set; } = "";
    public string Position { get; set; } = "";
    public string BinType { get; set; } = "Storage";
    public string StorageType { get; set; } = "Bulk";
    public decimal Capacity { get; set; }
    public decimal CurrentOccupancy { get; set; }
    public decimal WeightCapacity { get; set; }
    public decimal VolumeCapacity { get; set; }
    public string AssignedMaterial { get; set; } = "";
    public string AssignedVelocityClass { get; set; } = "";
    public bool IsPickable { get; set; } = true;
    public bool IsReceivable { get; set; } = true;
    public string Status { get; set; } = "Active";
    public DateTime? LastCycleCountAt { get; set; }
    public string Coordinates { get; set; } = "{}";
    public string Notes { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 2.2: Finite Capacity Production Scheduling (PP/DS)
// ══════════════════════════════════════════════════════════════════════════════

public class FiniteScheduleEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string ScheduleId { get; set; } = "";
    public string ScheduleName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Plant { get; set; } = "";
    public DateTime PlanningHorizonStart { get; set; }
    public DateTime PlanningHorizonEnd { get; set; }
    public string Status { get; set; } = "Draft";
    public string Strategy { get; set; } = "Finite";
    public decimal CapacityUtilizationTarget { get; set; } = 85.0m;
    public int TotalOperations { get; set; }
    public int ScheduledOperations { get; set; }
    public int ConflictsResolved { get; set; }
    public DateTime? CalculatedAt { get; set; }
    public long DurationMs { get; set; }
    public string CreatedBy { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class FiniteScheduleOperationEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid ScheduleId { get; set; }
    public string ProductionOrderNumber { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public int OperationNumber { get; set; }
    public string OperationDescription { get; set; } = "";
    public string WorkCenterCode { get; set; } = "";
    public string WorkCenterName { get; set; } = "";
    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public decimal SetupTimeHrs { get; set; }
    public decimal RunTimeHrs { get; set; }
    public decimal QueueTimeHrs { get; set; }
    public decimal WaitTimeHrs { get; set; }
    public decimal TotalDurationHrs { get; set; }
    public decimal CapacityLoad { get; set; }
    public string SetupGroup { get; set; } = "";
    public string SetupFamily { get; set; } = "";
    public string Dependencies { get; set; } = "[]";
    public bool IsCriticalPath { get; set; }
    public int SequenceNumber { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime? RescheduledFrom { get; set; }
    public string Notes { get; set; } = "";
}

public class CapacityLoadEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string WorkCenterCode { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal AvailableHours { get; set; }
    public decimal PlannedHours { get; set; }
    public decimal ActualHours { get; set; }
    public decimal LoadPercent { get; set; }
    public decimal OverloadHours { get; set; }
    public decimal UtilizationRate { get; set; }
    public string Operations { get; set; } = "[]";
    public decimal Shift1Hours { get; set; }
    public decimal Shift2Hours { get; set; }
    public decimal Shift3Hours { get; set; }
    public decimal MaintenanceHours { get; set; }
    public decimal DowntimeHours { get; set; }
    public string Status { get; set; } = "Normal";
}

public class MaterialAvailabilityEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string MaterialCode { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal RequiredQty { get; set; }
    public decimal AvailableQty { get; set; }
    public decimal ShortageQty { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal InTransitQty { get; set; }
    public string ProductionOrderNumber { get; set; } = "";
    public string OperationNumber { get; set; } = "";
    public bool IsCriticalMaterial { get; set; }
    public DateTime? EarliestAvailDate { get; set; }
    public string Status { get; set; } = "Available";
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 2.3: In-Memory Event-Driven MRP Scheduler
// ══════════════════════════════════════════════════════════════════════════════

public class MrpEventEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public string EventSource { get; set; } = "";
    public string SourceDocumentNumber { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string EventPayload { get; set; } = "{}";
    public int Priority { get; set; } = 5;
    public DateTime? ProcessedAt { get; set; }
    public long ProcessingDurationMs { get; set; }
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string ErrorMessage { get; set; } = "";
    public string CorrelationId { get; set; } = "";
}

public class MrpEventStreamEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string StreamId { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string Plant { get; set; } = "";
    public long EventSequence { get; set; }
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public decimal RunningDemand { get; set; }
    public decimal RunningSupply { get; set; }
    public decimal RunningProjectedBalance { get; set; }
    public DateTime SnapshotDate { get; set; }
    public bool IsSnapshot { get; set; }
}

public class MrpPlanningRunEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string RunId { get; set; } = "";
    public string RunType { get; set; } = "";
    public string Plant { get; set; } = "";
    public string MaterialsScope { get; set; } = "All";
    public string SelectedMaterials { get; set; } = "[]";
    public string TriggerEvent { get; set; } = "";
    public string TriggerEventId { get; set; } = "";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public int MaterialsProcessed { get; set; }
    public int MaterialsPlanned { get; set; }
    public int OrdersCreated { get; set; }
    public int OrdersChanged { get; set; }
    public int OrdersCancelled { get; set; }
    public decimal TotalPlannedOrders { get; set; }
    public decimal TotalPlannedReceipts { get; set; }
    public string Status { get; set; } = "Queued";
    public int ExceptionCount { get; set; }
    public string CreatedBy { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class MrpEventSubscriptionEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string EventType { get; set; } = "";
    public string MaterialCode { get; set; } = "*";
    public string Plant { get; set; } = "*";
    public string SubscriberService { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int DebounceMs { get; set; } = 1000;
    public DateTime? LastTriggeredAt { get; set; }
    public string Notes { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 3.1: Dynamic Multi-Entity Consolidation
// ══════════════════════════════════════════════════════════════════════════════

public class ConsolidationGroupEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string GroupCode { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string Description { get; set; } = "";
    public string FiscalYear { get; set; } = "";
    public string ConsolidationCurrency { get; set; } = "USD";
    public string ExchangeRateMethod { get; set; } = "Closing";
    public string EliminationRules { get; set; } = "{}";
    public string MinorityInterestPolicy { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public string CreatedBy { get; set; } = "";
    public DateTime? CompletedAt { get; set; }
    public string Notes { get; set; } = "";
}

public class ConsolidationEntityEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }
    public string EntityCode { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string EntityCurrency { get; set; } = "";
    public decimal OwnershipPercent { get; set; }
    public bool IsEliminationEntity { get; set; }
    public string ParentEntityCode { get; set; } = "";
    public string Country { get; set; } = "";
    public string AccountingStandard { get; set; } = "IFRS";
    public string ExchangeRateType { get; set; } = "Closing";
    public string Period { get; set; } = "";
    public string FiscalYear { get; set; } = "";
    public decimal LocalCurrencyRevenue { get; set; }
    public decimal LocalCurrencyCost { get; set; }
    public decimal TranslatedRevenue { get; set; }
    public decimal TranslatedCost { get; set; }
    public decimal TranslationDifference { get; set; }
    public string Status { get; set; } = "Submitted";
    public string Notes { get; set; } = "";
}

public class InterCompanyTransactionEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string FromEntityCode { get; set; } = "";
    public string FromEntityName { get; set; } = "";
    public string ToEntityCode { get; set; } = "";
    public string ToEntityName { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public decimal AmountGroupCurrency { get; set; }
    public decimal ExchangeRate { get; set; }
    public DateTime ExchangeRateDate { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateTime PostingDate { get; set; }
    public string Description { get; set; } = "";
    public bool IsEliminated { get; set; }
    public string EliminationDocument { get; set; } = "";
    public string Status { get; set; } = "Open";
    public decimal DiscrepancyAmount { get; set; }
    public string Notes { get; set; } = "";
}

public class ConsolidationEliminationEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }
    public string EliminationType { get; set; } = "";
    public string FromEntityCode { get; set; } = "";
    public string ToEntityCode { get; set; } = "";
    public string FromDocumentNumber { get; set; } = "";
    public string ToDocumentNumber { get; set; } = "";
    public decimal OriginalAmount { get; set; }
    public decimal EliminationAmount { get; set; }
    public string Currency { get; set; } = "";
    public string EliminationDocumentNumber { get; set; } = "";
    public DateTime PostingDate { get; set; }
    public string Notes { get; set; } = "";
    public string Status { get; set; } = "Proposed";
    public string PostedBy { get; set; } = "";
    public DateTime? PostedAt { get; set; }
}

public class CurrencyTranslationEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }
    public string EntityCode { get; set; } = "";
    public string EntityCurrency { get; set; } = "";
    public string GroupCurrency { get; set; } = "";
    public DateTime TranslationDate { get; set; }
    public string AccountType { get; set; } = "";
    public decimal ClosingRate { get; set; }
    public decimal AverageRate { get; set; }
    public decimal HistoricalRate { get; set; }
    public decimal LocalAmount { get; set; }
    public decimal TranslatedAmount { get; set; }
    public decimal TranslationGainLoss { get; set; }
    public string Period { get; set; } = "";
    public string FiscalYear { get; set; } = "";
    public string Status { get; set; } = "Calculated";
    public string Notes { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 3.2: Dynamic Localization Tax Microservice
// ══════════════════════════════════════════════════════════════════════════════

public class LocalizationCountryEntity : EntityBase
{
    public string CountryCode { get; set; } = "";
    public string CountryName { get; set; } = "";
    public string TaxSystem { get; set; } = "";
    public string Currency { get; set; } = "";
    public int CurrencyDecimals { get; set; } = 2;
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string NumberFormat { get; set; } = "";
    public string FiscalYearStart { get; set; } = "";
    public string TaxRegistrationLabel { get; set; } = "";
    public bool IsSupported { get; set; } = true;
    public string SupportedVersion { get; set; } = "1.0";
    public string LocalizationConfig { get; set; } = "{}";
    public string Notes { get; set; } = "";
}

public class LocalizationTaxConfigEntity : EntityBase
{
    public string CountryCode { get; set; } = "";
    public string TaxType { get; set; } = "";
    public string TaxCode { get; set; } = "";
    public string TaxName { get; set; } = "";
    public string TaxDescription { get; set; } = "";
    public decimal Rate { get; set; }
    public bool IsCompound { get; set; }
    public string CalculationMethod { get; set; } = "Percentage";
    public bool InclusiveOfTax { get; set; }
    public string RoundingRule { get; set; } = "Standard";
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string Conditions { get; set; } = "{}";
    public string Notes { get; set; } = "";
}

public class TaxReturnEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string CountryCode { get; set; } = "";
    public string TaxType { get; set; } = "";
    public string Period { get; set; } = "";
    public string FiscalYear { get; set; } = "";
    public string ReturnPeriod { get; set; } = "";
    public string ReturnNumber { get; set; } = "";
    public decimal TotalTaxableSales { get; set; }
    public decimal TotalOutputTax { get; set; }
    public decimal TotalTaxablePurchases { get; set; }
    public decimal TotalInputTax { get; set; }
    public decimal NetTaxPayable { get; set; }
    public decimal NetTaxRefund { get; set; }
    public DateTime FilingDueDate { get; set; }
    public DateTime? FilingDate { get; set; }
    public DateTime? PaymentDueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string FilingReference { get; set; } = "";
    public DateTime CalculatedAt { get; set; }
    public string Notes { get; set; } = "";
}

public class WithholdingTaxEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string CountryCode { get; set; } = "";
    public string VendorCode { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string WHTType { get; set; } = "";
    public string SectionCode { get; set; } = "";
    public string SectionDescription { get; set; } = "";
    public decimal PaymentAmount { get; set; }
    public decimal WHTRate { get; set; }
    public decimal WHTAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime DeductionDate { get; set; }
    public string PaymentVoucherNumber { get; set; } = "";
    public string ChallanNumber { get; set; } = "";
    public DateTime? ChallanDate { get; set; }
    public string Status { get; set; } = "Deducted";
    public string FinancialYear { get; set; } = "";
    public string Quarter { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 3.3: Embedded AI API Gateway (Document OCR & Predictive Analytics)
// ══════════════════════════════════════════════════════════════════════════════

public class AiDocumentOcrEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string DocumentId { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string FileHash { get; set; } = "";
    public string ExtractedData { get; set; } = "{}";
    public decimal ConfidenceScore { get; set; }
    public string OcrProvider { get; set; } = "";
    public long ProcessingTimeMs { get; set; }
    public string Status { get; set; } = "Uploaded";
    public DateTime? ExtractedAt { get; set; }
    public string ReviewedBy { get; set; } = "";
    public DateTime? ReviewedAt { get; set; }
    public bool IsVerified { get; set; }
    public string ExtractionErrors { get; set; } = "[]";
    public string Notes { get; set; } = "";
}

public class AiDocumentTemplateEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string TemplateCode { get; set; } = "";
    public string TemplateName { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string Description { get; set; } = "";
    public string Fields { get; set; } = "[]";
    public string ValidationRules { get; set; } = "{}";
    public string MappingRules { get; set; } = "{}";
    public decimal ConfidenceThreshold { get; set; } = 0.85m;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public string SampleDocumentUrl { get; set; } = "";
}

public class AiPredictiveModelEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string ModelCode { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string ModelType { get; set; } = "";
    public string Description { get; set; } = "";
    public string TrainingDataRange { get; set; } = "";
    public string Features { get; set; } = "[]";
    public decimal Accuracy { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1Score { get; set; }
    public string TrainingStatus { get; set; } = "NotStarted";
    public DateTime? LastTrainedAt { get; set; }
    public DateTime? NextRetrainAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsProduction { get; set; }
    public string ModelParameters { get; set; } = "{}";
    public string ModelBinaryPath { get; set; } = "";
    public int Version { get; set; } = 1;
}

public class AiForecastEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public Guid ModelId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string Warehouse { get; set; } = "";
    public DateTime ForecastDate { get; set; }
    public int ForecastHorizonDays { get; set; }
    public string DailyForecasts { get; set; } = "[]";
    public decimal TotalForecastQty { get; set; }
    public decimal ForecastAccuracy { get; set; }
    public decimal ActualQty { get; set; }
    public decimal Bias { get; set; }
    public string Method { get; set; } = "";
    public string Status { get; set; } = "Generated";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string ReviewedBy { get; set; } = "";
    public DateTime? ReviewedAt { get; set; }
    public string Notes { get; set; } = "";
}

public class AiAnomalyEntity : EntityBase
{
    public Guid TenantId { get; set; }
    public string AnomalyId { get; set; } = "";
    public string AnomalyType { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string EntityName { get; set; } = "";
    public decimal DetectedValue { get; set; }
    public decimal ExpectedValue { get; set; }
    public decimal DeviationPercent { get; set; }
    public string Severity { get; set; } = "Medium";
    public decimal ConfidenceScore { get; set; }
    public string DetectionMethod { get; set; } = "";
    public string ModelId { get; set; } = "";
    public DateTime DetectionDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Detected";
    public string InvestigatedBy { get; set; } = "";
    public DateTime? InvestigatedAt { get; set; }
    public string ResolutionNotes { get; set; } = "";
    public string RootCause { get; set; } = "";
    public string RecommendedAction { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Workflow step tracking for complaint/return lifecycle.
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Workflow step tracking for complaint/return lifecycle.
/// </summary>
public class ComplaintWorkflowStepEntity : EntityBase
{
    public Guid ComplaintReturnId { get; set; }
    public string StepName { get; set; } = "";
    public string StepCode { get; set; } = "";
    public int StepOrder { get; set; }
    public string Module { get; set; } = "";
    public string TransactionCode { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Status { get; set; } = "PENDING";
    public string AssignedTo { get; set; } = "";
    public string CompletedBy { get; set; } = "";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Notes { get; set; } = "";
    public bool IsRequired { get; set; } = true;
    public bool IsAutomated { get; set; } = false;
}
