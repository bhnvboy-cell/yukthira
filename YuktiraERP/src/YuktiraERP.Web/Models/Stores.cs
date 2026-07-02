using System.Collections.Generic;
using System.Linq;

namespace YuktiraERP.Web.Models;

public static class MaterialStore
{
    private static readonly List<MaterialMaster> _materials = new()
    {
        new() { Code = "FG-001", Name = "Finished Product A", Type = "FINISHED", UOM = "EA", Stock = 500, Price = 25.00m, Status = "Active" },
        new() { Code = "RM-001", Name = "Raw Material X", Type = "RAW", UOM = "KG", Stock = 1200, Price = 5.50m, Status = "Active" },
        new() { Code = "RM-002", Name = "Raw Material Y", Type = "RAW", UOM = "KG", Stock = 300, Price = 8.20m, Status = "Low Stock" },
        new() { Code = "PK-001", Name = "Packaging Box", Type = "PACKAGING", UOM = "EA", Stock = 5000, Price = 0.50m, Status = "Active" },
    };
    public static List<MaterialMaster> GetAll() => _materials.ToList();
    public static void Add(MaterialMaster m) => _materials.Add(m);
    public static void Delete(string code) => _materials.RemoveAll(x => x.Code == code);
}

public static class VendorStore
{
    private static readonly List<Vendor> _items = new()
    {
        new() { Code = "VEN-001", Name = "ABC Supplies Ltd.", TaxId = "TX-12345", PaymentTerms = "Net 30", Phone = "+1-555-0201", Status = "Active" },
        new() { Code = "VEN-002", Name = "GlobalChem Industries", TaxId = "TX-12346", PaymentTerms = "Net 45", Phone = "+1-555-0202", Status = "Active" },
        new() { Code = "VEN-003", Name = "PackRight Corp.", TaxId = "TX-12347", PaymentTerms = "Net 30", Phone = "+1-555-0203", Status = "On Hold" },
    };
    public static List<Vendor> GetAll() => _items.ToList();
    public static void Add(Vendor v) => _items.Add(v);
    public static void Delete(string code) => _items.RemoveAll(x => x.Code == code);
}

public static class PurchaseRequisitionStore
{
    private static readonly List<PurchaseRequisition> _items = new()
    {
        new() { PrNumber = "PR-2024-001", Date = new(2024,1,20), Requestor = "John Doe", ItemName = "Raw Material X", Quantity = "500 KG", Amount = 2750m, Status = "Pending" },
        new() { PrNumber = "PR-2024-002", Date = new(2024,1,22), Requestor = "Jane Smith", ItemName = "Packaging Box", Quantity = "2000 EA", Amount = 1000m, Status = "Approved" },
        new() { PrNumber = "PR-2024-003", Date = new(2024,1,25), Requestor = "Mike Brown", ItemName = "Lab Equipment", Quantity = "1 EA", Amount = 12500m, Status = "In Review" },
    };
    public static List<PurchaseRequisition> GetAll() => _items.ToList();
    public static void Add(PurchaseRequisition r) => _items.Add(r);
    public static void Delete(string pr) => _items.RemoveAll(x => x.PrNumber == pr);
}

public static class PurchaseOrderStore
{
    private static readonly List<PurchaseOrder> _items = new()
    {
        new() { PoNumber = "PO-2024-0056", Date = new(2024,1,25), VendorName = "ABC Supplies Ltd.", ItemName = "Raw Material X", Quantity = "500 KG", Amount = 2750m, Status = "Approved" },
        new() { PoNumber = "PO-2024-0057", Date = new(2024,1,26), VendorName = "PackRight Corp.", ItemName = "Packaging Box", Quantity = "5000 EA", Amount = 2500m, Status = "Pending Approval" },
        new() { PoNumber = "PO-2024-0058", Date = new(2024,1,28), VendorName = "GlobalChem Industries", ItemName = "Raw Material Y", Quantity = "1000 KG", Amount = 8200m, Status = "Partially Received" },
    };
    public static List<PurchaseOrder> GetAll() => _items.ToList();
    public static void Add(PurchaseOrder o) => _items.Add(o);
    public static void Delete(string po) => _items.RemoveAll(x => x.PoNumber == po);
}

public static class GoodsReceiptStore
{
    private static readonly List<GoodsReceipt> _items = new()
    {
        new() { GrnNumber = "GRN-2024-001", Date = new(2024,1,30), PoNumber = "PO-2024-0056", MaterialName = "Raw Material X", QtyReceived = "500 KG", QtyAccepted = "490 KG", Status = "Posted" },
        new() { GrnNumber = "GRN-2024-002", Date = new(2024,1,31), PoNumber = "PO-2024-0058", MaterialName = "Raw Material Y", QtyReceived = "500 KG", QtyAccepted = "500 KG", Status = "Inspection Pending" },
    };
    public static List<GoodsReceipt> GetAll() => _items.ToList();
    public static void Add(GoodsReceipt g) => _items.Add(g);
    public static void Delete(string grn) => _items.RemoveAll(x => x.GrnNumber == grn);
}

