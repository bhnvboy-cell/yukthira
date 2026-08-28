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