public static class StockItemStore
{
    private static readonly List<StockItem> _items = new()
    {
        new() { Bin = "A-01-01", MaterialName = "Raw Material X", Lot = "LOT-2401", Quantity = 1200, UOM = "KG", Value = 6600m, MinStock = 500, MaxStock = 2000 },
        new() { Bin = "A-01-02", MaterialName = "Raw Material Y", Lot = "LOT-2402", Quantity = 300, UOM = "KG", Value = 2460m, MinStock = 400, MaxStock = 1500 },
        new() { Bin = "B-02-01", MaterialName = "Finished Product A", Lot = "LOT-2312", Quantity = 500, UOM = "EA", Value = 12500m, MinStock = 200, MaxStock = 1000 },
        new() { Bin = "C-03-01", MaterialName = "Packaging Box", Lot = "LOT-2401", Quantity = 5000, UOM = "EA", Value = 2500m, MinStock = 1000, MaxStock = 10000 },
    };
    public static List<StockItem> GetAll() => _items.ToList();
    public static void Add(StockItem s) => _items.Add(s);
    public static void Delete(string bin) => _items.RemoveAll(x => x.Bin == bin);
}

public static class CustomerStore
{
    private static readonly List<Customer> _items = new()
    {
        new() { Code = "CUST-001", Name = "Acme Corporation", CreditLimit = 100000, PaymentTerms = "Net 30", Phone = "+1-555-0301", Status = "Active" },
        new() { Code = "CUST-002", Name = "Globex Industries", CreditLimit = 250000, PaymentTerms = "Net 45", Phone = "+1-555-0302", Status = "Active" },
        new() { Code = "CUST-003", Name = "Innotech Solutions", CreditLimit = 50000, PaymentTerms = "Net 30", Phone = "+1-555-0303", Status = "Credit Hold" },
    };
    public static List<Customer> GetAll() => _items.ToList();
    public static void Add(Customer c) => _items.Add(c);
    public static void Delete(string code) => _items.RemoveAll(x => x.Code == code);
}

public static class SalesOrderStore
{
    private static readonly List<SalesOrder> _items = new()
    {
        new() { OrderNumber = "SO-2024-001", CustomerName = "Acme Corporation", OrderDate = new(2024,1,20), ItemCount = 3, Amount = 12500m, Status = "Confirmed" },
        new() { OrderNumber = "SO-2024-002", CustomerName = "Globex Industries", OrderDate = new(2024,1,22), ItemCount = 1, Amount = 25000m, Status = "In Delivery" },
    };
    public static List<SalesOrder> GetAll() => _items.ToList();
    public static void Add(SalesOrder s) => _items.Add(s);
    public static void Delete(string num) => _items.RemoveAll(x => x.OrderNumber == num);
}

public static class ProductionPlanStore
{
    private static readonly List<ProductionPlan> _items = new()
    {
        new() { PlanId = "PP-2024-001", ProductName = "Finished Product A", Quantity = 1000, StartDate = new(2024,2,1), EndDate = new(2024,2,15), Status = "Released" },
        new() { PlanId = "PP-2024-002", ProductName = "Finished Product B", Quantity = 500, StartDate = new(2024,2,10), EndDate = new(2024,2,20), Status = "Planned" },
        new() { PlanId = "PP-2024-003", ProductName = "Sub-Assembly X", Quantity = 2000, StartDate = new(2024,2,5), EndDate = new(2024,2,12), Status = "In Progress" },
    };
    public static List<ProductionPlan> GetAll() => _items.ToList();
    public static void Add(ProductionPlan p) => _items.Add(p);
    public static void Delete(string id) => _items.RemoveAll(x => x.PlanId == id);
}

public static class InspectionLotStore
{
    private static readonly List<InspectionLot> _items = new()
    {
        new() { LotNumber = "IQ-2024-001", MaterialName = "Raw Material X", Quantity = "500 KG", Inspected = 50, Passed = 48, Failed = 2, Status = "Accepted" },
        new() { LotNumber = "IQ-2024-002", MaterialName = "Finished Product A", Quantity = "200 EA", Inspected = 20, Passed = 18, Failed = 2, Status = "Pending Review" },
        new() { LotNumber = "IQ-2024-003", MaterialName = "Packaging Box", Quantity = "1000 EA", Inspected = 100, Passed = 100, Failed = 0, Status = "Accepted" },
    };
    public static List<InspectionLot> GetAll() => _items.ToList();
    public static void Add(InspectionLot l) => _items.Add(l);
    public static void Delete(string id) => _items.RemoveAll(x => x.LotNumber == id);
}

public static class EmployeeStore
{
    private static readonly List<Employee> _items = new()
    {
        new() { Code = "EMP-001", Name = "John Doe", Department = "Production", Designation = "Plant Manager", Mobile = "+1-555-0101", Status = "Active" },
        new() { Code = "EMP-002", Name = "Jane Smith", Department = "Quality", Designation = "QC Supervisor", Mobile = "+1-555-0102", Status = "Active" },
        new() { Code = "EMP-003", Name = "Ahmed Khan", Department = "Warehouse", Designation = "Store Keeper", Mobile = "+1-555-0103", Status = "Active" },
        new() { Code = "EMP-004", Name = "Lisa Chen", Department = "Finance", Designation = "Accountant", Mobile = "+1-555-0104", Status = "On Leave" },
        new() { Code = "EMP-005", Name = "Raj Patel", Department = "IT", Designation = "System Admin", Mobile = "+1-555-0105", Status = "Active" },
    };
    public static List<Employee> GetAll() => _items.ToList();
    public static void Add(Employee e) => _items.Add(e);
    public static void Delete(string code) => _items.RemoveAll(x => x.Code == code);
}

public static class LeadStore
{
    private static readonly List<Lead> _items = new()
    {
        new() { LeadId = "LD-001", Company = "TechCorp Inc.", Contact = "Sarah Lee", Source = "Website", Value = 75000, Status = "New" },
        new() { LeadId = "LD-002", Company = "GreenFields Ltd.", Contact = "Mike Brown", Source = "Referral", Value = 120000, Status = "Qualified" },
        new() { LeadId = "LD-003", Company = "MedSupply Co.", Contact = "Dr. Anna White", Source = "Trade Show", Value = 200000, Status = "Converted" },
    };
    public static List<Lead> GetAll() => _items.ToList();
    public static void Add(Lead l) => _items.Add(l);
    public static void Delete(string id) => _items.RemoveAll(x => x.LeadId == id);
}

public static class SampleStore
{
    private static readonly List<Sample> _items = new()
    {
        new() { SampleId = "SMP-2401", MaterialName = "Raw Material X", Source = "Incoming", CollectionDate = new(2024,1,15), TestCount = 3, Status = "Completed" },
        new() { SampleId = "SMP-2402", MaterialName = "Finished Product A", Source = "Production", CollectionDate = new(2024,1,16), TestCount = 5, Status = "In Progress" },
        new() { SampleId = "SMP-2403", MaterialName = "Water Sample", Source = "Effluent", CollectionDate = new(2024,1,17), TestCount = 2, Status = "Submitted" },
    };
    public static List<Sample> GetAll() => _items.ToList();
    public static void Add(Sample s) => _items.Add(s);
    public static void Delete(string id) => _items.RemoveAll(x => x.SampleId == id);
}

public static class JournalEntryStore
{
    private static readonly List<JournalEntry> _items = new()
    {
        new() { DocumentNumber = "GL-2024-001", EntryDate = new(2024,1,31), Account = "1100 - Cash", Debit = 50000m, Reference = "Invoice INV-001" },
        new() { DocumentNumber = "GL-2024-001", EntryDate = new(2024,1,31), Account = "4100 - Revenue", Credit = 50000m, Reference = "Invoice INV-001" },
        new() { DocumentNumber = "GL-2024-002", EntryDate = new(2024,2,1), Account = "5100 - Raw Materials", Debit = 12500m, Reference = "PO-2024-001" },
        new() { DocumentNumber = "GL-2024-002", EntryDate = new(2024,2,1), Account = "2100 - Accounts Payable", Credit = 12500m, Reference = "PO-2024-001" },
    };
    public static List<JournalEntry> GetAll() => _items.ToList();
    public static void Add(JournalEntry j) => _items.Add(j);
    public static void Delete(string doc) => _items.RemoveAll(x => x.DocumentNumber == doc);
}

public static class ApprovalRequestStore
{
    private static readonly List<ApprovalRequest> _items = new()
    {
        new() { RequestId = "APR-001", Type = "Purchase Order", Subject = "PO-2024-0056 - Raw Materials", Requestor = "John Doe", RequestDate = new(2024,1,31), Amount = 25000m, Status = "Pending" },
        new() { RequestId = "APR-002", Type = "Leave", Subject = "Annual Leave - Jane Smith", Requestor = "Jane Smith", RequestDate = new(2024,1,30), Status = "Pending" },
        new() { RequestId = "APR-003", Type = "Capital Expense", Subject = "New QC Equipment", Requestor = "Mike Brown", RequestDate = new(2024,1,29), Amount = 45000m, Status = "Pending" },
        new() { RequestId = "APR-004", Type = "Invoice", Subject = "INV-2024-0089 - IT Services", Requestor = "Lisa Chen", RequestDate = new(2024,1,28), Amount = 5500m, Status = "Pending" },
    };
    public static List<ApprovalRequest> GetAll() => _items.ToList();
    public static void Add(ApprovalRequest a) => _items.Add(a);
    public static void Delete(string id) => _items.RemoveAll(x => x.RequestId == id);
    public static void Approve(string id) { var a = _items.FirstOrDefault(x => x.RequestId == id); if (a != null) a.Status = "Approved"; }
    public static void Reject(string id) { var a = _items.FirstOrDefault(x => x.RequestId == id); if (a != null) a.Status = "Rejected"; }
}

public static class CustomFieldStore
{
    private static readonly List<CustomField> _items = new()
    {
        new() { Module = "MM", EntityName = "Material Master", FieldName = "Lead Time (Days)", FieldType = "Number", IsRequired = true, DefaultValue = "0" },
        new() { Module = "SD", EntityName = "Customer", FieldName = "Payment Terms", FieldType = "Dropdown", IsRequired = true, DefaultValue = "Net 30" },
        new() { Module = "PP", EntityName = "Production Order", FieldName = "Quality Hold", FieldType = "Checkbox", DefaultValue = "False" },
        new() { Module = "HR", EntityName = "Employee", FieldName = "Emergency Contact", FieldType = "Text", IsRequired = true },
    };
    public static List<CustomField> GetAll() => _items.ToList();
    public static void Add(CustomField c) => _items.Add(c);
    public static void Delete(Guid id) { }
}

public static class AdminUserStore
{
    private static readonly List<AdminUser> _items = new()
    {
        new() { UserId = "USR-001", UserName = "admin", Email = "admin@yuktira.com", Role = "SUPER_USER", IsActive = true },
        new() { UserId = "USR-002", UserName = "jdoe", Email = "jdoe@yuktira.com", Role = "POWER_USER", IsActive = true },
        new() { UserId = "USR-003", UserName = "asmith", Email = "asmith@yuktira.com", Role = "READ_ONLY", IsActive = false },
    };
    public static List<AdminUser> GetAll() => _items.ToList();
    public static void Add(AdminUser u) => _items.Add(u);
    public static void Delete(string id) => _items.RemoveAll(x => x.UserId == id);
}

public static class TenantStore
{
    private static readonly List<Tenant> _items = new()
    {
        new() { TenantId = "TNT-001", Name = "Demo Company", Subdomain = "demo", Status = "Active" },
        new() { TenantId = "TNT-002", Name = "Test Corp", Subdomain = "test", Status = "Active" },
        new() { TenantId = "TNT-003", Name = "Dev Instance", Subdomain = "dev", Status = "Inactive" },
    };
    public static List<Tenant> GetAll() => _items.ToList();
    public static void Add(Tenant t) => _items.Add(t);
    public static void Delete(string id) => _items.RemoveAll(x => x.TenantId == id);
}

public static class SystemConfigStore
{
    private static readonly List<SystemConfig> _items = new()
    {
        new() { Key = "app.name", Value = "Yuktira ERP Suite", Description = "Application Name", Module = "Global" },
        new() { Key = "app.version", Value = "1.0.0", Description = "Application Version", Module = "Global" },
        new() { Key = "auth.max_login_attempts", Value = "5", Description = "Max login attempts before lockout", Module = "Auth" },
        new() { Key = "auth.password_min_length", Value = "8", Description = "Minimum password length", Module = "Auth" },
        new() { Key = "email.smtp_host", Value = "smtp.yuktira.com", Description = "SMTP Server Host", Module = "Email" },
        new() { Key = "email.smtp_port", Value = "587", Description = "SMTP Server Port", Module = "Email" },
        new() { Key = "features.enable_mfa", Value = "false", Description = "Enable Multi-Factor Authentication", Module = "Features" },
        new() { Key = "features.enable_audit", Value = "true", Description = "Enable Audit Logging", Module = "Features" },
    };
    public static List<SystemConfig> GetAll() => _items.ToList();
    public static void Add(SystemConfig c) => _items.Add(c);
    public static void Update(string key, string value) { var c = _items.FirstOrDefault(x => x.Key == key); if (c != null) c.Value = value; }
}

public static class BillOfMaterialStore
{
    private static readonly List<BillOfMaterial> _items = new()
    {
        new() { BomId = "BOM-001", ProductName = "Finished Product A", ComponentName = "Raw Material X", Quantity = 2, UOM = "KG", Status = "Active" },
        new() { BomId = "BOM-002", ProductName = "Finished Product A", ComponentName = "Packaging Box", Quantity = 1, UOM = "EA", Status = "Active" },
    };
    public static List<BillOfMaterial> GetAll() => _items.ToList();
    public static void Add(BillOfMaterial b) => _items.Add(b);
    public static void Delete(string id) => _items.RemoveAll(x => x.BomId == id);
}

public static class ProductionRoutingStore
{
    private static readonly List<ProductionRouting> _items = new()
    {
        new() { RoutingId = "RTG-001", ProductName = "Finished Product A", OperationNo = 10, WorkCenter = "Mixer-1", SetupTimeHrs = 0.5m, RunTimeHrs = 2.0m, Status = "Active" },
        new() { RoutingId = "RTG-002", ProductName = "Finished Product A", OperationNo = 20, WorkCenter = "Packer-1", SetupTimeHrs = 0.3m, RunTimeHrs = 1.5m, Status = "Active" },
    };
    public static List<ProductionRouting> GetAll() => _items.ToList();
    public static void Add(ProductionRouting r) => _items.Add(r);
    public static void Delete(string id) => _items.RemoveAll(x => x.RoutingId == id);
}

public static class InspectionPlanStore
{
    private static readonly List<InspectionPlan> _items = new()
    {
        new() { PlanId = "IP-001", MaterialName = "Raw Material X", Characteristic = "Purity", Method = "HPLC", Frequency = "Each Lot", Status = "Active" },
    };
    public static List<InspectionPlan> GetAll() => _items.ToList();
    public static void Add(InspectionPlan p) => _items.Add(p);
    public static void Delete(string id) => _items.RemoveAll(x => x.PlanId == id);
}

public static class WarehouseTransferStore
{
    private static readonly List<WarehouseTransfer> _items = new()
    {
        new() { TransferId = "TRF-001", Date = new(2024,2,1), MaterialName = "Raw Material X", FromBin = "A-01-01", ToBin = "B-02-01", Quantity = 100, Status = "Completed" },
    };
    public static List<WarehouseTransfer> GetAll() => _items.ToList();
    public static void Add(WarehouseTransfer t) => _items.Add(t);
    public static void Delete(string id) => _items.RemoveAll(x => x.TransferId == id);
}

public static class APEntryStore
{
    private static readonly List<APEntry> _items = new()
    {
        new() { DocumentNumber = "AP-001", Date = new(2024,2,1), VendorName = "ABC Supplies Ltd.", Amount = 2750m, PaidAmount = 0, Status = "Open" },
    };
    public static List<APEntry> GetAll() => _items.ToList();
    public static void Add(APEntry e) => _items.Add(e);
    public static void Delete(string id) => _items.RemoveAll(x => x.DocumentNumber == id);
}

public static class AREntryStore
{
    private static readonly List<AREntry> _items = new()
    {
        new() { DocumentNumber = "AR-001", Date = new(2024,2,1), CustomerName = "Acme Corporation", Amount = 12500m, ReceivedAmount = 12500m, Status = "Paid" },
    };
    public static List<AREntry> GetAll() => _items.ToList();
    public static void Add(AREntry e) => _items.Add(e);
    public static void Delete(string id) => _items.RemoveAll(x => x.DocumentNumber == id);
}

public static class LeaveRequestStore
{
    private static readonly List<LeaveRequest> _items = new()
    {
        new() { LeaveId = "LV-001", EmployeeName = "John Doe", LeaveType = "Annual", StartDate = new(2024,3,1), EndDate = new(2024,3,5), Status = "Approved" },
    };
    public static List<LeaveRequest> GetAll() => _items.ToList();
    public static void Add(LeaveRequest l) => _items.Add(l);
    public static void Delete(string id) => _items.RemoveAll(x => x.LeaveId == id);
}

public static class PayrollEntryStore
{
    private static readonly List<PayrollEntry> _items = new()
    {
        new() { PayrollId = "PRL-001", EmployeeName = "John Doe", Period = "January 2024", GrossPay = 5000m, Deductions = 1250m, NetPay = 3750m, Status = "Posted" },
    };
    public static List<PayrollEntry> GetAll() => _items.ToList();
    public static void Add(PayrollEntry p) => _items.Add(p);
    public static void Delete(string id) => _items.RemoveAll(x => x.PayrollId == id);
}

public static class OpportunityStore
{
    private static readonly List<Opportunity> _items = new()
    {
        new() { OppId = "OPP-001", OpportunityName = "ERP Software Deal", Company = "TechCorp Inc.", Value = 150000, Stage = "Negotiation", Status = "Open" },
    };
    public static List<Opportunity> GetAll() => _items.ToList();
    public static void Add(Opportunity o) => _items.Add(o);
    public static void Delete(string id) => _items.RemoveAll(x => x.OppId == id);
}

public static class TestResultStore
{
    private static readonly List<TestResult> _items = new()
    {
        new() { ResultId = "TR-001", SampleId = "SMP-2401", TestName = "Purity Assay", Result = "99.5%", Specification = ">99.0%", Status = "Passed" },
    };
    public static List<TestResult> GetAll() => _items.ToList();
    public static void Add(TestResult t) => _items.Add(t);
    public static void Delete(string id) => _items.RemoveAll(x => x.ResultId == id);
}

public static class InvoiceVerificationStore
{
    private static readonly List<InvoiceVerification> _items = new()
    {
        new() { InvoiceNumber = "INV-24001", Date = new(2024,2,5), PoNumber = "PO-2024-0056", VendorName = "ABC Supplies Ltd.", Amount = 2750m, MatchedAmount = 2750m, Status = "Matched" },
        new() { InvoiceNumber = "INV-24002", Date = new(2024,2,10), PoNumber = "PO-2024-0057", VendorName = "PackRight Corp.", Amount = 2500m, MatchedAmount = 2400m, Status = "Variance" },
    };
    public static List<InvoiceVerification> GetAll() => _items.ToList();
    public static void Add(InvoiceVerification v) => _items.Add(v);
    public static void Delete(string id) => _items.RemoveAll(x => x.InvoiceNumber == id);
}

public static class InquiryStore
{
    private static readonly List<Inquiry> _items = new()
    {
        new() { InquiryNumber = "INQ-24001", Date = new(2024,1,10), CustomerName = "Acme Corporation", Description = "Price quote for raw materials", Status = "Open" },
        new() { InquiryNumber = "INQ-24002", Date = new(2024,1,15), CustomerName = "Globex Industries", Description = "Packaging material enquiry", Status = "Quoted" },
    };
    public static List<Inquiry> GetAll() => _items.ToList();
    public static void Add(Inquiry i) => _items.Add(i);
    public static void Delete(string id) => _items.RemoveAll(x => x.InquiryNumber == id);
}

public static class QuotationStore
{
    private static readonly List<Quotation> _items = new()
    {
        new() { QuoteNumber = "QTE-24001", Date = new(2024,1,12), CustomerName = "Acme Corporation", Amount = 27500m, ValidUntil = new(2024,3,12), Status = "Sent" },
        new() { QuoteNumber = "QTE-24002", Date = new(2024,1,18), CustomerName = "Globex Industries", Amount = 15000m, ValidUntil = new(2024,3,18), Status = "Draft" },
    };
    public static List<Quotation> GetAll() => _items.ToList();
    public static void Add(Quotation q) => _items.Add(q);
    public static void Delete(string id) => _items.RemoveAll(x => x.QuoteNumber == id);
}

public static class DeliveryStore
{
    private static readonly List<Delivery> _items = new()
    {
        new() { DeliveryNumber = "DN-24001", Date = new(2024,2,1), SoNumber = "SO-2024-001", CustomerName = "Acme Corporation", Status = "Delivered" },
        new() { DeliveryNumber = "DN-24002", Date = new(2024,2,5), SoNumber = "SO-2024-002", CustomerName = "Globex Industries", Status = "Picked" },
    };
    public static List<Delivery> GetAll() => _items.ToList();
    public static void Add(Delivery d) => _items.Add(d);
    public static void Delete(string id) => _items.RemoveAll(x => x.DeliveryNumber == id);
}

public static class BillingDocumentStore
{
    private static readonly List<BillingDocument> _items = new()
    {
        new() { DocumentNumber = "INV-24001", Date = new(2024,2,1), SoNumber = "SO-2024-001", CustomerName = "Acme Corporation", Amount = 12500m, Status = "Paid" },
        new() { DocumentNumber = "INV-24002", Date = new(2024,2,5), SoNumber = "SO-2024-002", CustomerName = "Globex Industries", Amount = 25000m, Status = "Unpaid" },
    };
    public static List<BillingDocument> GetAll() => _items.ToList();
    public static void Add(BillingDocument b) => _items.Add(b);
    public static void Delete(string id) => _items.RemoveAll(x => x.DocumentNumber == id);
}

public static class WorkCenterStore
{
    private static readonly List<WorkCenter> _items = new()
    {
        new() { Code = "WC-001", Name = "Mixer Station 1", Department = "Production", CapacityPerShift = 500, Status = "Active" },
        new() { Code = "WC-002", Name = "Packer Line A", Department = "Packaging", CapacityPerShift = 2000, Status = "Active" },
        new() { Code = "WC-003", Name = "QC Lab Bench 1", Department = "Quality", CapacityPerShift = 100, Status = "Maintenance" },
    };
    public static List<WorkCenter> GetAll() => _items.ToList();
    public static void Add(WorkCenter w) => _items.Add(w);
    public static void Delete(string id) => _items.RemoveAll(x => x.Code == id);
}

public static class ProductionOrderStore
{
    private static readonly List<ProductionOrder> _items = new()
    {
        new() { OrderNumber = "MO-24001", ProductName = "Finished Product A", Quantity = 1000, StartDate = new(2024,2,1), EndDate = new(2024,2,10), Status = "In Progress" },
        new() { OrderNumber = "MO-24002", ProductName = "Sub-Assembly X", Quantity = 500, StartDate = new(2024,2,5), EndDate = new(2024,2,8), Status = "Planned" },
    };
    public static List<ProductionOrder> GetAll() => _items.ToList();
    public static void Add(ProductionOrder o) => _items.Add(o);
    public static void Delete(string id) => _items.RemoveAll(x => x.OrderNumber == id);
}

public static class InspectionResultStore
{
    private static readonly List<InspectionResult> _items = new()
    {
        new() { ResultId = "IR-24001", LotNumber = "IQ-2024-001", Characteristic = "Purity", Result = "99.5%", Specification = ">99.0%", Status = "Passed" },
        new() { ResultId = "IR-24002", LotNumber = "IQ-2024-001", Characteristic = "Moisture", Result = "2.1%", Specification = "<3.0%", Status = "Passed" },
        new() { ResultId = "IR-24003", LotNumber = "IQ-2024-003", Characteristic = "Dimension", Result = "48.5cm", Specification = "50±2cm", Status = "Passed" },
    };
    public static List<InspectionResult> GetAll() => _items.ToList();
    public static void Add(InspectionResult r) => _items.Add(r);
    public static void Delete(string id) => _items.RemoveAll(x => x.ResultId == id);
}

public static class UsageDecisionStore
{
    private static readonly List<UsageDecision> _items = new()
    {
        new() { DecisionId = "UD-24001", LotNumber = "IQ-2024-001", MaterialName = "Raw Material X", Decision = "Accept - Meets spec", Notes = "All parameters within limits", DecisionDate = new(2024,1,20) },
        new() { DecisionId = "UD-24002", LotNumber = "IQ-2024-003", MaterialName = "Packaging Box", Decision = "Accept - Minor deviation", Notes = "Dimension within tolerance", DecisionDate = new(2024,1,25) },
    };
    public static List<UsageDecision> GetAll() => _items.ToList();
    public static void Add(UsageDecision d) => _items.Add(d);
    public static void Delete(string id) => _items.RemoveAll(x => x.DecisionId == id);
}

public static class StorageLocationStore
{
    private static readonly List<StorageLocation> _items = new()
    {
        new() { Code = "SL-001", Name = "Raw Material Warehouse A", Type = "Warehouse", Capacity = 10000, Status = "Active" },
        new() { Code = "SL-002", Name = "Finished Goods Rack B", Type = "Rack", Capacity = 5000, Status = "Active" },
        new() { Code = "SL-003", Name = "Cold Storage C", Type = "Cold Storage", Capacity = 2000, Status = "Active" },
    };
    public static List<StorageLocation> GetAll() => _items.ToList();
    public static void Add(StorageLocation l) => _items.Add(l);
    public static void Delete(string id) => _items.RemoveAll(x => x.Code == id);
}

public static class FixedAssetStore
{
    private static readonly List<FixedAsset> _items = new()
    {
        new() { AssetCode = "FA-001", AssetName = "CNC Machine M3", Category = "Machinery", PurchaseDate = new(2023,6,15), Cost = 150000m, SalvageValue = 15000m, UsefulLifeYears = 10, Status = "Active" },
        new() { AssetCode = "FA-002", AssetName = "Forklift F2", Category = "Vehicles", PurchaseDate = new(2022,3,1), Cost = 45000m, SalvageValue = 5000m, UsefulLifeYears = 8, Status = "Active" },
        new() { AssetCode = "FA-003", AssetName = "Office Building", Category = "Property", PurchaseDate = new(2020,1,1), Cost = 2000000m, SalvageValue = 200000m, UsefulLifeYears = 30, Status = "Active" },
    };
    public static List<FixedAsset> GetAll() => _items.ToList();
    public static void Add(FixedAsset a) => _items.Add(a);
    public static void Delete(string id) => _items.RemoveAll(x => x.AssetCode == id);
}

public static class AttendanceStore
{
    private static readonly List<Attendance> _items = new()
    {
        new() { AttendanceId = "ATT-001", EmployeeCode = "EMP-001", EmployeeName = "John Doe", Date = new(2024,2,1), Status = "Present" },
        new() { AttendanceId = "ATT-002", EmployeeCode = "EMP-002", EmployeeName = "Jane Smith", Date = new(2024,2,1), Status = "Present" },
        new() { AttendanceId = "ATT-003", EmployeeCode = "EMP-003", EmployeeName = "Ahmed Khan", Date = new(2024,2,1), Status = "Absent" },
    };
    public static List<Attendance> GetAll() => _items.ToList();
    public static void Add(Attendance a) => _items.Add(a);
    public static void Delete(string id) => _items.RemoveAll(x => x.AttendanceId == id);
}

public static class AppraisalStore
{
    private static readonly List<Appraisal> _items = new()
    {
        new() { AppraisalId = "APR-001", EmployeeCode = "EMP-001", EmployeeName = "John Doe", Period = "Q1 2024", Rating = 4, Comments = "Exceeded targets", Status = "Completed" },
        new() { AppraisalId = "APR-002", EmployeeCode = "EMP-002", EmployeeName = "Jane Smith", Period = "Q1 2024", Rating = 5, Comments = "Outstanding quality focus", Status = "Completed" },
    };
    public static List<Appraisal> GetAll() => _items.ToList();
    public static void Add(Appraisal a) => _items.Add(a);
    public static void Delete(string id) => _items.RemoveAll(x => x.AppraisalId == id);
}

public static class ContactStore
{
    private static readonly List<Contact> _items = new()
    {
        new() { ContactId = "CNT-001", Name = "Sarah Lee", Email = "sarah@techcorp.com", Phone = "+1-555-0401", Company = "TechCorp Inc.", Status = "Active" },
        new() { ContactId = "CNT-002", Name = "Mike Brown", Email = "mike@greenfields.com", Phone = "+1-555-0402", Company = "GreenFields Ltd.", Status = "Active" },
    };
    public static List<Contact> GetAll() => _items.ToList();
    public static void Add(Contact c) => _items.Add(c);
    public static void Delete(string id) => _items.RemoveAll(x => x.ContactId == id);
}

public static class CampaignStore
{
    private static readonly List<Campaign> _items = new()
    {
        new() { CampaignId = "CMP-001", Name = "Summer Sale 2024", Type = "Email", StartDate = new(2024,6,1), EndDate = new(2024,6,30), Budget = 10000m, Status = "Draft" },
        new() { CampaignId = "CMP-002", Name = "New Product Launch", Type = "Social Media", StartDate = new(2024,3,1), EndDate = new(2024,4,15), Budget = 25000m, Status = "Active" },
    };
    public static List<Campaign> GetAll() => _items.ToList();
    public static void Add(Campaign c) => _items.Add(c);
    public static void Delete(string id) => _items.RemoveAll(x => x.CampaignId == id);
}

public static class ServiceTicketStore
{
    private static readonly List<ServiceTicket> _items = new()
    {
        new() { TicketId = "SR-001", CustomerName = "Acme Corporation", Subject = "Installation support for ERP module", Priority = "High", Status = "Open", CreatedDate = new(2024,2,1) },
        new() { TicketId = "SR-002", CustomerName = "Globex Industries", Subject = "User login issue", Priority = "Low", Status = "In Progress", CreatedDate = new(2024,1,28) },
    };
    public static List<ServiceTicket> GetAll() => _items.ToList();
    public static void Add(ServiceTicket t) => _items.Add(t);
    public static void Delete(string id) => _items.RemoveAll(x => x.TicketId == id);
}

public static class SpecificationStore
{
    private static readonly List<Specification> _items = new()
    {
        new() { SpecId = "SPEC-001", MaterialName = "Raw Material X", Characteristic = "Purity", MinValue = "99.0", MaxValue = "100.0", Status = "Active" },
        new() { SpecId = "SPEC-002", MaterialName = "Raw Material X", Characteristic = "Moisture", MinValue = "0", MaxValue = "3.0", Status = "Active" },
        new() { SpecId = "SPEC-003", MaterialName = "Finished Product A", Characteristic = "Weight", MinValue = "48", MaxValue = "52", Status = "Active" },
    };
    public static List<Specification> GetAll() => _items.ToList();
    public static void Add(Specification s) => _items.Add(s);
    public static void Delete(string id) => _items.RemoveAll(x => x.SpecId == id);
}

public static class InstrumentStore
{
    private static readonly List<Instrument> _items = new()
    {
        new() { Code = "INST-001", Name = "HPLC System 1", Type = "Chromatography", LastCalibration = new(2024,1,15), NextCalibration = new(2024,7,15), Status = "Operational" },
        new() { Code = "INST-002", Name = "pH Meter P1", Type = "Analytical", LastCalibration = new(2024,1,1), NextCalibration = new(2024,4,1), Status = "Operational" },
        new() { Code = "INST-003", Name = "Balance B1", Type = "Weighing", LastCalibration = new(2023,12,1), NextCalibration = new(2024,6,1), Status = "Under Maintenance" },
    };
    public static List<Instrument> GetAll() => _items.ToList();
    public static void Add(Instrument i) => _items.Add(i);
    public static void Delete(string id) => _items.RemoveAll(x => x.Code == id);
}

public static class DashboardStore
{
    private static readonly List<Dashboard> _items = new()
    {
        new() { DashboardId = "DB-001", Name = "Executive Overview", Category = "Executive", ConfigJson = "{\"widgets\":[\"sales\",\"inventory\",\"production\"]}", Status = "Active" },
        new() { DashboardId = "DB-002", Name = "Sales Performance", Category = "Sales", ConfigJson = "{\"widgets\":[\"revenue\",\"orders\",\"leads\"]}", Status = "Active" },
    };
    public static List<Dashboard> GetAll() => _items.ToList();
    public static void Add(Dashboard d) => _items.Add(d);
    public static void Delete(string id) => _items.RemoveAll(x => x.DashboardId == id);
}

public static class BIReportStore
{
    private static readonly List<BIReport> _items = new()
    {
        new() { ReportId = "RPT-001", ReportName = "Monthly Sales Summary", Category = "Sales", Format = "PDF", LastRun = new(2024,1,31), CreatedBy = "Admin" },
        new() { ReportId = "RPT-002", ReportName = "Inventory Valuation", Category = "Inventory", Format = "Excel", LastRun = new(2024,1,30), CreatedBy = "Admin" },
    };
    public static List<BIReport> GetAll() => _items.ToList();
    public static void Add(BIReport r) => _items.Add(r);
    public static void Delete(string id) => _items.RemoveAll(x => x.ReportId == id);
}
