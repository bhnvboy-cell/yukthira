using System.Collections.Concurrent;
using YuktiraERP.Core.Domain.Transaction;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class TCodeLayoutRegistry : ITCodeLayoutRegistry
{
    private readonly ConcurrentDictionary<string, TCodeLayoutConfig> _configs = new(StringComparer.OrdinalIgnoreCase);

    public TCodeLayoutRegistry() => SeedAll();

    public TCodeLayoutConfig? Get(string tcode) =>
        _configs.TryGetValue(tcode, out var cfg) ? cfg : null;

    public IReadOnlyList<TCodeLayoutConfig> GetAll() => _configs.Values.ToList();

    public void Register(TCodeLayoutConfig config) => _configs[config.TCode] = config;

    private void SeedAll()
    {
        Register(QE51N());
        Register(MIGO());
        Register(MM02());
        Register(VA01());
        Register(FB50());
        Register(CO01());
        Register(ME21N());
        Register(QE01());
        Register(FBL1N());
        Register(PA30());
        Register(QM01());
        Register(QM02());
        Register(QM03());
        Register(QM11());
        Register(QM12());
        Register(ZQM1());
        Register(QM1FM());
        Register(QM2F9());
        Register(QM1E1());
        Register(QM2QP());
        Register(QM2QN());
        Register(QMQMM());
        Register(QM1MP());
        Register(QMBKR());
        Register(QM2FA());
        Register(QMCALIB());
        Register(VL01N());
        Register(VL02N());
        Register(VF01());
        Register(ME51N());
        Register(ME28());
        Register(MIRO());
        Register(MD61());
        Register(MD02());
        Register(CO11N());
        Register(FB60());
        Register(F53());
        Register(F28());
        Register(ABZN());
        Register(KB11N());
        Register(IE01());
        Register(IW21());
        Register(IW31());
        Register(IW41());
        Register(IW32());
        Register(QP01());
        Register(QA01());
        Register(QN01());
        Register(QA11());
        Register(QC21());
        Register(KO88());
        Register(KS01());
        Register(BP());
        Register(CS01());
        Register(CR01());
        Register(IL01());
        // Customer Complaint & Return with Supplier Pass-Through Claim
        Register(CRRETURN());
        Register(CRINSPECT());
        Register(CRUDPOST());
        Register(CRCREDIT());
        Register(CRSUPPLY());
        Register(CRSRET());
        Register(CRDEBIT());
        Register(SOXADM());
        Register(UNIJRN());
        Register(RFSCAN());
        Register(RFPICK());
        Register(WAVEPK());
        Register(VSLOTT());
        Register(PPDS());
        Register(MRPEVT());
        Register(CONSOL());
        Register(TAXRET());
        Register(AIOCR());
    }

    private static TCodeLayoutConfig QE51N() => new()
    {
        TCode = "QE51N", Title = "Record Inspection Results", Module = "QM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Result", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Lot Created", Value = "", Key = "lotCreated", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Inspection Lot Origin", Value = "", Key = "inspectionLotOrigin", Editable = true },
            new() { Label = "Inspection Lot", Value = "", Key = "inspectionLot", Editable = true },
            new() { Label = "Material", Value = "", Key = "material" },
            new() { Label = "Batch", Value = "", Key = "batch" },
            new() { Label = "Status", Value = "", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "Results", Icon = "bi-clipboard-data", Active = true },
            new() { Id = "defects", Label = "Defects", Icon = "bi-exclamation-triangle" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "shortText", Label = "Characteristic", Type = "text", Width = 220, Required = true, Editable = true },
            new() { Key = "spec", Label = "Specifications", Type = "text", Width = 180, Editable = true },
            new() { Key = "result", Label = "Result", Type = "number", Editable = true, Width = 120, Validation = new() { Min = 0, Max = 99999, Required = true } },
            new() { Key = "valuation", Label = "Valuation", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OK", Label = "OK" },
                new() { Value = "NOK", Label = "Not OK" },
                new() { Value = "REVIEW", Label = "Review" },
            }},
            new() { Key = "defectClass", Label = "Defect Class", Type = "dropdown", Width = 150, Editable = true, Options = new() {
                new() { Value = "MAJOR", Label = "Major defect" },
                new() { Value = "MINOR", Label = "Minor defect" },
                new() { Value = "VALID", Label = "Valid" },
            }},
            new() { Key = "reportType", Label = "Report Type", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "CHAR", Label = "Characteristic" },
                new() { Value = "LOT", Label = "Lot" },
                new() { Value = "DEFECT", Label = "Defect" },
            }},
            new() { Key = "defectCodeGroup", Label = "Defect Code Group", Type = "dropdown", Width = 160, Editable = true, Options = new() {
                new() { Value = "VISUAL", Label = "Visual Defect" },
                new() { Value = "DIMENSIONAL", Label = "Dimensional" },
                new() { Value = "FUNCTIONAL", Label = "Functional" },
                new() { Value = "MATERIAL", Label = "Material Defect" },
                new() { Value = "PACKAGING", Label = "Packaging" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Results", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save inspection results?" },
            new() { Id = "printCOA", Label = "Print COA", Icon = "bi-printer", Style = "info", Handler = "printCOA" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig MIGO() => new()
    {
        TCode = "MIGO", Title = "Goods Movement - Posting", Module = "MM", Icon = "bi-box-arrow-right",
        ToolbarActions = new()
        {
            new() { Id = "check", Label = "Check", Icon = "bi-check-circle", Style = "primary", Handler = "validate" },
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "default", Handler = "simulate" },
            new() { Id = "post", Label = "Post", Icon = "bi-send", Style = "success", Handler = "post" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Movement Type", Value = "", Key = "movementType" },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Storage Location", Value = "", Key = "storageLocation" },
            new() { Label = "Posting Date", Value = "", Key = "postingDate", Type = "date" },
            new() { Label = "Document Date", Value = "", Key = "documentDate", Type = "date" },
            new() { Label = "Reference", Value = "", Key = "reference" },
            new() { Label = "Header Text", Value = "", Key = "headerText" },
        },
        Tabs = new()
        {
            new() { Id = "items", Label = "Item Overview", Icon = "bi-list-ol", Active = true },
            new() { Id = "purchaseOrder", Label = "Purchase Order", Icon = "bi-file-earmark-text" },
            new() { Id = "where", Label = "Where", Icon = "bi-geo-alt" },
            new() { Id = "accounting", Label = "Accounting", Icon = "bi-calculator" },
        },
        Columns = new()
        {
            new() { Key = "material", Label = "Material", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 200, Editable = true },
            new() { Key = "quantity", Label = "Qty", Type = "number", Editable = true, Width = 90, Validation = new() { Min = 0.001m, Required = true } },
            new() { Key = "uom", Label = "UoM", Type = "text", Width = 70, Editable = true },
            new() { Key = "amount", Label = "Amount", Type = "currency", Width = 110, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80, DefaultValue = "", Editable = true },
            new() { Key = "sloc", Label = "SLoc", Type = "text", Width = 80, Editable = true },
            new() { Key = "batch", Label = "Batch", Type = "text", Width = 120, Editable = true },
            new() { Key = "stockType", Label = "Stock Type", Type = "dropdown", Width = 120, Options = new() {
                new() { Value = "FREE", Label = "Unrestricted" },
                new() { Value = "QI", Label = "Quality Inspection" },
                new() { Value = "BLOCKED", Label = "Blocked" },
                new() { Value = "CONSIGNMENT", Label = "Consignment" },
            }},
            new() { Key = "poNumber", Label = "PO Number", Type = "text", Width = 130, Editable = true },
            new() { Key = "costCenter", Label = "Cost Center", Type = "text", Width = 120, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "post", Label = "Post", Icon = "bi-send", Style = "primary", Handler = "post", Confirm = true, ConfirmMessage = "Post goods movement?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new()
        {
            ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true,
            CustomActions = new()
            {
                new() { Id = "force", Label = "Force", Icon = "bi-shield-check", Style = "warning", Handler = "force" },
                new() { Id = "details", Label = "More Details", Icon = "bi-info-circle", Style = "default", Handler = "showDetails" },
            }
        }
    };

    private static TCodeLayoutConfig MM02() => new()
    {
        TCode = "MM02", Title = "Change Material", Module = "MM", Icon = "bi-pencil-square",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
            new() { Id = "extensions", Label = "Extensions", Icon = "bi-arrows-expand", Style = "default", Handler = "extensions" },
        },
        Metadata = new()
        {
            new() { Label = "Material", Value = "", Key = "material", Editable = true },
            new() { Label = "Material Type", Value = "", Key = "materialType" },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Last Changed", Value = "", Key = "lastChanged" },
            new() { Label = "Status", Value = "Active", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "basicData", Label = "Basic Data", Icon = "bi-database", Active = true },
            new() { Id = "sales", Label = "Sales", Icon = "bi-cart3" },
            new() { Id = "purchasing", Label = "Purchasing", Icon = "bi-bag" },
            new() { Id = "mrp", Label = "MRP", Icon = "bi-calculator" },
            new() { Id = "accounting", Label = "Accounting", Icon = "bi-cash" },
            new() { Id = "costing", Label = "Costing", Icon = "bi-currency-dollar" },
            new() { Id = "classification", Label = "Classification", Icon = "bi-tags" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 200 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 300, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
            new() { Key = "changed", Label = "", Type = "changed_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save material changes?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = false, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig VA01() => new()
    {
        TCode = "VA01", Title = "Create Sales Order", Module = "SD", Icon = "bi-cart-plus",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
            new() { Id = "check", Label = "Check", Icon = "bi-check-circle", Style = "default", Handler = "validate" },
            new() { Id = "copyFrom", Label = "Copy From", Icon = "bi-clipboard", Style = "default", Handler = "copyFrom" },
        },
        Metadata = new()
        {
            new() { Label = "Order Type", Value = "", Key = "orderType" },
            new() { Label = "Sales Org", Value = "", Key = "salesOrg" },
            new() { Label = "Dist. Channel", Value = "", Key = "distChannel" },
            new() { Label = "Division", Value = "", Key = "division" },
            new() { Label = "Sold-To Party", Value = "", Key = "soldToParty", Editable = true },
            new() { Label = "Ship-To Party", Value = "", Key = "shipToParty" },
            new() { Label = "PO Number", Value = "", Key = "poNumber", Editable = true },
            new() { Label = "Net Value", Value = "", Key = "netValue" },
            new() { Label = "Currency", Value = "", Key = "currency" },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "items", Label = "Items", Icon = "bi-list-ol" },
            new() { Id = "scheduleLines", Label = "Schedule Lines", Icon = "bi-calendar3" },
            new() { Id = "partners", Label = "Partners", Icon = "bi-people" },
            new() { Id = "conditions", Label = "Conditions", Icon = "bi-percent" },
            new() { Id = "text", Label = "Texts", Icon = "bi-file-text" },
        },
        Columns = new()
        {
            new() { Key = "item", Label = "Item", Type = "number", Width = 60 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 220, Editable = true },
            new() { Key = "quantity", Label = "Order Qty", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 1, Required = true } },
            new() { Key = "uom", Label = "UoM", Type = "text", Width = 60, Editable = true },
            new() { Key = "unitPrice", Label = "Unit Price", Type = "currency", Width = 110, Editable = true },
            new() { Key = "netValue", Label = "Net Value", Type = "currency", Width = 120 },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80, Editable = true },
            new() { Key = "deliveryDate", Label = "Delivery Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this sales order?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new()
        {
            ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true,
            CustomActions = new()
            {
                new() { Id = "copyFrom", Label = "Copy From PO", Icon = "bi-clipboard", Style = "default", Handler = "copyFrom" },
            }
        }
    };

    private static TCodeLayoutConfig FB50() => new()
    {
        TCode = "FB50", Title = "Enter G/L Account Document", Module = "FI", Icon = "bi-calculator",
        ToolbarActions = new()
        {
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "primary", Handler = "simulate" },
            new() { Id = "post", Label = "Post", Icon = "bi-send", Style = "success", Handler = "post" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Document Date", Value = "", Key = "documentDate", Type = "date" },
            new() { Label = "Posting Date", Value = "", Key = "postingDate", Type = "date" },
            new() { Label = "Document Type", Value = "", Key = "documentType" },
            new() { Label = "Company Code", Value = "", Key = "companyCode" },
            new() { Label = "Currency", Value = "", Key = "currency" },
            new() { Label = "Reference", Value = "", Key = "reference", Editable = true },
            new() { Label = "Doc. Header Text", Value = "", Key = "headerText", Editable = true },
        },
        Tabs = new()
        {
            new() { Id = "lineItems", Label = "Line Items", Icon = "bi-list-ol", Active = true },
            new() { Id = "balanceCheck", Label = "Balance Check", Icon = "bi-scale" },
        },
        Columns = new()
        {
            new() { Key = "item", Label = "Item", Type = "number", Width = 50 },
            new() { Key = "glAccount", Label = "G/L Account", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 220, Editable = true },
            new() { Key = "amount", Label = "Amount in LC", Type = "currency", Width = 130, Editable = true, Required = true },
            new() { Key = "debitCredit", Label = "D/C", Type = "dropdown", Width = 80, Editable = true, Options = new() {
                new() { Value = "D", Label = "Debit" },
                new() { Value = "C", Label = "Credit" },
            }},
            new() { Key = "costCenter", Label = "Cost Center", Type = "text", Width = 120, Editable = true },
            new() { Key = "profitCenter", Label = "Profit Center", Type = "text", Width = 110, Editable = true },
            new() { Key = "taxCode", Label = "Tax Code", Type = "text", Width = 80, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "post", Label = "Post Document", Icon = "bi-send", Style = "primary", Handler = "post", Confirm = true, ConfirmMessage = "Post this journal entry?" },
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "default", Handler = "simulate" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new()
        {
            ShowSearch = false, ShowFilter = false, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true,
            CustomActions = new()
            {
                new() { Id = "balance", Label = "Balance", Icon = "bi-scale", Style = "default", Handler = "balanceCheck" },
            }
        }
    };

    private static TCodeLayoutConfig CO01() => new()
    {
        TCode = "CO01", Title = "Create Production Order", Module = "PP", Icon = "bi-gear",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
            new() { Id = "release", Label = "Release", Icon = "bi-unlock", Style = "default", Handler = "release" },
        },
        Metadata = new()
        {
            new() { Label = "Order Type", Value = "", Key = "orderType" },
            new() { Label = "Material", Value = "", Key = "material", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Production Qty", Value = "0", Key = "prodQty", Editable = true },
            new() { Label = "UoM", Value = "", Key = "uom" },
            new() { Label = "BOM", Value = "", Key = "bom" },
            new() { Label = "Routing", Value = "", Key = "routing" },
            new() { Label = "Status", Value = "", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "operations", Label = "Operations", Icon = "bi-diagram-3" },
            new() { Id = "components", Label = "Components", Icon = "bi-boxes" },
            new() { Id = "dates", Label = "Dates/Times", Icon = "bi-calendar" },
            new() { Id = "costs", Label = "Costs", Icon = "bi-currency-dollar" },
        },
        Columns = new()
        {
            new() { Key = "operation", Label = "Operation", Type = "number", Width = 80 },
            new() { Key = "workCenter", Label = "Work Center", Type = "text", Width = 120, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 220, Editable = true },
            new() { Key = "setupTime", Label = "Setup Time", Type = "number", Width = 100, Editable = true },
            new() { Key = "machineTime", Label = "Machine Time", Type = "number", Width = 110, Editable = true },
            new() { Key = "laborTime", Label = "Labor Time", Type = "number", Width = 100, Editable = true },
            new() { Key = "quantity", Label = "Qty", Type = "number", Width = 80 },
            new() { Key = "uom", Label = "UoM", Type = "text", Width = 60, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "release", Label = "Release Order", Icon = "bi-unlock", Style = "success", Handler = "release" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig ME21N() => new()
    {
        TCode = "ME21N", Title = "Create Purchase Order", Module = "MM", Icon = "bi-bag-plus",
        ToolbarActions = new()
        {
            new() { Id = "check", Label = "Check", Icon = "bi-check-circle", Style = "primary", Handler = "validate" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "success", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "PO Type", Value = "", Key = "poType" },
            new() { Label = "Purch. Org", Value = "", Key = "purchOrg" },
            new() { Label = "Purch. Group", Value = "", Key = "purchGroup" },
            new() { Label = "Company Code", Value = "", Key = "companyCode" },
            new() { Label = "Vendor", Value = "", Key = "vendor", Editable = true },
            new() { Label = "PO Date", Value = "", Key = "poDate", Type = "date" },
        },
        Tabs = new()
        {
            new() { Id = "delivery", Label = "Delivery/Invoice", Icon = "bi-truck", Active = true },
            new() { Id = "items", Label = "Item Overview", Icon = "bi-list-ol" },
            new() { Id = "conditions", Label = "Conditions", Icon = "bi-percent" },
            new() { Id = "text", Label = "Texts", Icon = "bi-file-text" },
        },
        Columns = new()
        {
            new() { Key = "item", Label = "Item", Type = "number", Width = 50 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Short Text", Type = "text", Width = 200, Editable = true },
            new() { Key = "quantity", Label = "PO Quantity", Type = "number", Width = 110, Editable = true, Validation = new() { Min = 1, Required = true } },
            new() { Key = "uom", Label = "Order Unit", Type = "text", Width = 80, Editable = true },
            new() { Key = "netPrice", Label = "Net Price", Type = "currency", Width = 110, Editable = true, Required = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80, DefaultValue = "", Editable = true },
            new() { Key = "deliveryDate", Label = "Delivery Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "taxCode", Label = "Tax Code", Type = "text", Width = 80, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this purchase order?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QE01() => new()
    {
        TCode = "QE01", Title = "Record Inspection Lot Results", Module = "QM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
            new() { Id = "usage", Label = "Usage Decision", Icon = "bi-check2-square", Style = "default", Handler = "usageDecision" },
        },
        Metadata = new()
        {
            new() { Label = "Inspection Lot", Value = "", Key = "inspectionLot", Editable = true },
            new() { Label = "Material", Value = "", Key = "material" },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Inspection Type", Value = "", Key = "inspType" },
            new() { Label = "Status", Value = "", Key = "status" },
            new() { Label = "Lots Status", Value = "", Key = "lotStatus" },
        },
        Tabs = new()
        {
            new() { Id = "results", Label = "Results", Icon = "bi-clipboard-data", Active = true },
            new() { Id = "samples", Label = "Samples", Icon = "bi-droplet" },
            new() { Id = "defects", Label = "Defects", Icon = "bi-exclamation-triangle" },
        },
        Columns = new()
        {
            new() { Key = "char", Label = "Char.", Type = "number", Width = 60 },
            new() { Key = "shortText", Label = "Short Text", Type = "text", Width = 220, Required = true, Editable = true },
            new() { Key = "spec", Label = "Specs", Type = "text", Width = 140, Editable = true },
            new() { Key = "result", Label = "Result", Type = "number", Width = 110, Editable = true, Validation = new() { Required = true } },
            new() { Key = "valuation", Label = "Valuation", Type = "status_icon", Width = 100, Options = new() {
                new() { Value = "OK", Label = "OK", Color = "success" },
                new() { Value = "NOK", Label = "Reject", Color = "danger" },
            }},
            new() { Key = "defectClass", Label = "Defect Class", Type = "dropdown", Width = 140, Options = new() {
                new() { Value = "A", Label = "Major defect" },
                new() { Value = "B", Label = "Minor defect" },
                new() { Value = "C", Label = "Valid" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "usage", Label = "Usage Decision", Icon = "bi-check2-square", Style = "success", Handler = "usageDecision" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig FBL1N() => new()
    {
        TCode = "FBL1N", Title = "Vendor Line Item Display", Module = "FI", Icon = "bi-file-earmark-text",
        ToolbarActions = new()
        {
            new() { Id = "execute", Label = "Execute", Icon = "bi-play-fill", Style = "primary", Handler = "execute" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
            new() { Id = "export", Label = "Export", Icon = "bi-download", Style = "default", Handler = "export" },
        },
        Metadata = new()
        {
            new() { Label = "Vendor", Value = "", Key = "vendor", Editable = true },
            new() { Label = "Company Code", Value = "", Key = "companyCode" },
            new() { Label = "Item Type", Value = "", Key = "itemType" },
            new() { Label = "Posting Date From", Value = "", Key = "dateFrom", Type = "date" },
            new() { Label = "Posting Date To", Value = "", Key = "dateTo", Type = "date" },
        },
        Tabs = new()
        {
            new() { Id = "allItems", Label = "All Items", Icon = "bi-list-check", Active = true },
            new() { Id = "openItems", Label = "Open Items", Icon = "bi-folder2-open" },
            new() { Id = "clearedItems", Label = "Cleared Items", Icon = "bi-folder-check" },
        },
        Columns = new()
        {
            new() { Key = "docNumber", Label = "Document No.", Type = "text", Width = 130 },
            new() { Key = "docDate", Label = "Doc. Date", Type = "date", Width = 110 },
            new() { Key = "postingDate", Label = "Posting Date", Type = "date", Width = 110 },
            new() { Key = "reference", Label = "Reference", Type = "text", Width = 130 },
            new() { Key = "type", Label = "Type", Type = "text", Width = 60 },
            new() { Key = "debit", Label = "Debit", Type = "currency", Width = 120, Align = "right" },
            new() { Key = "credit", Label = "Credit", Type = "currency", Width = 120, Align = "right" },
            new() { Key = "amount", Label = "Amount", Type = "currency", Width = 130, Align = "right" },
            new() { Key = "currency", Label = "Curr.", Type = "text", Width = 60 },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 80, Options = new() {
                new() { Value = "OPEN", Label = "Open", Color = "warning" },
                new() { Value = "CLEARED", Label = "Cleared", Color = "success" },
            }},
        },
        FooterActions = new()
        {
            new() { Id = "execute", Label = "Execute", Icon = "bi-play-fill", Style = "primary", Handler = "execute" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig PA30() => new()
    {
        TCode = "PA30", Title = "Maintain HR Master Data", Module = "HR", Icon = "bi-person-lines-fill",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
            new() { Id = "copy", Label = "Copy", Icon = "bi-clipboard", Style = "default", Handler = "copy" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "delete" },
        },
        Metadata = new()
        {
            new() { Label = "Employee ID", Value = "", Key = "employeeId", Editable = true },
            new() { Label = "Name", Value = "", Key = "name" },
            new() { Label = "Position", Value = "", Key = "position" },
            new() { Label = "Department", Value = "", Key = "department" },
            new() { Label = "Hire Date", Value = "", Key = "hireDate", Type = "date" },
            new() { Label = "Status", Value = "", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "personalData", Label = "Personal Data", Icon = "bi-person", Active = true },
            new() { Id = "orgAssignment", Label = "Org. Assignment", Icon = "bi-diagram-3" },
            new() { Id = "bankDetails", Label = "Bank Details", Icon = "bi-bank" },
            new() { Id = "addresses", Label = "Addresses", Icon = "bi-geo-alt" },
            new() { Id = "qualifications", Label = "Qualifications", Icon = "bi-mortarboard" },
        },
        Columns = new()
        {
            new() { Key = "infotype", Label = "Infotype", Type = "text", Width = 100, Editable = true },
            new() { Key = "name", Label = "Name", Type = "text", Width = 200, Editable = true },
            new() { Key = "validFrom", Label = "Valid From", Type = "date", Width = 120, Editable = true },
            new() { Key = "validTo", Label = "Valid To", Type = "date", Width = 120, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 80, Options = new() {
                new() { Value = "ACTIVE", Label = "Active", Color = "success" },
                new() { Value = "INACTIVE", Label = "Inactive", Color = "secondary" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM01() => new()
    {
        TCode = "QM01", Title = "Create Quality Notification", Module = "QM", Icon = "bi-clipboard-plus",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Notification", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Notification Type", Value = "Q1", Key = "notificationType", Editable = true },
            new() { Label = "Reference Doc (Process Order)", Value = "", Key = "referenceDocument", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Partner (Coordinator)", Value = "", Key = "partnerName", Editable = true },
            new() { Label = "Subject Coding", Value = "", Key = "subjectCoding", Editable = true },
            new() { Label = "Description", Value = "", Key = "description", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "text", Label = "Long Text", Icon = "bi-file-text" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "notificationNumber", Label = "Notification No.", Type = "text", Width = 150, Required = true, Editable = true },
            new() { Key = "notificationType", Label = "Type", Type = "dropdown", Width = 100, Editable = true, Options = new() {
                new() { Value = "Q1", Label = "Q1 - Quality" },
                new() { Value = "Q2", Label = "Q2 - Customer" },
                new() { Value = "Q3", Label = "Q3 - Supplier" },
                new() { Value = "Q5", Label = "Q5 - Internal" },
            }},
            new() { Key = "referenceDocument", Label = "Process Order Ref", Type = "text", Width = 160, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "partnerName", Label = "Coordinator", Type = "text", Width = 160, Editable = true },
            new() { Key = "subjectCoding", Label = "Subject Code", Type = "dropdown", Width = 150, Editable = true, Options = new() {
                new() { Value = "QUALITY", Label = "Quality Issue" },
                new() { Value = "DELIVERY", Label = "Delivery Problem" },
                new() { Value = "PACKAGING", Label = "Packaging" },
                new() { Value = "DOCUMENTATION", Label = "Documentation" },
            }},
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "longText", Label = "Long Text", Type = "text", Width = 250, Editable = true },
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "NEW", Label = "New" },
                new() { Value = "IN_PROCESS", Label = "In Process" },
                new() { Value = "COMPLETED", Label = "Completed" },
                new() { Value = "CLOSED", Label = "Closed" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Notification", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this quality notification?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM02() => new()
    {
        TCode = "QM02", Title = "Change Quality Notification", Module = "QM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Notification No.", Value = "", Key = "notificationNumber", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Description", Value = "", Key = "description" },
            new() { Label = "Status", Value = "", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "defect", Label = "Defect Assignment", Icon = "bi-exclamation-triangle", Active = true },
            new() { Id = "cause", Label = "Cause Code", Icon = "bi-search" },
            new() { Id = "tasks", Label = "Tasks", Icon = "bi-list-check" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "defectLocation", Label = "Defect Location", Type = "dropdown", Width = 160, Editable = true, Options = new() {
                new() { Value = "SURFACE", Label = "Surface" },
                new() { Value = "INTERNAL", Label = "Internal" },
                new() { Value = "EDGE", Label = "Edge" },
                new() { Value = "COATING", Label = "Coating" },
                new() { Value = "ASSEMBLY", Label = "Assembly" },
            }},
            new() { Key = "defectType", Label = "Defect Type", Type = "dropdown", Width = 150, Editable = true, Options = new() {
                new() { Value = "VISUAL", Label = "Visual" },
                new() { Value = "DIMENSIONAL", Label = "Dimensional" },
                new() { Value = "FUNCTIONAL", Label = "Functional" },
                new() { Value = "MATERIAL", Label = "Material" },
                new() { Value = "PACKAGING", Label = "Packaging" },
            }},
            new() { Key = "causeCode", Label = "Cause Code", Type = "dropdown", Width = 150, Editable = true, Options = new() {
                new() { Value = "MACHINE", Label = "Machine" },
                new() { Value = "MATERIAL", Label = "Material" },
                new() { Value = "METHOD", Label = "Method" },
                new() { Value = "MANPOWER", Label = "Manpower" },
                new() { Value = "ENVIRONMENT", Label = "Environment" },
            }},
            new() { Key = "defectCodeGroup", Label = "Defect Code Group", Type = "dropdown", Width = 160, Editable = true, Options = new() {
                new() { Value = "VISUAL", Label = "Visual Defect" },
                new() { Value = "DIMENSIONAL", Label = "Dimensional" },
                new() { Value = "FUNCTIONAL", Label = "Functional" },
            }},
            new() { Key = "defectDescription", Label = "Defect Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "tasks", Label = "Tasks", Type = "text", Width = 200, Editable = true },
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OPEN", Label = "Open" },
                new() { Value = "IN_PROCESS", Label = "In Process" },
                new() { Value = "COMPLETED", Label = "Completed" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Changes", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save notification changes?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM03() => new()
    {
        TCode = "QM03", Title = "Quality Notification Tasks", Module = "QM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Task", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Notification No.", Value = "", Key = "notificationNumber", Editable = true },
            new() { Label = "Description", Value = "", Key = "description" },
            new() { Label = "Status", Value = "", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "tasks", Label = "Tasks", Icon = "bi-list-check", Active = true },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "taskNumber", Label = "Task No.", Type = "text", Width = 90 },
            new() { Key = "chooseTask", Label = "Choose Task", Type = "dropdown", Width = 180, Editable = true, Required = true, Options = new() {
                new() { Value = "INVESTIGATE", Label = "Investigate Root Cause" },
                new() { Value = "CORRECT", Label = "Corrective Action" },
                new() { Value = "PREVENT", Label = "Preventive Action" },
                new() { Value = "VERIFY", Label = "Verify Effectiveness" },
                new() { Value = "DOCUMENT", Label = "Document Findings" },
            }},
            new() { Key = "userResponsible", Label = "User Responsible", Type = "text", Width = 160, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true, Required = true },
            new() { Key = "completionText", Label = "Completion Text", Type = "text", Width = 250, Editable = true },
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OPEN", Label = "Open" },
                new() { Value = "IN_PROCESS", Label = "In Process" },
                new() { Value = "COMPLETED", Label = "Completed" },
            }},
            new() { Key = "completedAt", Label = "Completed At", Type = "date", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "complete", Label = "Complete Selected Tasks", Icon = "bi-check2-all", Style = "success", Handler = "complete", Confirm = true, ConfirmMessage = "Mark selected tasks as completed?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new()
        {
            ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true
        }
    };

    private static TCodeLayoutConfig QM11() => new()
    {
        TCode = "QM11", Title = "Record Inspection Results", Module = "QM", Icon = "bi-clipboard-data",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
            new() { Id = "defect", Label = "Record Defect", Icon = "bi-exclamation-triangle", Style = "warning", Handler = "recordDefect" },
        },
        Metadata = new()
        {
            new() { Label = "Inspection Lot", Value = "", Key = "lotNumber", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Material Code", Value = "", Key = "materialCode" },
            new() { Label = "Material Name", Value = "", Key = "materialName" },
            new() { Label = "Inspection Lot Origin", Value = "", Key = "inspectionLotOrigin", Editable = true },
            new() { Label = "Result Status", Value = "", Key = "resultStatus" },
        },
        Tabs = new()
        {
            new() { Id = "results", Label = "Results", Icon = "bi-clipboard-data", Active = true },
            new() { Id = "defects", Label = "Defects", Icon = "bi-exclamation-triangle" },
        },
        Columns = new()
        {
            new() { Key = "characteristic", Label = "Characteristic", Type = "text", Width = 200, Required = true, Editable = true },
            new() { Key = "specification", Label = "Specification", Type = "text", Width = 160, Editable = true },
            new() { Key = "result", Label = "Result", Type = "number", Width = 110, Editable = true, Validation = new() { Required = true } },
            new() { Key = "uom", Label = "UoM", Type = "text", Width = 70 },
            new() { Key = "valuation", Label = "Valuation", Type = "status_icon", Width = 110, Options = new() {
                new() { Value = "OK", Label = "OK", Color = "success" },
                new() { Value = "NOK", Label = "Not OK", Color = "danger" },
                new() { Value = "REVIEW", Label = "Review", Color = "warning" },
            }},
            new() { Key = "defectCodeGroup", Label = "Defect Code Group", Type = "dropdown", Width = 160, Editable = true, Options = new() {
                new() { Value = "VISUAL", Label = "Visual Defect" },
                new() { Value = "DIMENSIONAL", Label = "Dimensional" },
                new() { Value = "FUNCTIONAL", Label = "Functional" },
                new() { Value = "MATERIAL", Label = "Material Defect" },
                new() { Value = "PACKAGING", Label = "Packaging" },
            }},
            new() { Key = "reportType", Label = "Report Type", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "CHAR", Label = "Characteristic" },
                new() { Value = "LOT", Label = "Lot" },
                new() { Value = "DEFECT", Label = "Defect" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Results", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save inspection results?" },
            new() { Id = "defect", Label = "Record Defect", Icon = "bi-exclamation-triangle", Style = "warning", Handler = "recordDefect" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM12() => new()
    {
        TCode = "QM12", Title = "Manage Usage Decisions", Module = "QM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Decision", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Inspection Lot", Value = "", Key = "lotNumber", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Insp. Lot Origin", Value = "", Key = "inspectionLotOrigin" },
            new() { Label = "Result Recording Status", Value = "", Key = "resultRecordingStatus" },
            new() { Label = "UD Code", Value = "", Key = "udCode", Editable = true },
            new() { Label = "Stock Proposal", Value = "", Key = "stockProposal", Editable = true },
            new() { Label = "Certificate Received", Value = "", Key = "certificateReceived" },
            new() { Label = "Status", Value = "", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "usageDecision", Label = "Usage Decision", Icon = "bi-check2-square", Active = true },
            new() { Id = "stockPosting", Label = "Stock Posting", Icon = "bi-box" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "lotNumber", Label = "Inspection Lot", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80, Editable = true },
            new() { Key = "origin", Label = "Origin", Type = "text", Width = 110 },
            new() { Key = "resultStatus", Label = "Result Status", Type = "status_badge", Width = 120, Options = new() {
                new() { Value = "RECORDED", Label = "Recorded", Color = "info" },
                new() { Value = "CERT_CONFIRMED", Label = "Cert. Confirmed", Color = "success" },
                new() { Value = "UD_RECORDED", Label = "UD Recorded", Color = "success" },
            }},
            new() { Key = "udCode", Label = "UD Code", Type = "dropdown", Width = 150, Editable = true, Required = true, Options = new() {
                new() { Value = "A", Label = "A - Accept" },
                new() { Value = "R", Label = "R - Reject" },
                new() { Value = "R1", Label = "R1 - Rework" },
                new() { Value = "N", Label = "N - Return to Vendor" },
                new() { Value = "S", Label = "S - Scrap" },
            }},
            new() { Key = "stockProposal", Label = "Stock Proposal", Type = "dropdown", Width = 150, Editable = true, Required = true, Options = new() {
                new() { Value = "FREE", Label = "Unrestricted Use" },
                new() { Value = "QI", Label = "Quality Inspection" },
                new() { Value = "BLOCKED", Label = "Blocked Stock" },
                new() { Value = "SAMPLE", Label = "Sample" },
            }},
            new() { Key = "certificateReceived", Label = "Cert. Received", Type = "status_icon", Width = 100, Options = new() {
                new() { Value = "Yes", Label = "Yes", Color = "success" },
                new() { Value = "No", Label = "No", Color = "warning" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save & Post Stock", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save usage decision and post stock?" },
            new() { Id = "confirm", Label = "Confirm Certificate", Icon = "bi-patch-check", Style = "success", Handler = "confirmCertificate" },
            new() { Id = "printCOA", Label = "Print COA", Icon = "bi-printer", Style = "info", Handler = "printCOA" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig ZQM1() => new()
    {
        TCode = "ZQM1", Title = "QM Master Data Setup", Module = "QM", Icon = "bi-clipboard-data",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Master Data", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete Selected", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected master data?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "export", Label = "Export", Icon = "bi-download", Style = "default", Handler = "export" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Material", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Material Name", Value = "", Key = "materialName", Editable = true },
            new() { Label = "Status", Value = "Active", Key = "status" },
            new() { Label = "Created By", Value = "", Key = "createdBy" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "inspection", Label = "Inspection Settings", Icon = "bi-search" },
            new() { Id = "catalogs", Label = "Catalogs", Icon = "bi-book" },
            new() { Id = "scheduling", Label = "Scheduling", Icon = "bi-calendar" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "materialCode", Label = "Material Code", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "materialName", Label = "Material Name", Type = "text", Width = 200, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "inspectionType", Label = "Inspection Type", Type = "dropdown", Width = 160, Editable = true, Options = new() {
                new() { Value = "01", Label = "Goods Receipt" },
                new() { Value = "02", Label = "Production" },
                new() { Value = "03", Label = "Stock Transfer" },
                new() { Value = "04", Label = "Other" },
            }},
            new() { Key = "inspectionLotOrigin", Label = "Lot Origin", Type = "dropdown", Width = 150, Editable = true, Options = new() {
                new() { Value = "PURCHASE_ORDER", Label = "Purchase Order" },
                new() { Value = "PRODUCTION_ORDER", Label = "Production Order" },
                new() { Value = "DELIVERY", Label = "Delivery" },
                new() { Value = "INVENTORY", Label = "Inventory" },
            }},
            new() { Key = "inspectionProcedure", Label = "Inspection Procedure", Type = "dropdown", Width = 160, Editable = true, Options = new() {
                new() { Value = "STD", Label = "Standard" },
                new() { Value = "SKIP", Label = "Skip" },
                new() { Value = "FULL", Label = "Full Inspection" },
            }},
            new() { Key = "sampleProcedure", Label = "Sample Procedure", Type = "dropdown", Width = 150, Editable = true, Options = new() {
                new() { Value = "AQL", Label = "AQL" },
                new() { Value = "FIXED", Label = "Fixed Sample" },
                new() { Value = "DYN", Label = "Dynamic" },
            }},
            new() { Key = "qmControlKey", Label = "QM Control Key", Type = "dropdown", Width = 140, Editable = true, Options = new() {
                new() { Value = "0001", Label = "Manual Insp. obligatory" },
                new() { Value = "0002", Label = "Auto Insp." },
                new() { Value = "0003", Label = "Manual Insp. optional" },
            }},
            new() { Key = "catalogType", Label = "Catalog Type", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "1", Label = "Defect" },
                new() { Value = "2", Label = "Usage Decision" },
                new() { Value = "3", Label = "Inspector" },
            }},
            new() { Key = "defectCatalog", Label = "Defect Catalog", Type = "text", Width = 130, Editable = true },
            new() { Key = "defectCodeGroup", Label = "Defect Code Group", Type = "text", Width = 150, Editable = true },
            new() { Key = "udCatalog", Label = "UD Catalog", Type = "text", Width = 120, Editable = true },
            new() { Key = "UDCodeGroup", Label = "UD Code Group", Type = "text", Width = 140, Editable = true },
            new() { Key = "frequency", Label = "Frequency", Type = "number", Width = 100, Editable = true },
            new() { Key = "frequencyUnit", Label = "Freq. Unit", Type = "dropdown", Width = 100, Editable = true, Options = new() {
                new() { Value = "Days", Label = "Days" },
                new() { Value = "Weeks", Label = "Weeks" },
                new() { Value = "Months", Label = "Months" },
            }},
            new() { Key = "isActive", Label = "Active", Type = "status_icon", Width = 80, Options = new() {
                new() { Value = "true", Label = "Active", Color = "success" },
                new() { Value = "false", Label = "Inactive", Color = "danger" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "activate", Label = "Activate", Icon = "bi-toggle-on", Style = "success", Handler = "activate" },
            new() { Id = "deactivate", Label = "Deactivate", Icon = "bi-toggle-off", Style = "danger", Handler = "deactivate" },
            new() { Id = "save", Label = "Save Changes", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save master data changes?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM1FM() => new()
    {
        TCode = "1FM", Title = "QM in Procurement", Module = "QM", Icon = "bi-cart-check",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Inspection", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "PO Number", Value = "", Key = "poReference", Editable = true },
            new() { Label = "Vendor", Value = "", Key = "vendorCode", Editable = true },
            new() { Label = "Vendor Name", Value = "", Key = "vendorName", Editable = true },
            new() { Label = "Material Group", Value = "", Key = "materialGroup", Editable = true },
            new() { Label = "Inspection Type", Value = "01", Key = "inspectionType" },
            new() { Label = "Status", Value = "Active", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "materials", Label = "Materials", Icon = "bi-box" },
            new() { Id = "sample", Label = "Sample", Icon = "bi-layers" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "configName", Label = "Config Name", Type = "text", Width = 200, Required = true, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "inspectionType", Label = "Insp. Type", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "01", Label = "Goods Receipt" },
                new() { Value = "02", Label = "Production" },
                new() { Value = "03", Label = "Stock Transfer" },
            }},
            new() { Key = "vendorCode", Label = "Vendor Code", Type = "text", Width = 120, Editable = true },
            new() { Key = "vendorName", Label = "Vendor Name", Type = "text", Width = 180, Editable = true },
            new() { Key = "materialGroup", Label = "Material Group", Type = "text", Width = 140, Editable = true },
            new() { Key = "poReference", Label = "PO Reference", Type = "text", Width = 130, Editable = true },
            new() { Key = "sampleSize", Label = "Sample Size", Type = "number", Width = 110, Editable = true, Validation = new() { Min = 0, Max = 9999 } },
            new() { Key = "inspectionLevel", Label = "Insp. Level", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "I", Label = "Level I" },
                new() { Value = "II", Label = "Level II" },
                new() { Value = "III", Label = "Level III" },
                new() { Value = "S1", Label = "S1" },
                new() { Value = "S2", Label = "S2" },
            }},
            new() { Key = "status", Label = "Status", Type = "status_icon", Width = 100, Options = new() {
                new() { Value = "ACTIVE", Label = "Active", Color = "success" },
                new() { Value = "INACTIVE", Label = "Inactive", Color = "secondary" },
                new() { Value = "BLOCKED", Label = "Blocked", Color = "danger" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Config", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save inspection config?" },
            new() { Id = "activate", Label = "Activate", Icon = "bi-toggle-on", Style = "success", Handler = "activate" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM2F9() => new()
    {
        TCode = "2F9", Title = "Quality Notification - Supplier Complaints", Module = "QM", Icon = "bi-exclamation-triangle",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "Create Complaint", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected complaint?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Vendor", Value = "", Key = "vendorCode", Editable = true },
            new() { Label = "Vendor Name", Value = "", Key = "vendorName", Editable = true },
            new() { Label = "Material", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "PO Reference", Value = "", Key = "poReference", Editable = true },
            new() { Label = "Complaint Type", Value = "QUALITY", Key = "complaintType" },
            new() { Label = "Priority", Value = "MEDIUM", Key = "priority", Editable = true },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "tasks", Label = "Tasks", Icon = "bi-check2-square" },
            new() { Id = "defects", Label = "Defects", Icon = "bi-x-octagon" },
            new() { Id = "effects", Label = "Effects", Icon = "bi-graph-up" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "notificationNumber", Label = "Notification No.", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "vendorCode", Label = "Vendor Code", Type = "text", Width = 120, Editable = true },
            new() { Key = "vendorName", Label = "Vendor Name", Type = "text", Width = 180, Editable = true },
            new() { Key = "materialCode", Label = "Material", Type = "text", Width = 130, Editable = true },
            new() { Key = "poReference", Label = "PO Ref", Type = "text", Width = 120, Editable = true },
            new() { Key = "priority", Label = "Priority", Type = "dropdown", Width = 110, Editable = true, Options = new() {
                new() { Value = "LOW", Label = "Low" },
                new() { Value = "MEDIUM", Label = "Medium" },
                new() { Value = "HIGH", Label = "High" },
                new() { Value = "URGENT", Label = "Urgent" },
            }},
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OPEN", Label = "Open" },
                new() { Value = "IN_PROGRESS", Label = "In Progress" },
                new() { Value = "COMPLETED", Label = "Completed" },
                new() { Value = "CLOSED", Label = "Closed" },
                new() { Value = "CANCELLED", Label = "Cancelled" },
            }},
            new() { Key = "reportedBy", Label = "Reported By", Type = "text", Width = 130, Editable = true },
            new() { Key = "reportedDate", Label = "Reported Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Complaint", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save supplier complaint?" },
            new() { Id = "complete", Label = "Mark Complete", Icon = "bi-check-circle", Style = "success", Handler = "complete" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM1E1() => new()
    {
        TCode = "1E1", Title = "QM in Production", Module = "QM", Icon = "bi-gear-wide-connected",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Inspection", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Production Order", Value = "", Key = "productionOrderReference", Editable = true },
            new() { Label = "Material", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Batch", Value = "", Key = "batchNumber", Editable = true },
            new() { Label = "Inspection Type", Value = "02", Key = "inspectionType" },
            new() { Label = "Status", Value = "Active", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "operations", Label = "Operations", Icon = "bi-diagram-3" },
            new() { Id = "characteristics", Label = "Characteristics", Icon = "bi-list-check" },
            new() { Id = "sample", Label = "Sample", Icon = "bi-layers" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "configName", Label = "Config Name", Type = "text", Width = 200, Required = true, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "inspectionType", Label = "Insp. Type", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "02", Label = "Production" },
                new() { Value = "03", Label = "Stock Transfer" },
            }},
            new() { Key = "productionOrderReference", Label = "Prod. Order", Type = "text", Width = 140, Editable = true },
            new() { Key = "batchNumber", Label = "Batch", Type = "text", Width = 130, Editable = true },
            new() { Key = "materialCode", Label = "Material", Type = "text", Width = 130, Editable = true },
            new() { Key = "sampleSize", Label = "Sample Size", Type = "number", Width = 110, Editable = true, Validation = new() { Min = 0, Max = 9999 } },
            new() { Key = "inspectionLevel", Label = "Insp. Level", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "I", Label = "Level I" },
                new() { Value = "II", Label = "Level II" },
                new() { Value = "III", Label = "Level III" },
                new() { Value = "S1", Label = "S1" },
            }},
            new() { Key = "status", Label = "Status", Type = "status_icon", Width = 100, Options = new() {
                new() { Value = "ACTIVE", Label = "Active", Color = "success" },
                new() { Value = "INACTIVE", Label = "Inactive", Color = "secondary" },
                new() { Value = "BLOCKED", Label = "Blocked", Color = "danger" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Config", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save production inspection config?" },
            new() { Id = "activate", Label = "Activate", Icon = "bi-toggle-on", Style = "success", Handler = "activate" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM2QP() => new()
    {
        TCode = "2QP", Title = "Quality Notification - Internal Problems", Module = "QM", Icon = "bi-flag",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "Create Notification", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Material", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Production Order", Value = "", Key = "productionOrderReference", Editable = true },
            new() { Label = "Work Center", Value = "", Key = "workCenter", Editable = true },
            new() { Label = "Complaint Type", Value = "INTERNAL", Key = "complaintType" },
            new() { Label = "Priority", Value = "MEDIUM", Key = "priority", Editable = true },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "tasks", Label = "Tasks", Icon = "bi-check2-square" },
            new() { Id = "defects", Label = "Defects", Icon = "bi-x-octagon" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "notificationNumber", Label = "Notification No.", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "materialCode", Label = "Material", Type = "text", Width = 130, Editable = true },
            new() { Key = "productionOrderReference", Label = "Prod. Order", Type = "text", Width = 140, Editable = true },
            new() { Key = "workCenter", Label = "Work Center", Type = "text", Width = 130, Editable = true },
            new() { Key = "priority", Label = "Priority", Type = "dropdown", Width = 110, Editable = true, Options = new() {
                new() { Value = "LOW", Label = "Low" },
                new() { Value = "MEDIUM", Label = "Medium" },
                new() { Value = "HIGH", Label = "High" },
                new() { Value = "URGENT", Label = "Urgent" },
            }},
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OPEN", Label = "Open" },
                new() { Value = "IN_PROGRESS", Label = "In Progress" },
                new() { Value = "COMPLETED", Label = "Completed" },
                new() { Value = "CLOSED", Label = "Closed" },
            }},
            new() { Key = "reportedBy", Label = "Reported By", Type = "text", Width = 130, Editable = true },
            new() { Key = "reportedDate", Label = "Reported Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save internal notification?" },
            new() { Id = "complete", Label = "Mark Complete", Icon = "bi-check-circle", Style = "success", Handler = "complete" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM2QN() => new()
    {
        TCode = "2QN", Title = "Manual Inspection", Module = "QM", Icon = "bi-clipboard",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Inspection", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Material", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Batch", Value = "", Key = "batchNumber", Editable = true },
            new() { Label = "Inspector", Value = "", Key = "inspector", Editable = true },
            new() { Label = "Inspection Lot", Value = "", Key = "inspectionLot" },
            new() { Label = "Status", Value = "Active", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "results", Label = "Results", Icon = "bi-clipboard-check" },
            new() { Id = "defects", Label = "Defects", Icon = "bi-x-octagon" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "inspectionLot", Label = "Inspection Lot", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "materialCode", Label = "Material", Type = "text", Width = 130, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "batchNumber", Label = "Batch", Type = "text", Width = 130, Editable = true },
            new() { Key = "inspector", Label = "Inspector", Type = "text", Width = 140, Editable = true },
            new() { Key = "inspectionType", Label = "Insp. Type", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "01", Label = "Goods Receipt" },
                new() { Value = "02", Label = "Production" },
                new() { Value = "03", Label = "Stock Transfer" },
                new() { Value = "04", Label = "Manual" },
            }},
            new() { Key = "sampleSize", Label = "Sample Size", Type = "number", Width = 110, Editable = true, Validation = new() { Min = 0, Max = 9999 } },
            new() { Key = "result", Label = "Result", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OK", Label = "OK" },
                new() { Value = "NOK", Label = "Not OK" },
                new() { Value = "PARTIAL", Label = "Partial" },
            }},
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OPEN", Label = "Open" },
                new() { Value = "RECORDED", Label = "Recorded" },
                new() { Value = "COMPLETED", Label = "Completed" },
            }},
            new() { Key = "inspectionDate", Label = "Inspection Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Results", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save inspection results?" },
            new() { Id = "complete", Label = "Complete Inspection", Icon = "bi-check-circle", Style = "success", Handler = "complete" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QMQMM() => new()
    {
        TCode = "QMM", Title = "Recurring Batch Inspection", Module = "QM", Icon = "bi-arrow-repeat",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Batch Inspection", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Material", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Batch", Value = "", Key = "batchNumber", Editable = true },
            new() { Label = "Frequency", Value = "", Key = "frequency", Editable = true },
            new() { Label = "Next Inspection", Value = "", Key = "nextInspection" },
            new() { Label = "Status", Value = "Active", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "schedule", Label = "Schedule", Icon = "bi-calendar" },
            new() { Id = "results", Label = "Results", Icon = "bi-clipboard-check" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "batchNumber", Label = "Batch Number", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "materialCode", Label = "Material", Type = "text", Width = 130, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "frequency", Label = "Frequency", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 1, Max = 365 } },
            new() { Key = "frequencyUnit", Label = "Unit", Type = "dropdown", Width = 100, Editable = true, Options = new() {
                new() { Value = "Days", Label = "Days" },
                new() { Value = "Weeks", Label = "Weeks" },
                new() { Value = "Months", Label = "Months" },
            }},
            new() { Key = "nextInspection", Label = "Next Inspection", Type = "date", Width = 130, Editable = true },
            new() { Key = "lastInspection", Label = "Last Inspection", Type = "date", Width = 130 },
            new() { Key = "inspectionCount", Label = "Times Inspected", Type = "text", Width = 130 },
            new() { Key = "status", Label = "Status", Type = "status_icon", Width = 100, Options = new() {
                new() { Value = "ACTIVE", Label = "Active", Color = "success" },
                new() { Value = "OVERDUE", Label = "Overdue", Color = "danger" },
                new() { Value = "COMPLETED", Label = "Completed", Color = "secondary" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Schedule", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save batch inspection schedule?" },
            new() { Id = "execute", Label = "Execute Now", Icon = "bi-play-circle", Style = "success", Handler = "execute", Confirm = true, ConfirmMessage = "Execute inspection now?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM1MP() => new()
    {
        TCode = "1MP", Title = "Outbound Delivery Inspection", Module = "QM", Icon = "bi-truck",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Delivery Inspection", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Delivery", Value = "", Key = "deliveryReference", Editable = true },
            new() { Label = "Customer", Value = "", Key = "customerCode", Editable = true },
            new() { Label = "Customer Name", Value = "", Key = "customerName", Editable = true },
            new() { Label = "Material", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Batch", Value = "", Key = "batchNumber", Editable = true },
            new() { Label = "Status", Value = "Active", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "items", Label = "Delivery Items", Icon = "bi-box" },
            new() { Id = "inspection", Label = "Inspection", Icon = "bi-search" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "deliveryReference", Label = "Delivery No.", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "customerCode", Label = "Customer", Type = "text", Width = 120, Editable = true },
            new() { Key = "customerName", Label = "Customer Name", Type = "text", Width = 180, Editable = true },
            new() { Key = "materialCode", Label = "Material", Type = "text", Width = 130, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "batchNumber", Label = "Batch", Type = "text", Width = 130, Editable = true },
            new() { Key = "inspectionType", Label = "Insp. Type", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "04", Label = "Outbound" },
                new() { Value = "01", Label = "Goods Receipt" },
            }},
            new() { Key = "sampleSize", Label = "Sample Size", Type = "number", Width = 110, Editable = true, Validation = new() { Min = 0, Max = 9999 } },
            new() { Key = "result", Label = "Result", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OK", Label = "OK" },
                new() { Value = "NOK", Label = "Not OK" },
                new() { Value = "PARTIAL", Label = "Partial" },
            }},
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OPEN", Label = "Open" },
                new() { Value = "IN_PROGRESS", Label = "In Progress" },
                new() { Value = "COMPLETED", Label = "Completed" },
                new() { Value = "BLOCKED", Label = "Blocked" },
            }},
            new() { Key = "inspectionDate", Label = "Inspection Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save delivery inspection?" },
            new() { Id = "release", Label = "Release Delivery", Icon = "bi-send", Style = "success", Handler = "release", Confirm = true, ConfirmMessage = "Release delivery for shipment?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QMBKR() => new()
    {
        TCode = "BKR", Title = "Customer Return Inspection", Module = "QM", Icon = "bi-arrow-return-left",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Return Inspection", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Customer", Value = "", Key = "customerCode", Editable = true },
            new() { Label = "Customer Name", Value = "", Key = "customerName", Editable = true },
            new() { Label = "Material", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Return Order", Value = "", Key = "deliveryReference", Editable = true },
            new() { Label = "Reason", Value = "", Key = "reason", Editable = true },
            new() { Label = "Status", Value = "Open", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "defects", Label = "Defects", Icon = "bi-x-octagon" },
            new() { Id = "disposition", Label = "Disposition", Icon = "bi-gear" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "deliveryReference", Label = "Return No.", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "customerCode", Label = "Customer", Type = "text", Width = 120, Editable = true },
            new() { Key = "customerName", Label = "Customer Name", Type = "text", Width = 180, Editable = true },
            new() { Key = "materialCode", Label = "Material", Type = "text", Width = 130, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "reason", Label = "Return Reason", Type = "dropdown", Width = 140, Editable = true, Options = new() {
                new() { Value = "QUALITY", Label = "Quality Issue" },
                new() { Value = "DAMAGE", Label = "Damage" },
                new() { Value = "WRONG_ITEM", Label = "Wrong Item" },
                new() { Value = "OTHER", Label = "Other" },
            }},
            new() { Key = "inspectionType", Label = "Insp. Type", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "04", Label = "Return" },
                new() { Value = "01", Label = "Goods Receipt" },
            }},
            new() { Key = "sampleSize", Label = "Sample Size", Type = "number", Width = 110, Editable = true, Validation = new() { Min = 0, Max = 9999 } },
            new() { Key = "result", Label = "Result", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OK", Label = "OK" },
                new() { Value = "NOK", Label = "Not OK" },
                new() { Value = "PARTIAL", Label = "Partial" },
            }},
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OPEN", Label = "Open" },
                new() { Value = "IN_PROGRESS", Label = "In Progress" },
                new() { Value = "COMPLETED", Label = "Completed" },
                new() { Value = "BLOCKED", Label = "Blocked" },
            }},
            new() { Key = "inspectionDate", Label = "Inspection Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save return inspection?" },
            new() { Id = "dispose", Label = "Dispose", Icon = "bi-gear", Style = "warning", Handler = "dispose", Confirm = true, ConfirmMessage = "Dispose return items?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM2FA() => new()
    {
        TCode = "2FA", Title = "Quality Notification - Customer Complaints", Module = "QM", Icon = "bi-person-exclamation",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "Create Complaint", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Customer", Value = "", Key = "customerCode", Editable = true },
            new() { Label = "Customer Name", Value = "", Key = "customerName", Editable = true },
            new() { Label = "Material", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Sales Order", Value = "", Key = "deliveryReference", Editable = true },
            new() { Label = "Complaint Type", Value = "CUSTOMER", Key = "complaintType" },
            new() { Label = "Priority", Value = "MEDIUM", Key = "priority", Editable = true },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "tasks", Label = "Tasks", Icon = "bi-check2-square" },
            new() { Id = "defects", Label = "Defects", Icon = "bi-x-octagon" },
            new() { Id = "effects", Label = "Effects", Icon = "bi-graph-up" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "notificationNumber", Label = "Notification No.", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "customerCode", Label = "Customer", Type = "text", Width = 120, Editable = true },
            new() { Key = "customerName", Label = "Customer Name", Type = "text", Width = 180, Editable = true },
            new() { Key = "materialCode", Label = "Material", Type = "text", Width = 130, Editable = true },
            new() { Key = "deliveryReference", Label = "Sales Order", Type = "text", Width = 130, Editable = true },
            new() { Key = "priority", Label = "Priority", Type = "dropdown", Width = 110, Editable = true, Options = new() {
                new() { Value = "LOW", Label = "Low" },
                new() { Value = "MEDIUM", Label = "Medium" },
                new() { Value = "HIGH", Label = "High" },
                new() { Value = "URGENT", Label = "Urgent" },
            }},
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OPEN", Label = "Open" },
                new() { Value = "IN_PROGRESS", Label = "In Progress" },
                new() { Value = "COMPLETED", Label = "Completed" },
                new() { Value = "CLOSED", Label = "Closed" },
            }},
            new() { Key = "reportedBy", Label = "Reported By", Type = "text", Width = 130, Editable = true },
            new() { Key = "reportedDate", Label = "Reported Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Complaint", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save customer complaint?" },
            new() { Id = "complete", Label = "Mark Complete", Icon = "bi-check-circle", Style = "success", Handler = "complete" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QMCALIB() => new()
    {
        TCode = "CALIB", Title = "Calibration Inspection", Module = "QM", Icon = "bi-rulers",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Calibration", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Equipment", Value = "", Key = "equipmentCode", Editable = true },
            new() { Label = "Equipment Name", Value = "", Key = "equipmentName", Editable = true },
            new() { Label = "Calibration Date", Value = "", Key = "calibrationDate", Editable = true },
            new() { Label = "Next Due", Value = "", Key = "nextDueDate" },
            new() { Label = "Status", Value = "Scheduled", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "measurements", Label = "Measurements", Icon = "bi-speedometer2" },
            new() { Id = "tolerances", Label = "Tolerances", Icon = "bi-arrows-expand" },
            new() { Id = "certificate", Label = "Certificate", Icon = "bi-file-earmark-text" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "equipmentCode", Label = "Equipment", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "equipmentName", Label = "Equipment Name", Type = "text", Width = 200, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 100, Editable = true },
            new() { Key = "calibrationDate", Label = "Calibration Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "nextDueDate", Label = "Next Due", Type = "date", Width = 130, Editable = true },
            new() { Key = "calibrationType", Label = "Type", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "FULL", Label = "Full Calibration" },
                new() { Value = "PARTIAL", Label = "Partial Calibration" },
                new() { Value = "VERIFICATION", Label = "Verification Only" },
            }},
            new() { Key = "measuredValue", Label = "Measured Value", Type = "number", Width = 130, Editable = true, Validation = new() { Min = -99999, Max = 99999 } },
            new() { Key = "nominalValue", Label = "Nominal Value", Type = "number", Width = 120, Editable = true, Validation = new() { Min = -99999, Max = 99999 } },
            new() { Key = "toleranceUpper", Label = "Tol. Upper", Type = "number", Width = 110, Editable = true, Validation = new() { Min = 0, Max = 99999 } },
            new() { Key = "toleranceLower", Label = "Tol. Lower", Type = "number", Width = 110, Editable = true, Validation = new() { Min = 0, Max = 99999 } },
            new() { Key = "result", Label = "Result", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "PASS", Label = "Pass" },
                new() { Value = "FAIL", Label = "Fail" },
                new() { Value = "CONDITIONAL", Label = "Conditional" },
            }},
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "SCHEDULED", Label = "Scheduled" },
                new() { Value = "IN_PROGRESS", Label = "In Progress" },
                new() { Value = "COMPLETED", Label = "Completed" },
                new() { Value = "OVERDUE", Label = "Overdue" },
            }},
            new() { Key = "calibratedBy", Label = "Calibrated By", Type = "text", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save calibration result?" },
            new() { Id = "complete", Label = "Complete Calibration", Icon = "bi-check-circle", Style = "success", Handler = "complete", Confirm = true, ConfirmMessage = "Mark calibration complete?" },
            new() { Id = "certificate", Label = "Generate Certificate", Icon = "bi-file-earmark-pdf", Style = "warning", Handler = "generateCertificate" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig VL01N() => new()
    {
        TCode = "VL01N", Title = "Create Outbound Delivery", Module = "SD", Icon = "bi-truck",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Delivery", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Shipping Point", Value = "", Key = "shippingPoint", Editable = true },
            new() { Label = "Order Number", Value = "", Key = "orderNumber", Editable = true },
            new() { Label = "Delivery Date", Value = "", Key = "deliveryDate", Editable = true },
            new() { Label = "Ship-To Party", Value = "", Key = "shipToParty" },
            new() { Label = "Route", Value = "", Key = "route", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "items", Label = "Delivery Items", Icon = "bi-box", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "item", Label = "Item", Type = "number", Width = 60 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 220, Editable = true },
            new() { Key = "quantity", Label = "Delivery Qty", Type = "number", Width = 110, Editable = true, Validation = new() { Min = 1, Required = true } },
            new() { Key = "uom", Label = "UoM", Type = "text", Width = 60, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80, Editable = true },
            new() { Key = "storageLocation", Label = "Storage Loc", Type = "text", Width = 100, Editable = true },
            new() { Key = "weight", Label = "Gross Weight", Type = "number", Width = 110, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 110, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "PICKED", Label = "Picked", Color = "warning" },
                new() { Value = "SHIPPED", Label = "Shipped", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Delivery", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this outbound delivery?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig VL02N() => new()
    {
        TCode = "VL02N", Title = "Change Outbound Delivery / PGI", Module = "SD", Icon = "bi-truck",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Delivery", Value = "", Key = "deliveryNumber", Editable = true },
            new() { Label = "Shipping Point", Value = "", Key = "shippingPoint" },
            new() { Label = "Delivery Date", Value = "", Key = "deliveryDate" },
            new() { Label = "Ship-To Party", Value = "", Key = "shipToParty" },
            new() { Label = "Picking Status", Value = "", Key = "pickingStatus" },
            new() { Label = "PGI Status", Value = "", Key = "pgiStatus" },
        },
        Tabs = new()
        {
            new() { Id = "picking", Label = "Picking", Icon = "bi-box-arrow-right", Active = true },
            new() { Id = "packing", Label = "Packing", Icon = "bi-box" },
            new() { Id = "pgi", Label = "Post Goods Issue", Icon = "bi-send" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "item", Label = "Item", Type = "number", Width = 60 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140 },
            new() { Key = "description", Label = "Description", Type = "text", Width = 200 },
            new() { Key = "deliveryQty", Label = "Delivery Qty", Type = "number", Width = 110 },
            new() { Key = "pickedQty", Label = "Picked Qty", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 0 } },
            new() { Key = "storageLocation", Label = "Storage Loc", Type = "text", Width = 100, Editable = true },
            new() { Key = "handlingUnit", Label = "Handling Unit", Type = "text", Width = 140, Editable = true },
            new() { Key = "batch", Label = "Batch", Type = "text", Width = 120, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Changes", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save delivery changes?" },
            new() { Id = "pgi", Label = "Post Goods Issue", Icon = "bi-send", Style = "success", Handler = "pgi", Confirm = true, ConfirmMessage = "Post goods issue? Physical inventory will be reduced." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig VF01() => new()
    {
        TCode = "VF01", Title = "Create Billing Document", Module = "SD", Icon = "bi-receipt",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Invoice", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Outbound Delivery", Value = "", Key = "deliveryNumber", Editable = true },
            new() { Label = "Billing Type", Value = "F2", Key = "billingType", Editable = true },
            new() { Label = "Billing Date", Value = "", Key = "billingDate", Editable = true },
            new() { Label = "Payer", Value = "", Key = "payer" },
            new() { Label = "Net Value", Value = "", Key = "netValue" },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "items", Label = "Billing Items", Icon = "bi-receipt-cutoff", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "item", Label = "Item", Type = "number", Width = 60 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140 },
            new() { Key = "description", Label = "Description", Type = "text", Width = 220 },
            new() { Key = "quantity", Label = "Quantity", Type = "number", Width = 100 },
            new() { Key = "unitPrice", Label = "Unit Price", Type = "currency", Width = 110, Editable = true },
            new() { Key = "netValue", Label = "Net Value", Type = "currency", Width = 120 },
            new() { Key = "taxAmount", Label = "Tax", Type = "currency", Width = 100 },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Invoice", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create billing document? Accounting entry will be posted to AR." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig ME51N() => new()
    {
        TCode = "ME51N", Title = "Create Purchase Requisition", Module = "MM", Icon = "bi-cart-plus",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New PR", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "PR Type", Value = "NB", Key = "prType", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Purch. Org", Value = "", Key = "purchasingOrg", Editable = true },
            new() { Label = "Purch. Group", Value = "", Key = "purchasingGroup", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "items", Label = "PR Items", Icon = "bi-list-ol", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "item", Label = "Item", Type = "number", Width = 60 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 220, Editable = true },
            new() { Key = "quantity", Label = "Quantity", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 1, Required = true } },
            new() { Key = "uom", Label = "UoM", Type = "text", Width = 60, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80, Editable = true },
            new() { Key = "storageLocation", Label = "Storage Loc", Type = "text", Width = 100, Editable = true },
            new() { Key = "deliveryDate", Label = "Delivery Date", Type = "date", Width = 130, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "OPEN", Label = "Open", Color = "warning" },
                new() { Value = "PO_CREATED", Label = "PO Created", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create PR", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this purchase requisition?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig ME28() => new()
    {
        TCode = "ME28", Title = "PO Release / Approval", Module = "MM", Icon = "bi-shield-check",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "PO Number", Value = "", Key = "poNumber", Editable = true },
            new() { Label = "Vendor", Value = "", Key = "vendor" },
            new() { Label = "Net Value", Value = "", Key = "netValue" },
            new() { Label = "Release Strategy", Value = "", Key = "releaseStrategy" },
            new() { Label = "Status", Value = "IN_APPROVAL", Key = "status" },
        },
        Tabs = new() { new() { Id = "release", Label = "Release", Icon = "bi-shield-check", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "poNumber", Label = "PO Number", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "vendor", Label = "Vendor", Type = "text", Width = 140 },
            new() { Key = "netValue", Label = "Net Value", Type = "currency", Width = 120 },
            new() { Key = "releaseStrategy", Label = "Release Strategy", Type = "dropdown", Width = 160, Editable = true, Options = new() {
                new() { Value = "RF1", Label = "RF1 - Level 1" },
                new() { Value = "RF2", Label = "RF2 - Level 2" },
                new() { Value = "RF3", Label = "RF3 - Level 3" },
            }},
            new() { Key = "releaseCode", Label = "Release Code", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "R1", Label = "R1 - Approve" },
                new() { Value = "R2", Label = "R2 - Final Approve" },
                new() { Value = "X", Label = "X - Reject" },
            }},
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 120, Options = new() {
                new() { Value = "IN_APPROVAL", Label = "In Approval", Color = "warning" },
                new() { Value = "RELEASED", Label = "Released", Color = "success" },
                new() { Value = "REJECTED", Label = "Rejected", Color = "danger" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Release PO", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Release this purchase order?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig MIRO() => new()
    {
        TCode = "MIRO", Title = "Invoice Verification (LIV)", Module = "MM", Icon = "bi-file-earmark-text",
        ToolbarActions = new()
        {
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "primary", Handler = "simulate" },
            new() { Id = "save", Label = "Post", Icon = "bi-send", Style = "success", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "PO Number", Value = "", Key = "poNumber", Editable = true },
            new() { Label = "Invoice Date", Value = "", Key = "invoiceDate", Editable = true },
            new() { Label = "Invoice Amount", Value = "", Key = "invoiceAmount", Editable = true },
            new() { Label = "Tax Code", Value = "", Key = "taxCode", Editable = true },
            new() { Label = "Vendor", Value = "", Key = "vendor" },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "lineItems", Label = "Line Items", Icon = "bi-list-ol", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "poItem", Label = "PO Item", Type = "number", Width = 80 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140 },
            new() { Key = "description", Label = "Description", Type = "text", Width = 200 },
            new() { Key = "poQty", Label = "PO Qty", Type = "number", Width = 90 },
            new() { Key = "invoiceQty", Label = "Invoice Qty", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 0 } },
            new() { Key = "poPrice", Label = "PO Price", Type = "currency", Width = 110 },
            new() { Key = "invoicedAmount", Label = "Invoiced Amt", Type = "currency", Width = 120, Editable = true },
            new() { Key = "taxAmount", Label = "Tax", Type = "currency", Width = 100 },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Post Invoice", Icon = "bi-send", Style = "success", Handler = "save", Confirm = true, ConfirmMessage = "Post vendor invoice? AP line item will be created in FI." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig MD61() => new()
    {
        TCode = "MD61", Title = "Planned Independent Requirements", Module = "PP", Icon = "bi-calendar-range",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New PIR", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Material", Value = "", Key = "material", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Planning Horizon", Value = "", Key = "horizon", Editable = true },
            new() { Label = "Version", Value = "00", Key = "version" },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "schedule", Label = "Forecast Schedule", Icon = "bi-bar-chart", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "period", Label = "Period", Type = "text", Width = 100, Required = true, Editable = true },
            new() { Key = "year", Label = "Year", Type = "text", Width = 80, Editable = true },
            new() { Key = "forecastQty", Label = "Forecast Qty", Type = "number", Width = 120, Editable = true, Validation = new() { Min = 0, Required = true } },
            new() { Key = "actualQty", Label = "Actual Qty", Type = "number", Width = 110 },
            new() { Key = "deviation", Label = "Deviation %", Type = "number", Width = 100 },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "ACTIVE", Label = "Active", Color = "success" },
                new() { Value = "INACTIVE", Label = "Inactive", Color = "secondary" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save PIR", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save planned independent requirements?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig MD02() => new()
    {
        TCode = "MD02", Title = "Material Requirements Planning (MRP)", Module = "PP", Icon = "bi-gear-wide-connected",
        ToolbarActions = new()
        {
            new() { Id = "execute", Label = "Execute MRP", Icon = "bi-play-circle", Style = "success", Handler = "execute" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Material", Value = "", Key = "material", Editable = true },
            new() { Label = "MRP Type", Value = "PD", Key = "mrpType", Editable = true },
            new() { Label = "Planning Horizon", Value = "90", Key = "horizon", Editable = true },
            new() { Label = "Status", Value = "READY", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "results", Label = "Planning Results", Icon = "bi-list-check", Active = true },
            new() { Id = "exceptions", Label = "Exceptions", Icon = "bi-exclamation-triangle" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140 },
            new() { Key = "plannedOrder", Label = "Planned Order", Type = "text", Width = 140 },
            new() { Key = "purchaseReq", Label = "Purchase Req", Type = "text", Width = 130 },
            new() { Key = "quantity", Label = "Quantity", Type = "number", Width = 100 },
            new() { Key = "startDate", Label = "Start Date", Type = "date", Width = 120 },
            new() { Key = "endDate", Label = "End Date", Type = "date", Width = 120 },
            new() { Key = "type", Label = "Type", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "PLANNED_ORDER", Label = "Planned Order" },
                new() { Value = "PR", Label = "Purchase Requisition" },
                new() { Value = "RESCHEDULE", Label = "Reschedule" },
            }},
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 110, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "CONVERTED", Label = "Converted", Color = "success" },
                new() { Value = "OPEN", Label = "Open", Color = "warning" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "execute", Label = "Run MRP", Icon = "bi-play-circle", Style = "success", Handler = "execute", Confirm = true, ConfirmMessage = "Execute MRP? Planned orders and PRs will be generated." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig CO11N() => new()
    {
        TCode = "CO11N", Title = "Production Order Confirmation", Module = "PP", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Confirmation", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Order Number", Value = "", Key = "orderNumber", Editable = true },
            new() { Label = "Operation", Value = "", Key = "operation", Editable = true },
            new() { Label = "Work Center", Value = "", Key = "workCenter" },
            new() { Label = "Yield Qty", Value = "", Key = "yieldQty", Editable = true },
            new() { Label = "Scrap Qty", Value = "", Key = "scrapQty", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "confirmation", Label = "Confirmation", Icon = "bi-check2-square", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "orderNumber", Label = "Order", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "operation", Label = "Operation", Type = "text", Width = 100, Editable = true },
            new() { Key = "workCenter", Label = "Work Center", Type = "text", Width = 120 },
            new() { Key = "yieldQty", Label = "Yield Qty", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 0, Required = true } },
            new() { Key = "scrapQty", Label = "Scrap Qty", Type = "number", Width = 90, Editable = true },
            new() { Key = "laborHours", Label = "Labor Hours", Type = "number", Width = 100, Editable = true },
            new() { Key = "machineHours", Label = "Machine Hours", Type = "number", Width = 110, Editable = true },
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "NEW", Label = "New" },
                new() { Value = "CONFIRMED", Label = "Confirmed" },
                new() { Value = "REVERSED", Label = "Reversed" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Confirm Order", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Confirm this production order? Labor will be posted." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig FB60() => new()
    {
        TCode = "FB60", Title = "Vendor Invoice Entry", Module = "FI", Icon = "bi-file-earmark-text",
        ToolbarActions = new()
        {
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "primary", Handler = "simulate" },
            new() { Id = "save", Label = "Post", Icon = "bi-send", Style = "success", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Vendor ID", Value = "", Key = "vendorId", Editable = true },
            new() { Label = "Invoice Date", Value = "", Key = "invoiceDate", Editable = true },
            new() { Label = "Amount", Value = "", Key = "amount", Editable = true },
            new() { Label = "Tax Code", Value = "", Key = "taxCode", Editable = true },
            new() { Label = "Expense G/L", Value = "", Key = "expenseGl", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "lineItems", Label = "Line Items", Icon = "bi-list-ol", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "glAccount", Label = "G/L Account", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "amount", Label = "Amount", Type = "currency", Width = 120, Editable = true, Validation = new() { Required = true } },
            new() { Key = "taxCode", Label = "Tax Code", Type = "text", Width = 90, Editable = true },
            new() { Key = "costCenter", Label = "Cost Center", Type = "text", Width = 120, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Post Invoice", Icon = "bi-send", Style = "success", Handler = "save", Confirm = true, ConfirmMessage = "Post vendor invoice? AP open item will be created." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig F53() => new()
    {
        TCode = "F-53", Title = "Vendor Outgoing Payment", Module = "FI", Icon = "bi-cash-stack",
        ToolbarActions = new()
        {
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "primary", Handler = "simulate" },
            new() { Id = "save", Label = "Post", Icon = "bi-send", Style = "success", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Vendor ID", Value = "", Key = "vendorId", Editable = true },
            new() { Label = "Bank Account", Value = "", Key = "bankAccount", Editable = true },
            new() { Label = "Amount", Value = "", Key = "amount", Editable = true },
            new() { Label = "Payment Date", Value = "", Key = "paymentDate", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "items", Label = "Open Items", Icon = "bi-list-check", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "docNumber", Label = "Document", Type = "text", Width = 130 },
            new() { Key = "invoiceDate", Label = "Invoice Date", Type = "date", Width = 120 },
            new() { Key = "amount", Label = "Amount", Type = "currency", Width = 120 },
            new() { Key = "openAmount", Label = "Open Amount", Type = "currency", Width = 120 },
            new() { Key = "paymentAmount", Label = "Payment Amt", Type = "currency", Width = 120, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Post Payment", Icon = "bi-send", Style = "success", Handler = "save", Confirm = true, ConfirmMessage = "Post vendor payment? Vendor balance will be cleared." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig F28() => new()
    {
        TCode = "F-28", Title = "Customer Incoming Payment", Module = "FI", Icon = "bi-bank",
        ToolbarActions = new()
        {
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "primary", Handler = "simulate" },
            new() { Id = "save", Label = "Post", Icon = "bi-send", Style = "success", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Customer ID", Value = "", Key = "customerId", Editable = true },
            new() { Label = "Bank Account", Value = "", Key = "bankAccount", Editable = true },
            new() { Label = "Amount", Value = "", Key = "amount", Editable = true },
            new() { Label = "Payment Date", Value = "", Key = "paymentDate", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "items", Label = "Open Items", Icon = "bi-list-check", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "docNumber", Label = "Document", Type = "text", Width = 130 },
            new() { Key = "invoiceDate", Label = "Invoice Date", Type = "date", Width = 120 },
            new() { Key = "amount", Label = "Amount", Type = "currency", Width = 120 },
            new() { Key = "openAmount", Label = "Open Amount", Type = "currency", Width = 120 },
            new() { Key = "paymentAmount", Label = "Payment Amt", Type = "currency", Width = 120, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Post Payment", Icon = "bi-send", Style = "success", Handler = "save", Confirm = true, ConfirmMessage = "Post customer payment? A/R open item will be cleared." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig ABZN() => new()
    {
        TCode = "ABZN", Title = "Asset Acquisition", Module = "FI", Icon = "bi-building",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Asset", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Asset Class", Value = "", Key = "assetClass", Editable = true },
            new() { Label = "Description", Value = "", Key = "description", Editable = true },
            new() { Label = "Amount", Value = "", Key = "amount", Editable = true },
            new() { Label = "G/L Account", Value = "", Key = "glAccount", Editable = true },
            new() { Label = "Capitalization Date", Value = "", Key = "capDate", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "assets", Label = "Asset Details", Icon = "bi-building", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "assetNumber", Label = "Asset No.", Type = "text", Width = 120 },
            new() { Key = "assetClass", Label = "Asset Class", Type = "text", Width = 120, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "amount", Label = "Acquisition Value", Type = "currency", Width = 140, Editable = true, Validation = new() { Min = 0, Required = true } },
            new() { Key = "glAccount", Label = "G/L Account", Type = "text", Width = 110, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "CAPITALIZED", Label = "Capitalized", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Acquire Asset", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Acquire this asset? G/L will be updated." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig KB11N() => new()
    {
        TCode = "KB11N", Title = "Cost Center Allocation", Module = "CO", Icon = "bi-arrow-left-right",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Allocation", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Sender Cost Center", Value = "", Key = "senderCC", Editable = true },
            new() { Label = "Receiver Cost Center", Value = "", Key = "receiverCC", Editable = true },
            new() { Label = "Amount", Value = "", Key = "amount", Editable = true },
            new() { Label = "Period", Value = "", Key = "period", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "allocations", Label = "Allocations", Icon = "bi-arrow-left-right", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "senderCC", Label = "Sender CC", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "receiverCC", Label = "Receiver CC", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "amount", Label = "Amount", Type = "currency", Width = 120, Editable = true, Validation = new() { Min = 0, Required = true } },
            new() { Key = "costElement", Label = "Cost Element", Type = "text", Width = 120, Editable = true },
            new() { Key = "period", Label = "Period", Type = "text", Width = 80, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Post Allocation", Icon = "bi-send", Style = "success", Handler = "save", Confirm = true, ConfirmMessage = "Post cost center allocation?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig IE01() => new()
    {
        TCode = "IE01", Title = "Equipment Master Creation", Module = "PM", Icon = "bi-tools",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Equipment", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Equipment Category", Value = "", Key = "category", Editable = true },
            new() { Label = "Functional Location", Value = "", Key = "funcLocation", Editable = true },
            new() { Label = "Work Center", Value = "", Key = "workCenter", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "details", Label = "Equipment Details", Icon = "bi-tools", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "equipmentNumber", Label = "Equipment No.", Type = "text", Width = 130 },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Required = true, Editable = true },
            new() { Key = "category", Label = "Category", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "M", Label = "M - Machine" },
                new() { Value = "T", Label = "T - Tool" },
                new() { Value = "V", Label = "V - Vehicle" },
                new() { Value = "B", Label = "B - Building" },
            }},
            new() { Key = "funcLocation", Label = "Func. Location", Type = "text", Width = 140, Editable = true },
            new() { Key = "workCenter", Label = "Work Center", Type = "text", Width = 120, Editable = true },
            new() { Key = "manufacturer", Label = "Manufacturer", Type = "text", Width = 140, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "ACTIVE", Label = "Active", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Equipment", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this equipment master?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig IW21() => new()
    {
        TCode = "IW21", Title = "Create Maintenance Notification", Module = "PM", Icon = "bi-clipboard-plus",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Notification", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Notification Type", Value = "M1", Key = "notificationType", Editable = true },
            new() { Label = "Equipment ID", Value = "", Key = "equipmentId", Editable = true },
            new() { Label = "Description", Value = "", Key = "description", Editable = true },
            new() { Label = "Status", Value = "NOPR", Key = "status" },
        },
        Tabs = new() { new() { Id = "details", Label = "Notification Details", Icon = "bi-clipboard", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "notificationNumber", Label = "Notification No.", Type = "text", Width = 150 },
            new() { Key = "notificationType", Label = "Type", Type = "dropdown", Width = 100, Editable = true, Options = new() {
                new() { Value = "M1", Label = "M1 - Malfunction" },
                new() { Value = "M2", Label = "M2 - Maintenance" },
                new() { Value = "M3", Label = "M3 - Improvement" },
            }},
            new() { Key = "equipmentId", Label = "Equipment", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "damage", Label = "Damage", Type = "text", Width = 150, Editable = true },
            new() { Key = "cause", Label = "Cause", Type = "text", Width = 150, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "NOPR", Label = "Not Processed", Color = "warning" },
                new() { Value = "IN_PROCESS", Label = "In Process", Color = "info" },
                new() { Value = "COMPLETE", Label = "Complete", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Notification", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create maintenance notification?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig IW31() => new()
    {
        TCode = "IW31", Title = "Create Maintenance Order", Module = "PM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Order", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Order Type", Value = "PM01", Key = "orderType", Editable = true },
            new() { Label = "Equipment", Value = "", Key = "equipment", Editable = true },
            new() { Label = "Functional Location", Value = "", Key = "funcLocation", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "operations", Label = "Operations", Icon = "bi-list-check", Active = true },
            new() { Id = "spareParts", Label = "Spare Parts", Icon = "bi-box" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "orderNumber", Label = "Order No.", Type = "text", Width = 130 },
            new() { Key = "orderType", Label = "Type", Type = "dropdown", Width = 100, Editable = true, Options = new() {
                new() { Value = "PM01", Label = "PM01 - Maintenance" },
                new() { Value = "PM02", Label = "PM02 - Capital" },
            }},
            new() { Key = "equipment", Label = "Equipment", Type = "text", Width = 120, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "plannerGroup", Label = "Planner Group", Type = "text", Width = 120, Editable = true },
            new() { Key = "workCenter", Label = "Work Center", Type = "text", Width = 120, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 110, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "REL", Label = "Released", Color = "success" },
                new() { Value = "TECO", Label = "TECO", Color = "secondary" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Order", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this maintenance order?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig IW41() => new()
    {
        TCode = "IW41", Title = "PM Order Confirmation", Module = "PM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Confirmation", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "PM Order", Value = "", Key = "orderNumber", Editable = true },
            new() { Label = "Operation", Value = "", Key = "operation", Editable = true },
            new() { Label = "Actual Hours", Value = "", Key = "actualHours", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "confirmation", Label = "Confirmation", Icon = "bi-check2-square", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "orderNumber", Label = "PM Order", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "operation", Label = "Operation", Type = "text", Width = 100, Editable = true },
            new() { Key = "actualHours", Label = "Actual Hours", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 0 } },
            new() { Key = "workCenter", Label = "Work Center", Type = "text", Width = 120 },
            new() { Key = "notes", Label = "Notes", Type = "text", Width = 250, Editable = true },
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 110, Editable = true, Options = new() {
                new() { Value = "NEW", Label = "New" },
                new() { Value = "CONF", Label = "Confirmed" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Confirm", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Confirm PM order? Labor will be posted." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig IW32() => new()
    {
        TCode = "IW32", Title = "Change Maintenance Order / TECO", Module = "PM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "PM Order", Value = "", Key = "orderNumber", Editable = true },
            new() { Label = "Equipment", Value = "", Key = "equipment" },
            new() { Label = "Description", Value = "", Key = "description" },
            new() { Label = "Status", Value = "", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "operations", Label = "Operations", Icon = "bi-list-check" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "orderNumber", Label = "Order No.", Type = "text", Width = 120 },
            new() { Key = "operation", Label = "Operation", Type = "text", Width = 100 },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250 },
            new() { Key = "actualHours", Label = "Actual Hours", Type = "number", Width = 100 },
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "REL", Label = "Released" },
                new() { Value = "CONF", Label = "Confirmed" },
                new() { Value = "TECO", Label = "TECO" },
                new() { Value = "CLSD", Label = "Closed" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "teco", Label = "Technical Completion", Icon = "bi-check-circle", Style = "success", Handler = "complete", Confirm = true, ConfirmMessage = "Technically complete this PM order?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig QP01() => new()
    {
        TCode = "QP01", Title = "Create Inspection Plan", Module = "QM", Icon = "bi-clipboard-data",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Plan", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Material ID", Value = "", Key = "material", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Usage", Value = "", Key = "usage", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "characteristics", Label = "Characteristics", Icon = "bi-list-check", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "planNumber", Label = "Plan No.", Type = "text", Width = 120 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80, Editable = true },
            new() { Key = "usage", Label = "Usage", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "PURCHASE", Label = "Purchase" },
                new() { Value = "PRODUCTION", Label = "Production" },
                new() { Value = "STOCK_TRANSFER", Label = "Stock Transfer" },
            }},
            new() { Key = "characteristic", Label = "Characteristic", Type = "text", Width = 200, Editable = true },
            new() { Key = "specification", Label = "Specification", Type = "text", Width = 160, Editable = true },
            new() { Key = "uom", Label = "UoM", Type = "text", Width = 60, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "ACTIVE", Label = "Active", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Plan", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save inspection plan?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QA01() => new()
    {
        TCode = "QA01", Title = "Create Inspection Lot", Module = "QM", Icon = "bi-clipboard-plus",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Lot", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Material", Value = "", Key = "material", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Inspection Type", Value = "01", Key = "inspectionType", Editable = true },
            new() { Label = "Lot Origin", Value = "", Key = "lotOrigin", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "lots", Label = "Inspection Lots", Icon = "bi-clipboard", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "lotNumber", Label = "Lot Number", Type = "text", Width = 130 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80, Editable = true },
            new() { Key = "inspectionType", Label = "Insp. Type", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "01", Label = "Goods Receipt" },
                new() { Value = "02", Label = "Production" },
                new() { Value = "03", Label = "Stock Transfer" },
                new() { Value = "04", Label = "Manual" },
            }},
            new() { Key = "lotOrigin", Label = "Lot Origin", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "PO", Label = "Purchase Order" },
                new() { Value = "PROD", Label = "Production" },
                new() { Value = "MANUAL", Label = "Manual" },
            }},
            new() { Key = "quantity", Label = "Lot Qty", Type = "number", Width = 100, Editable = true },
            new() { Key = "stockStatus", Label = "Stock Status", Type = "status_badge", Width = 120, Options = new() {
                new() { Value = "QI", Label = "In QI", Color = "warning" },
                new() { Value = "FREE", Label = "Unrestricted", Color = "success" },
            }},
            new() { Key = "status", Label = "Lot Status", Type = "status_badge", Width = 110, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "RREC", Label = "Results Rec.", Color = "warning" },
                new() { Value = "UD", Label = "UD Posted", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Lot", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create inspection lot? Stock will be in QI status." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QN01() => new()
    {
        TCode = "QN01", Title = "Create Quality Notification (Defect)", Module = "QM", Icon = "bi-exclamation-triangle",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Notification", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Notification Type", Value = "Q1", Key = "notificationType", Editable = true },
            new() { Label = "Defect Type", Value = "", Key = "defectType", Editable = true },
            new() { Label = "Code Group", Value = "", Key = "codeGroup", Editable = true },
            new() { Label = "Fault Description", Value = "", Key = "faultDescription", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "details", Label = "Defect Details", Icon = "bi-exclamation-triangle", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "notificationNumber", Label = "Notification No.", Type = "text", Width = 150 },
            new() { Key = "notificationType", Label = "Type", Type = "dropdown", Width = 100, Editable = true, Options = new() {
                new() { Value = "Q1", Label = "Q1 - Quality" },
                new() { Value = "Q2", Label = "Q2 - Customer" },
                new() { Value = "Q5", Label = "Q5 - Internal" },
            }},
            new() { Key = "defectType", Label = "Defect Type", Type = "dropdown", Width = 140, Editable = true, Options = new() {
                new() { Value = "VISUAL", Label = "Visual" },
                new() { Value = "DIMENSIONAL", Label = "Dimensional" },
                new() { Value = "FUNCTIONAL", Label = "Functional" },
                new() { Value = "MATERIAL", Label = "Material" },
            }},
            new() { Key = "codeGroup", Label = "Code Group", Type = "text", Width = 140, Editable = true },
            new() { Key = "faultDescription", Label = "Fault Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 110, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "IN_PROCESS", Label = "In Process", Color = "warning" },
                new() { Value = "COMPLETE", Label = "Complete", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Notification", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create quality notification for non-conformance?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QA11() => new()
    {
        TCode = "QA11", Title = "Usage Decision & Stock Posting", Module = "QM", Icon = "bi-check2-square",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Decision", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Lot ID", Value = "", Key = "lotNumber", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant" },
            new() { Label = "Material", Value = "", Key = "material" },
            new() { Label = "UD Code", Value = "", Key = "udCode", Editable = true },
            new() { Label = "Stock Proposal", Value = "", Key = "stockProposal", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "usageDecision", Label = "Usage Decision", Icon = "bi-check2-square", Active = true },
            new() { Id = "stockPosting", Label = "Stock Posting", Icon = "bi-box" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "lotNumber", Label = "Lot ID", Type = "text", Width = 130, Required = true, Editable = true },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80 },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140 },
            new() { Key = "udCode", Label = "UD Code", Type = "dropdown", Width = 150, Editable = true, Required = true, Options = new() {
                new() { Value = "A", Label = "A - Accept" },
                new() { Value = "R", Label = "R - Reject" },
                new() { Value = "R1", Label = "R1 - Rework" },
                new() { Value = "N", Label = "N - Return to Vendor" },
                new() { Value = "S", Label = "S - Scrap" },
            }},
            new() { Key = "stockProposal", Label = "Stock Proposal", Type = "dropdown", Width = 150, Editable = true, Required = true, Options = new() {
                new() { Value = "FREE", Label = "Unrestricted Use" },
                new() { Value = "QI", Label = "Quality Inspection" },
                new() { Value = "BLOCKED", Label = "Blocked Stock" },
                new() { Value = "SAMPLE", Label = "Sample" },
            }},
            new() { Key = "fromStock", Label = "From Stock", Type = "text", Width = 110 },
            new() { Key = "toStock", Label = "To Stock", Type = "text", Width = 110 },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "UD_POSTED", Label = "UD Posted", Color = "success" },
                new() { Value = "STOCK_POSTED", Label = "Stock Posted", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Post UD & Stock", Icon = "bi-send", Style = "success", Handler = "save", Confirm = true, ConfirmMessage = "Post usage decision and stock posting?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QC21() => new()
    {
        TCode = "QC21", Title = "Quality Certificate (COA)", Module = "QM", Icon = "bi-file-earmark-medical",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Certificate", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "delete", Label = "Delete", Icon = "bi-trash", Style = "danger", Handler = "deleteRow", Confirm = true, ConfirmMessage = "Delete selected?" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Delivery Number", Value = "", Key = "deliveryNumber", Editable = true },
            new() { Label = "Lot Number", Value = "", Key = "lotNumber", Editable = true },
            new() { Label = "Material", Value = "", Key = "material" },
            new() { Label = "Customer", Value = "", Key = "customer" },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new() { new() { Id = "certificate", Label = "Certificate Details", Icon = "bi-file-earmark-medical", Active = true } },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "certificateNumber", Label = "Cert. No.", Type = "text", Width = 130 },
            new() { Key = "deliveryNumber", Label = "Delivery", Type = "text", Width = 130, Editable = true },
            new() { Key = "lotNumber", Label = "Lot No.", Type = "text", Width = 120, Editable = true },
            new() { Key = "material", Label = "Material", Type = "text", Width = 140 },
            new() { Key = "customer", Label = "Customer", Type = "text", Width = 140 },
            new() { Key = "certificateDate", Label = "Cert. Date", Type = "date", Width = 120, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 110, Options = new() {
                new() { Value = "NEW", Label = "New", Color = "info" },
                new() { Value = "GENERATED", Label = "Generated", Color = "success" },
                new() { Value = "SENT", Label = "Sent", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Generate COA", Icon = "bi-file-earmark-pdf", Style = "primary", Handler = "printCOA", Confirm = true, ConfirmMessage = "Generate Certificate of Analysis PDF?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig KO88() => new()
    {
        TCode = "KO88", Title = "Settle Production / PM Order", Module = "CO", Icon = "bi-currency-dollar",
        ToolbarActions = new()
        {
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "primary", Handler = "simulate" },
            new() { Id = "save", Label = "Settle", Icon = "bi-send", Style = "success", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Order Number", Value = "", Key = "orderNumber", Editable = true },
            new() { Label = "Order Type", Value = "", Key = "orderType" },
            new() { Label = "Settlement Period", Value = "", Key = "period", Editable = true },
            new() { Label = "Fiscal Year", Value = "", Key = "fiscalYear", Editable = true },
            new() { Label = "Settlement Rule", Value = "", Key = "settlementRule" },
            new() { Label = "Status", Value = "", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "settlement", Label = "Settlement", Icon = "bi-currency-dollar", Active = true },
            new() { Id = "costAnalysis", Label = "Cost Analysis", Icon = "bi-bar-chart" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "costElement", Label = "Cost Element", Type = "text", Width = 140 },
            new() { Key = "description", Label = "Description", Type = "text", Width = 220 },
            new() { Key = "planCost", Label = "Plan Cost", Type = "currency", Width = 120, Align = "right" },
            new() { Key = "actualCost", Label = "Actual Cost", Type = "currency", Width = 120, Align = "right" },
            new() { Key = "variance", Label = "Variance", Type = "currency", Width = 120, Align = "right" },
            new() { Key = "settledAmount", Label = "Settled Amount", Type = "currency", Width = 130, Editable = true, Align = "right" },
            new() { Key = "receiver", Label = "Settlement Receiver", Type = "text", Width = 160, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Settle Order", Icon = "bi-send", Style = "success", Handler = "save", Confirm = true, ConfirmMessage = "Settle this order? Actual costs will be posted to receiver." },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig KS01() => new()
    {
        TCode = "KS01", Title = "Create Cost Center", Module = "CO", Icon = "bi-diagram-3",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Cost Center", Value = "", Key = "costCenter", Editable = true },
            new() { Label = "Name", Value = "", Key = "name", Editable = true },
            new() { Label = "Cost Center Category", Value = "", Key = "category", Editable = true },
            new() { Label = "Company Code", Value = "", Key = "companyCode", Editable = true },
            new() { Label = "Business Area", Value = "", Key = "businessArea", Editable = true },
            new() { Label = "Valid From", Value = "", Key = "validFrom", Editable = true },
            new() { Label = "Valid To", Value = "", Key = "validTo", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "basic", Label = "Basic Data", Icon = "bi-card-heading", Active = true },
            new() { Id = "address", Label = "Address", Icon = "bi-geo-alt" },
            new() { Id = "control", Label = "Control", Icon = "bi-sliders" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 200 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 300, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Cost Center", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this cost center?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig BP() => new()
    {
        TCode = "BP", Title = "Create Business Partner (Vendor/Customer)", Module = "MM", Icon = "bi-person-lines-fill",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "BP Category", Value = "", Key = "bpCategory", Editable = true },
            new() { Label = "BP Role", Value = "", Key = "bpRole", Editable = true },
            new() { Label = "Search Term", Value = "", Key = "searchTerm", Editable = true },
            new() { Label = "Name 1", Value = "", Key = "name1", Editable = true },
            new() { Label = "Country", Value = "", Key = "country", Editable = true },
            new() { Label = "Language", Value = "", Key = "language", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General Data", Icon = "bi-person", Active = true },
            new() { Id = "vendorArea", Label = "Vendor Functions", Icon = "bi-bag" },
            new() { Id = "customerArea", Label = "Customer Functions", Icon = "bi-cart3" },
            new() { Id = "companyCode", Label = "Company Code Data", Icon = "bi-building" },
            new() { Id = "purchasing", Label = "Purchasing Data", Icon = "bi-bag-plus" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 200 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 300, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Partner", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this business partner?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig CS01() => new()
    {
        TCode = "CS01", Title = "Create Bill of Materials", Module = "PP", Icon = "bi-layers",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Material", Value = "", Key = "material", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "BOM Usage", Value = "", Key = "bomUsage", Editable = true },
            new() { Label = "Valid From", Value = "", Key = "validFrom", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "items", Label = "BOM Items", Icon = "bi-list-ol", Active = true },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "item", Label = "Item", Type = "number", Width = 60 },
            new() { Key = "component", Label = "Component", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 220, Editable = true },
            new() { Key = "quantity", Label = "Quantity", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 0.001m, Required = true } },
            new() { Key = "uom", Label = "UoM", Type = "text", Width = 60, Editable = true },
            new() { Key = "itemCategory", Label = "Category", Type = "dropdown", Width = 110, Editable = true, Options = new() {
                new() { Value = "L", Label = "Standard" },
                new() { Value = "R", Label = "Phantom" },
                new() { Value = "T", Label = "Text" },
            }},
            new() { Key = "validFrom", Label = "Valid From", Type = "date", Width = 120, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create BOM", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this bill of materials?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = false, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig CR01() => new()
    {
        TCode = "CR01", Title = "Create Work Center", Module = "PP", Icon = "bi-gear",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Work Center", Value = "", Key = "workCenter", Editable = true },
            new() { Label = "Description", Value = "", Key = "description", Editable = true },
            new() { Label = "Work Center Category", Value = "", Key = "category", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Work Center Category", Value = "", Key = "wcCategory", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "basic", Label = "Basic Data", Icon = "bi-card-heading", Active = true },
            new() { Id = "capacities", Label = "Capacities", Icon = "bi-clock-history" },
            new() { Id = "scheduling", Label = "Scheduling", Icon = "bi-calendar" },
            new() { Id = "costing", Label = "Costing", Icon = "bi-currency-dollar" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 200 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 300, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Work Center", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this work center?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig IL01() => new()
    {
        TCode = "IL01", Title = "Create Functional Location", Module = "PM", Icon = "bi-geo-alt",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Functional Location", Value = "", Key = "funcLocation", Editable = true },
            new() { Label = "Description", Value = "", Key = "description", Editable = true },
            new() { Label = "Location Category", Value = "", Key = "category", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General Data", Icon = "bi-card-heading", Active = true },
            new() { Id = "location", Label = "Location", Icon = "bi-geo-alt" },
            new() { Id = "organization", Label = "Organization", Icon = "bi-diagram-3" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 200 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 300, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 250, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Func. Location", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this functional location?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    // ══════════════════════════════════════════════════════════════════════════
    // Customer Complaint & Return with Supplier Pass-Through Claim
    // ══════════════════════════════════════════════════════════════════════════

    private static TCodeLayoutConfig CRRETURN() => new()
    {
        TCode = "CRRETURN", Title = "Customer Complaint & Return Order", Module = "SD", Icon = "bi-arrow-return-left",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Complaint", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "workflow", Label = "Start Workflow", Icon = "bi-play-circle", Style = "info", Handler = "startWorkflow" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Complaint Number", Value = "", Key = "complaintNumber" },
            new() { Label = "Complaint Type", Value = "Q1", Key = "complaintType", Editable = true },
            new() { Label = "Return Order Type", Value = "RE", Key = "returnType", Editable = true },
            new() { Label = "Customer Code", Value = "", Key = "customerCode", Editable = true },
            new() { Label = "Customer Name", Value = "", Key = "customerName", Editable = true },
            new() { Label = "Material Code", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Material Name", Value = "", Key = "materialName", Editable = true },
            new() { Label = "Return Quantity", Value = "0", Key = "returnQuantity", Editable = true },
            new() { Label = "Unit Price", Value = "0", Key = "unitPrice", Editable = true },
            new() { Label = "Batch Number", Value = "", Key = "batchNumber", Editable = true },
            new() { Label = "Defect Code", Value = "", Key = "defectCode", Editable = true },
            new() { Label = "Defect Description", Value = "", Key = "defectDescription", Editable = true },
            new() { Label = "Supplier Vendor Code", Value = "", Key = "supplierVendorCode", Editable = true },
            new() { Label = "Supplier Batch Number", Value = "", Key = "supplierBatchNumber", Editable = true },
            new() { Label = "Plant", Value = "PLT-01", Key = "plant", Editable = true },
            new() { Label = "Status", Value = "CREATED", Key = "status" },
            new() { Label = "Current Step", Value = "", Key = "currentStep" },
        },
        Tabs = new()
        {
            new() { Id = "complaint", Label = "Complaint Details", Icon = "bi-exclamation-circle", Active = true },
            new() { Id = "return", Label = "Return Order", Icon = "bi-arrow-return-left" },
            new() { Id = "quality", Label = "Quality", Icon = "bi-clipboard-check" },
            new() { Id = "financials", Label = "Financials", Icon = "bi-calculator" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "complaintNumber", Label = "Complaint #", Type = "text", Width = 160, Required = true },
            new() { Key = "customerName", Label = "Customer", Type = "text", Width = 200, Editable = true },
            new() { Key = "materialName", Label = "Material", Type = "text", Width = 200, Editable = true },
            new() { Key = "returnQuantity", Label = "Qty", Type = "number", Width = 100, Editable = true },
            new() { Key = "unitPrice", Label = "Unit Price", Type = "currency", Width = 120, Editable = true },
            new() { Key = "returnAmount", Label = "Amount", Type = "currency", Width = 120 },
            new() { Key = "defectCode", Label = "Defect", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "SURFACE", Label = "Surface Defect" },
                new() { Value = "DIMENSION", Label = "Dimensional" },
                new() { Value = "FUNCTIONAL", Label = "Functional" },
                new() { Value = "MATERIAL", Label = "Material Defect" },
                new() { Value = "PACKAGING", Label = "Packaging" },
                new() { Value = "CONTAMINATION", Label = "Contamination" },
            }},
            new() { Key = "supplierVendorCode", Label = "Supplier", Type = "text", Width = 150, Editable = true },
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 130, Options = new() {
                new() { Value = "CREATED", Label = "Created" },
                new() { Value = "RETURN_RECEIVED", Label = "Return Received" },
                new() { Value = "INSPECTION_COMPLETED", Label = "Inspection Done" },
                new() { Value = "USAGE_DECIDED", Label = "UD Posted" },
                new() { Value = "CREDIT_MEMO_ISSUED", Label = "Credit Memo" },
                new() { Value = "SUPPLIER_CLAIM_CREATED", Label = "Supplier Claim" },
                new() { Value = "SUPPLIER_RETURN_POSTED", Label = "Supplier Return" },
                new() { Value = "RECOVERY_COMPLETED", Label = "Recovery Done" },
                new() { Value = "CLOSED", Label = "Closed" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Complaint", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save complaint?" },
            new() { Id = "startWorkflow", Label = "Start Full Workflow", Icon = "bi-play-circle", Style = "info", Handler = "startWorkflow", Confirm = true, ConfirmMessage = "Execute full complaint workflow?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig CRINSPECT() => new()
    {
        TCode = "CRINSPECT", Title = "Quality Inspection - Return Analysis", Module = "QM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Result", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "ud", Label = "Usage Decision", Icon = "bi-check-circle", Style = "info", Handler = "usageDecision" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Inspection Lot", Value = "", Key = "inspectionLot", Editable = true },
            new() { Label = "Material", Value = "", Key = "material" },
            new() { Label = "Batch", Value = "", Key = "batch" },
            new() { Label = "Supplier Batch", Value = "", Key = "supplierBatch" },
            new() { Label = "Quantity", Value = "", Key = "quantity" },
            new() { Label = "Plant", Value = "PLT-01", Key = "plant" },
            new() { Label = "Status", Value = "OPEN", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "results", Label = "Inspection Results", Icon = "bi-clipboard-data", Active = true },
            new() { Id = "defects", Label = "Defect Analysis", Icon = "bi-exclamation-triangle" },
            new() { Id = "rootCause", Label = "Root Cause", Icon = "bi-search" },
            new() { Id = "ud", Label = "Usage Decision", Icon = "bi-check-circle" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "characteristic", Label = "Characteristic", Type = "text", Width = 200, Required = true, Editable = true },
            new() { Key = "specification", Label = "Specification", Type = "text", Width = 180, Editable = true },
            new() { Key = "resultValue", Label = "Result", Type = "number", Editable = true, Width = 120, Validation = new() { Min = 0, Max = 99999, Required = true } },
            new() { Key = "resultValuation", Label = "Valuation", Type = "dropdown", Width = 120, Editable = true, Options = new() {
                new() { Value = "OK", Label = "OK" },
                new() { Value = "NOK", Label = "Not OK" },
                new() { Value = "REVIEW", Label = "Review" },
            }},
            new() { Key = "defectCode", Label = "Defect Code", Type = "dropdown", Width = 150, Editable = true, Options = new() {
                new() { Value = "SURFACE", Label = "Surface Defect" },
                new() { Value = "DIMENSION", Label = "Dimensional" },
                new() { Value = "FUNCTIONAL", Label = "Functional" },
                new() { Value = "MATERIAL", Label = "Material Defect" },
                new() { Value = "CONTAMINATION", Label = "Contamination" },
            }},
            new() { Key = "rootCause", Label = "Root Cause", Type = "dropdown", Width = 180, Editable = true, Options = new() {
                new() { Value = "SUPPLIER-RAW", Label = "Supplier Raw Material" },
                new() { Value = "SUPPLIER-PROCESS", Label = "Supplier Process" },
                new() { Value = "SUPPLIER-STORAGE", Label = "Supplier Storage" },
                new() { Value = "INTERNAL-PROCESS", Label = "Internal Process" },
                new() { Value = "TRANSPORT", Label = "Transport Damage" },
                new() { Value = "CUSTOMER-MISHANDLING", Label = "Customer Mishandling" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Results", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save inspection results?" },
            new() { Id = "usageDecision", Label = "Post Usage Decision", Icon = "bi-check-circle", Style = "info", Handler = "usageDecision", Confirm = true, ConfirmMessage = "Post usage decision?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig CRUDPOST() => new()
    {
        TCode = "CRUDPOST", Title = "Post Usage Decision - Return", Module = "QM", Icon = "bi-check-circle",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Post Decision", Icon = "bi-send", Style = "primary", Handler = "post" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Inspection Lot", Value = "", Key = "inspectionLot" },
            new() { Label = "Material", Value = "", Key = "material" },
            new() { Label = "Usage Decision", Value = "", Key = "usageDecision", Editable = true },
            new() { Label = "UD Code", Value = "", Key = "udCode", Editable = true },
            new() { Label = "Stock Proposal", Value = "", Key = "stockProposal", Editable = true },
            new() { Label = "Target Stock Type", Value = "BLOCKED", Key = "targetStockType", Editable = true },
            new() { Label = "Decided By", Value = "", Key = "decidedBy", Editable = true },
            new() { Label = "Status", Value = "OPEN", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "decision", Label = "Usage Decision", Icon = "bi-check-circle", Active = true },
            new() { Id = "stock", Label = "Stock Transfer", Icon = "bi-box-arrow-right" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 200 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 300, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "post", Label = "Post Usage Decision", Icon = "bi-send", Style = "primary", Handler = "post", Confirm = true, ConfirmMessage = "Post usage decision and move stock?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig CRCREDIT() => new()
    {
        TCode = "CRCREDIT", Title = "Customer Credit Memo - Return", Module = "SD", Icon = "bi-credit-card",
        ToolbarActions = new()
        {
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "default", Handler = "simulate" },
            new() { Id = "post", Label = "Post", Icon = "bi-send", Style = "primary", Handler = "post" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Billing Type", Value = "RE", Key = "billingType" },
            new() { Label = "Complaint Number", Value = "", Key = "complaintNumber" },
            new() { Label = "Customer Code", Value = "", Key = "customerCode" },
            new() { Label = "Customer Name", Value = "", Key = "customerName" },
            new() { Label = "Material", Value = "", Key = "material" },
            new() { Label = "Return Quantity", Value = "", Key = "returnQuantity" },
            new() { Label = "Credit Amount", Value = "", Key = "creditAmount", Editable = true },
            new() { Label = "Currency", Value = "INR", Key = "currency" },
            new() { Label = "Cost Center", Value = "", Key = "costCenter", Editable = true },
            new() { Label = "GL Account", Value = "", Key = "glAccount", Editable = true },
            new() { Label = "Status", Value = "PENDING", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Credit Memo Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "lineItems", Label = "Line Items", Icon = "bi-list-ol" },
            new() { Id = "accounting", Label = "Accounting", Icon = "bi-calculator" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 200 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 300, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "post", Label = "Post Credit Memo", Icon = "bi-send", Style = "primary", Handler = "post", Confirm = true, ConfirmMessage = "Issue credit memo to customer?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig CRSUPPLY() => new()
    {
        TCode = "CRSUPPLY", Title = "Supplier Complaint & Claim", Module = "QM", Icon = "bi-person-exclamation",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Claim", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "notify", Label = "Notify Supplier", Icon = "bi-envelope", Style = "info", Handler = "notifySupplier" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Supplier Claim Number", Value = "", Key = "supplierClaimNumber" },
            new() { Label = "Complaint Type", Value = "Q2", Key = "complaintType" },
            new() { Label = "Vendor Code", Value = "", Key = "vendorCode", Editable = true },
            new() { Label = "Vendor Name", Value = "", Key = "vendorName", Editable = true },
            new() { Label = "Material Code", Value = "", Key = "materialCode", Editable = true },
            new() { Label = "Material Name", Value = "", Key = "materialName", Editable = true },
            new() { Label = "Supplier Batch", Value = "", Key = "supplierBatch", Editable = true },
            new() { Label = "Claim Quantity", Value = "0", Key = "claimQuantity", Editable = true },
            new() { Label = "Claim Amount", Value = "0", Key = "claimAmount", Editable = true },
            new() { Label = "Defect Code", Value = "", Key = "defectCode", Editable = true },
            new() { Label = "Root Cause", Value = "", Key = "rootCause", Editable = true },
            new() { Label = "Plant", Value = "PLT-01", Key = "plant", Editable = true },
            new() { Label = "Status", Value = "CREATED", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "claim", Label = "Claim Details", Icon = "bi-exclamation-circle", Active = true },
            new() { Id = "defect", Label = "Defect Analysis", Icon = "bi-search" },
            new() { Id = "recovery", Label = "Recovery", Icon = "bi-cash" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "supplierClaimNumber", Label = "Claim #", Type = "text", Width = 160 },
            new() { Key = "vendorName", Label = "Vendor", Type = "text", Width = 200, Editable = true },
            new() { Key = "materialName", Label = "Material", Type = "text", Width = 200, Editable = true },
            new() { Key = "claimQuantity", Label = "Qty", Type = "number", Width = 100, Editable = true },
            new() { Key = "claimAmount", Label = "Amount", Type = "currency", Width = 120, Editable = true },
            new() { Key = "defectCode", Label = "Defect", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "RAW-IMPURITY", Label = "Raw Material Impurity" },
                new() { Value = "RAW-SPEC", Label = "Out of Specification" },
                new() { Value = "RAW-CONTAMINATION", Label = "Contamination" },
                new() { Value = "PROCESS", Label = "Supplier Process Defect" },
                new() { Value = "PACKAGING", Label = "Packaging Defect" },
            }},
            new() { Key = "status", Label = "Status", Type = "dropdown", Width = 130, Options = new() {
                new() { Value = "CREATED", Label = "Created" },
                new() { Value = "SUPPLIER_NOTIFIED", Label = "Supplier Notified" },
                new() { Value = "SUPPLIER_RETURN_POSTED", Label = "Return Posted" },
                new() { Value = "RECOVERY_COMPLETED", Label = "Recovery Done" },
                new() { Value = "CLOSED", Label = "Closed" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Claim", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save supplier claim?" },
            new() { Id = "notifySupplier", Label = "Notify Supplier", Icon = "bi-envelope", Style = "info", Handler = "notifySupplier", Confirm = true, ConfirmMessage = "Send notification to supplier?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig CRSRET() => new()
    {
        TCode = "CRSRET", Title = "Supplier Return Delivery (Mvt 122)", Module = "MM", Icon = "bi-box-arrow-right",
        ToolbarActions = new()
        {
            new() { Id = "check", Label = "Check", Icon = "bi-check-circle", Style = "primary", Handler = "validate" },
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "default", Handler = "simulate" },
            new() { Id = "post", Label = "Post", Icon = "bi-send", Style = "success", Handler = "post" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Movement Type", Value = "122", Key = "movementType" },
            new() { Label = "Supplier Claim Number", Value = "", Key = "supplierClaimNumber" },
            new() { Label = "Vendor Code", Value = "", Key = "vendorCode" },
            new() { Label = "Vendor Name", Value = "", Key = "vendorName" },
            new() { Label = "Material", Value = "", Key = "material" },
            new() { Label = "Quantity", Value = "0", Key = "quantity", Editable = true },
            new() { Label = "Batch", Value = "", Key = "batch", Editable = true },
            new() { Label = "Plant", Value = "PLT-01", Key = "plant", Editable = true },
            new() { Label = "Storage Location", Value = "", Key = "storageLocation", Editable = true },
            new() { Label = "PO Reference", Value = "", Key = "poReference" },
            new() { Label = "Posting Date", Value = "", Key = "postingDate", Type = "date", Editable = true },
            new() { Label = "Status", Value = "PENDING", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "items", Label = "Item Overview", Icon = "bi-list-ol", Active = true },
            new() { Id = "vendor", Label = "Vendor", Icon = "bi-person" },
            new() { Id = "accounting", Label = "Accounting", Icon = "bi-calculator" },
        },
        Columns = new()
        {
            new() { Key = "material", Label = "Material", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "description", Label = "Description", Type = "text", Width = 200, Editable = true },
            new() { Key = "quantity", Label = "Qty", Type = "number", Editable = true, Width = 90, Validation = new() { Min = 0.001m, Required = true } },
            new() { Key = "uom", Label = "UoM", Type = "text", Width = 70, Editable = true },
            new() { Key = "unitPrice", Label = "Unit Price", Type = "currency", Width = 110, Editable = true },
            new() { Key = "totalValue", Label = "Total Value", Type = "currency", Width = 120 },
            new() { Key = "batch", Label = "Batch", Type = "text", Width = 120, Editable = true },
            new() { Key = "poReference", Label = "PO Reference", Type = "text", Width = 130 },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "post", Label = "Post Supplier Return", Icon = "bi-send", Style = "primary", Handler = "post", Confirm = true, ConfirmMessage = "Post supplier return delivery (Mvt 122)?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig CRDEBIT() => new()
    {
        TCode = "CRDEBIT", Title = "Supplier Debit Memo (Credit Recovery)", Module = "FI", Icon = "bi-cash-stack",
        ToolbarActions = new()
        {
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "default", Handler = "simulate" },
            new() { Id = "post", Label = "Post", Icon = "bi-send", Style = "primary", Handler = "post" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Document Type", Value = "Credit Memo", Key = "documentType" },
            new() { Label = "Supplier Claim Number", Value = "", Key = "supplierClaimNumber" },
            new() { Label = "Vendor Code", Value = "", Key = "vendorCode" },
            new() { Label = "Vendor Name", Value = "", Key = "vendorName" },
            new() { Label = "Debit Amount", Value = "0", Key = "debitAmount", Editable = true },
            new() { Label = "Currency", Value = "INR", Key = "currency" },
            new() { Label = "PO Reference", Value = "", Key = "poReference" },
            new() { Label = "Cost Center", Value = "", Key = "costCenter", Editable = true },
            new() { Label = "GL Account", Value = "", Key = "glAccount", Editable = true },
            new() { Label = "Status", Value = "PENDING", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Debit Memo Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "accounting", Label = "Accounting", Icon = "bi-calculator" },
            new() { Id = "recovery", Label = "Recovery Details", Icon = "bi-cash" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 200 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 300, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "post", Label = "Post Debit Memo", Icon = "bi-send", Style = "primary", Handler = "post", Confirm = true, ConfirmMessage = "Issue debit memo and recover from supplier?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig SOXADM() => new()
    {
        TCode = "SOXADM", Title = "SOX Compliance Administration", Module = "GRC", Icon = "bi-shield-check",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Duty", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "violations", Label = "Violations", Icon = "bi-exclamation-triangle", Style = "warning", Handler = "violations" },
            new() { Id = "audittrail", Label = "Audit Trail", Icon = "bi-clock-history", Style = "default", Handler = "audittrail" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Compliance Status", Value = "", Key = "complianceStatus" },
            new() { Label = "Total Duties", Value = "0", Key = "totalDuties" },
            new() { Label = "Active Assignments", Value = "0", Key = "activeAssignments" },
            new() { Label = "Violations Count", Value = "0", Key = "violationsCount" },
        },
        Tabs = new()
        {
            new() { Id = "duties", Label = "SoD Duties", Icon = "bi-list-check", Active = true },
            new() { Id = "assignments", Label = "Assignments", Icon = "bi-people" },
            new() { Id = "violations", Label = "Violations", Icon = "bi-exclamation-octagon" },
            new() { Id = "audittrail", Label = "Audit Trail", Icon = "bi-clock-history" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "dutyCode", Label = "Duty Code", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "dutyName", Label = "Duty Name", Type = "text", Width = 200, Editable = true },
            new() { Key = "module", Label = "Module", Type = "text", Width = 100, Editable = true },
            new() { Key = "conflictDuties", Label = "Conflict Duties", Type = "text", Width = 200, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "ACTIVE", Label = "Active", Color = "success" },
                new() { Value = "INACTIVE", Label = "Inactive", Color = "secondary" },
                new() { Value = "VIOLATED", Label = "Violated", Color = "danger" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save SOX compliance duties?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig UNIJRN() => new()
    {
        TCode = "UNIJRN", Title = "Universal Journal Entry", Module = "FI", Icon = "bi-journal-text",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Entry", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "default", Handler = "simulate" },
            new() { Id = "post", Label = "Post", Icon = "bi-send", Style = "success", Handler = "post" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Document Number", Value = "", Key = "documentNumber" },
            new() { Label = "Fiscal Year", Value = "", Key = "fiscalYear" },
            new() { Label = "Period", Value = "", Key = "period", Editable = true },
            new() { Label = "Posting Date", Value = "", Key = "postingDate", Type = "date", Editable = true },
            new() { Label = "Document Type", Value = "", Key = "documentType", Editable = true },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "lineitems", Label = "Line Items", Icon = "bi-list-ol" },
            new() { Id = "costcenter", Label = "Cost Center", Icon = "bi-diagram-3" },
            new() { Id = "profitcenter", Label = "Profit Center", Icon = "bi-graph-up" },
            new() { Id = "accounting", Label = "Accounting", Icon = "bi-calculator" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "lineNum", Label = "Line", Type = "number", Width = 50 },
            new() { Key = "accountCode", Label = "Account Code", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "accountName", Label = "Account Name", Type = "text", Width = 200, Editable = true },
            new() { Key = "debit", Label = "Debit", Type = "currency", Width = 120, Editable = true, Validation = new() { Min = 0 } },
            new() { Key = "credit", Label = "Credit", Type = "currency", Width = 120, Editable = true, Validation = new() { Min = 0 } },
            new() { Key = "costCenter", Label = "Cost Center", Type = "text", Width = 120, Editable = true },
            new() { Key = "profitCenter", Label = "Profit Center", Type = "text", Width = 120, Editable = true },
            new() { Key = "material", Label = "Material", Type = "text", Width = 130, Editable = true },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "simulate", Label = "Simulate", Icon = "bi-play-circle", Style = "default", Handler = "simulate" },
            new() { Id = "post", Label = "Post Journal", Icon = "bi-send", Style = "primary", Handler = "post", Confirm = true, ConfirmMessage = "Post universal journal entry?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig RFSCAN() => new()
    {
        TCode = "RFSCAN", Title = "RF Scanner Menu", Module = "WM", Icon = "bi-upc-scan",
        ToolbarActions = new()
        {
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Session ID", Value = "", Key = "sessionId" },
            new() { Label = "User ID", Value = "", Key = "userId" },
            new() { Label = "Plant", Value = "", Key = "plant" },
            new() { Label = "Warehouse", Value = "", Key = "warehouse" },
            new() { Label = "Current Bin", Value = "", Key = "currentBin" },
            new() { Label = "Device Type", Value = "", Key = "deviceType" },
        },
        Tabs = new()
        {
            new() { Id = "menu", Label = "Menu", Icon = "bi-list-ul", Active = true },
            new() { Id = "scan", Label = "Scan", Icon = "bi-upc-scan" },
            new() { Id = "tasks", Label = "Tasks", Icon = "bi-list-check" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "menuCode", Label = "Menu Code", Type = "text", Width = 120, Required = true },
            new() { Key = "menuName", Label = "Menu Name", Type = "text", Width = 220 },
            new() { Key = "transactionType", Label = "Transaction Type", Type = "text", Width = 160 },
            new() { Key = "requiredPermission", Label = "Required Permission", Type = "text", Width = 160 },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig RFPICK() => new()
    {
        TCode = "RFPICK", Title = "RF Pick Task", Module = "WM", Icon = "bi-box-seam",
        ToolbarActions = new()
        {
            new() { Id = "scan", Label = "Scan", Icon = "bi-upc-scan", Style = "primary", Handler = "scan" },
            new() { Id = "pick", Label = "Pick", Icon = "bi-box-arrow-right", Style = "success", Handler = "pick" },
            new() { Id = "shortPick", Label = "Short Pick", Icon = "bi-dash-circle", Style = "warning", Handler = "shortPick" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Wave Number", Value = "", Key = "waveNumber" },
            new() { Label = "Task ID", Value = "", Key = "taskId" },
            new() { Label = "Material Code", Value = "", Key = "materialCode" },
            new() { Label = "Source Bin", Value = "", Key = "sourceBin" },
            new() { Label = "Destination Bin", Value = "", Key = "destinationBin" },
            new() { Label = "Required Qty", Value = "0", Key = "requiredQty" },
        },
        Tabs = new()
        {
            new() { Id = "task", Label = "Task", Icon = "bi-clipboard-check", Active = true },
            new() { Id = "scan", Label = "Scan", Icon = "bi-upc-scan" },
            new() { Id = "confirm", Label = "Confirm", Icon = "bi-check2-square" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "taskLine", Label = "Task Line", Type = "number", Width = 80 },
            new() { Key = "materialName", Label = "Material Name", Type = "text", Width = 200 },
            new() { Key = "sourceBin", Label = "Source Bin", Type = "text", Width = 120 },
            new() { Key = "requiredQty", Label = "Required Qty", Type = "number", Width = 110 },
            new() { Key = "pickedQty", Label = "Picked Qty", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 0 } },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "OPEN", Label = "Open", Color = "warning" },
                new() { Value = "IN_PROGRESS", Label = "In Progress", Color = "info" },
                new() { Value = "COMPLETED", Label = "Completed", Color = "success" },
                new() { Value = "SHORT", Label = "Short", Color = "danger" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "confirmPick", Label = "Confirm Pick", Icon = "bi-check-lg", Style = "primary", Handler = "confirmPick", Confirm = true, ConfirmMessage = "Confirm pick completion?" },
            new() { Id = "shortPick", Label = "Short Pick", Icon = "bi-dash-circle", Style = "warning", Handler = "shortPick", Confirm = true, ConfirmMessage = "Report short pick?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig WAVEPK() => new()
    {
        TCode = "WAVEPK", Title = "Wave Pick Management", Module = "WM", Icon = "bi-layers",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Wave", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "release", Label = "Release", Icon = "bi-send", Style = "primary", Handler = "release" },
            new() { Id = "assign", Label = "Assign", Icon = "bi-people", Style = "default", Handler = "assign" },
            new() { Id = "optimize", Label = "Optimize", Icon = "bi-speedometer", Style = "default", Handler = "optimize" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Wave Number", Value = "", Key = "waveNumber" },
            new() { Label = "Wave Name", Value = "", Key = "waveName", Editable = true },
            new() { Label = "Wave Type", Value = "", Key = "waveType", Editable = true },
            new() { Label = "Warehouse", Value = "", Key = "warehouse" },
            new() { Label = "Total Lines", Value = "0", Key = "totalLines" },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "lines", Label = "Wave Lines", Icon = "bi-list-ol" },
            new() { Id = "progress", Label = "Progress", Icon = "bi-graph-up" },
            new() { Id = "assignment", Label = "Assignment", Icon = "bi-people" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "lineNum", Label = "Line", Type = "number", Width = 50 },
            new() { Key = "deliveryNumber", Label = "Delivery Number", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "customerName", Label = "Customer Name", Type = "text", Width = 180 },
            new() { Key = "materialName", Label = "Material Name", Type = "text", Width = 180 },
            new() { Key = "sourceBin", Label = "Source Bin", Type = "text", Width = 110 },
            new() { Key = "requiredQty", Label = "Required Qty", Type = "number", Width = 100 },
            new() { Key = "pickedQty", Label = "Picked Qty", Type = "number", Width = 100 },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "OPEN", Label = "Open", Color = "warning" },
                new() { Value = "IN_PROGRESS", Label = "In Progress", Color = "info" },
                new() { Value = "COMPLETED", Label = "Completed", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "release", Label = "Release Wave", Icon = "bi-send", Style = "primary", Handler = "release", Confirm = true, ConfirmMessage = "Release this wave for picking?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig VSLOTT() => new()
    {
        TCode = "VSLOTT", Title = "Velocity Slotting", Module = "WM", Icon = "bi-speedometer",
        ToolbarActions = new()
        {
            new() { Id = "calculate", Label = "Calculate", Icon = "bi-calculator", Style = "primary", Handler = "calculate" },
            new() { Id = "apply", Label = "Apply", Icon = "bi-check-lg", Style = "success", Handler = "apply" },
            new() { Id = "batchApply", Label = "Batch Apply", Icon = "bi-layers", Style = "default", Handler = "batchApply" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Plant", Value = "", Key = "plant" },
            new() { Label = "Warehouse", Value = "", Key = "warehouse" },
            new() { Label = "Total Materials", Value = "0", Key = "totalMaterials" },
            new() { Label = "Class A", Value = "0", Key = "classA" },
            new() { Label = "Class B", Value = "0", Key = "classB" },
            new() { Label = "Class C", Value = "0", Key = "classC" },
            new() { Label = "Class D", Value = "0", Key = "classD" },
        },
        Tabs = new()
        {
            new() { Id = "velocity", Label = "Velocity Classes", Icon = "bi-speedometer2", Active = true },
            new() { Id = "recommendations", Label = "Recommendations", Icon = "bi-lightbulb" },
            new() { Id = "bins", Label = "Bin Assignment", Icon = "bi-grid-3x3-gap" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "materialCode", Label = "Material Code", Type = "text", Width = 130, Required = true },
            new() { Key = "materialName", Label = "Material Name", Type = "text", Width = 200 },
            new() { Key = "velocityClass", Label = "Velocity Class", Type = "dropdown", Width = 130, Editable = true, Options = new() {
                new() { Value = "A", Label = "Class A - Fast" },
                new() { Value = "B", Label = "Class B - Medium" },
                new() { Value = "C", Label = "Class C - Slow" },
                new() { Value = "D", Label = "Class D - Dead" },
            }},
            new() { Key = "consumption30", Label = "30-Day Consumption", Type = "number", Width = 150 },
            new() { Key = "currentBin", Label = "Current Bin", Type = "text", Width = 110 },
            new() { Key = "recommendedBin", Label = "Recommended Bin", Type = "text", Width = 140, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "CALCULATED", Label = "Calculated", Color = "info" },
                new() { Value = "APPLIED", Label = "Applied", Color = "success" },
                new() { Value = "PENDING", Label = "Pending", Color = "warning" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "apply", Label = "Apply Slotting", Icon = "bi-check-lg", Style = "primary", Handler = "apply", Confirm = true, ConfirmMessage = "Apply velocity slotting recommendations?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig PPDS() => new()
    {
        TCode = "PPDS", Title = "PP/DS Finite Scheduling", Module = "PP", Icon = "bi-calendar-range",
        ToolbarActions = new()
        {
            new() { Id = "create", Label = "Create Schedule", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "calculate", Label = "Calculate", Icon = "bi-calculator", Style = "primary", Handler = "calculate" },
            new() { Id = "optimize", Label = "Optimize", Icon = "bi-speedometer", Style = "default", Handler = "optimize" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Schedule ID", Value = "", Key = "scheduleId" },
            new() { Label = "Schedule Name", Value = "", Key = "scheduleName", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Horizon Start", Value = "", Key = "horizonStart", Type = "date", Editable = true },
            new() { Label = "Horizon End", Value = "", Key = "horizonEnd", Type = "date", Editable = true },
            new() { Label = "Strategy", Value = "", Key = "strategy", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "operations", Label = "Operations", Icon = "bi-diagram-3" },
            new() { Id = "capacity", Label = "Capacity", Icon = "bi-bar-chart-steps" },
            new() { Id = "gantt", Label = "Gantt Chart", Icon = "bi-calendar-range" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "productionOrder", Label = "Production Order", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "materialName", Label = "Material Name", Type = "text", Width = 180 },
            new() { Key = "workCenter", Label = "Work Center", Type = "text", Width = 120, Editable = true },
            new() { Key = "plannedStart", Label = "Planned Start", Type = "date", Width = 130, Editable = true },
            new() { Key = "plannedEnd", Label = "Planned End", Type = "date", Width = 130, Editable = true },
            new() { Key = "duration", Label = "Duration (hrs)", Type = "number", Width = 120 },
            new() { Key = "capacityLoad", Label = "Capacity Load %", Type = "number", Width = 120 },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "PLANNED", Label = "Planned", Color = "info" },
                new() { Value = "OPTIMIZED", Label = "Optimized", Color = "success" },
                new() { Value = "OVERLOADED", Label = "Overloaded", Color = "danger" },
                new() { Value = "CONFIRMED", Label = "Confirmed", Color = "secondary" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "calculate", Label = "Calculate Schedule", Icon = "bi-calculator", Style = "primary", Handler = "calculate", Confirm = true, ConfirmMessage = "Recalculate finite schedule?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig MRPEVT() => new()
    {
        TCode = "MRPEVT", Title = "MRP Event Monitor", Module = "MM", Icon = "bi-lightning",
        ToolbarActions = new()
        {
            new() { Id = "publish", Label = "Publish Event", Icon = "bi-send", Style = "primary", Handler = "publish" },
            new() { Id = "runMrp", Label = "Run MRP", Icon = "bi-play-circle", Style = "success", Handler = "runMrp" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Event Type", Value = "", Key = "eventType" },
            new() { Label = "Material Code", Value = "", Key = "materialCode" },
            new() { Label = "Plant", Value = "", Key = "plant" },
            new() { Label = "Status", Value = "", Key = "status" },
            new() { Label = "Pending Events", Value = "0", Key = "pendingEvents" },
            new() { Label = "Processed Events", Value = "0", Key = "processedEvents" },
        },
        Tabs = new()
        {
            new() { Id = "events", Label = "Events", Icon = "bi-lightning", Active = true },
            new() { Id = "stream", Label = "Event Stream", Icon = "bi-activity" },
            new() { Id = "runs", Label = "MRP Runs", Icon = "bi-play-circle" },
            new() { Id = "subscriptions", Label = "Subscriptions", Icon = "bi-bell" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "eventType", Label = "Event Type", Type = "text", Width = 140, Required = true },
            new() { Key = "materialCode", Label = "Material Code", Type = "text", Width = 130 },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80 },
            new() { Key = "priority", Label = "Priority", Type = "dropdown", Width = 100, Editable = true, Options = new() {
                new() { Value = "HIGH", Label = "High" },
                new() { Value = "MEDIUM", Label = "Medium" },
                new() { Value = "LOW", Label = "Low" },
            }},
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "PENDING", Label = "Pending", Color = "warning" },
                new() { Value = "PROCESSED", Label = "Processed", Color = "success" },
                new() { Value = "FAILED", Label = "Failed", Color = "danger" },
            }},
            new() { Key = "processedAt", Label = "Processed At", Type = "date", Width = 130 },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "runMrp", Label = "Run MRP Now", Icon = "bi-play-circle", Style = "primary", Handler = "runMrp", Confirm = true, ConfirmMessage = "Execute MRP with pending events?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig CONSOL() => new()
    {
        TCode = "CONSOL", Title = "Consolidation Workbench", Module = "FI", Icon = "bi-diagram-3",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Group", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "addEntity", Label = "Add Entity", Icon = "bi-building", Style = "default", Handler = "addEntity" },
            new() { Id = "translate", Label = "Translate", Icon = "bi-translate", Style = "default", Handler = "translate" },
            new() { Id = "eliminate", Label = "Eliminate", Icon = "bi-x-octagon", Style = "default", Handler = "eliminate" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Group Code", Value = "", Key = "groupCode" },
            new() { Label = "Group Name", Value = "", Key = "groupName", Editable = true },
            new() { Label = "Fiscal Year", Value = "", Key = "fiscalYear", Editable = true },
            new() { Label = "Consolidation Currency", Value = "", Key = "consolidationCurrency", Editable = true },
            new() { Label = "Status", Value = "NEW", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "group", Label = "Group", Icon = "bi-diagram-3", Active = true },
            new() { Id = "entities", Label = "Entities", Icon = "bi-building" },
            new() { Id = "eliminations", Label = "Eliminations", Icon = "bi-x-octagon" },
            new() { Id = "translation", Label = "Translation", Icon = "bi-translate" },
            new() { Id = "report", Label = "Report", Icon = "bi-file-earmark-bar-graph" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "entityCode", Label = "Entity Code", Type = "text", Width = 120, Required = true, Editable = true },
            new() { Key = "entityName", Label = "Entity Name", Type = "text", Width = 200, Editable = true },
            new() { Key = "currency", Label = "Currency", Type = "text", Width = 80, Editable = true },
            new() { Key = "ownership", Label = "Ownership %", Type = "number", Width = 100, Editable = true, Validation = new() { Min = 0, Max = 100 } },
            new() { Key = "revenue", Label = "Revenue", Type = "currency", Width = 130 },
            new() { Key = "cost", Label = "Cost", Type = "currency", Width = 130 },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "DRAFT", Label = "Draft", Color = "secondary" },
                new() { Value = "TRANSLATED", Label = "Translated", Color = "info" },
                new() { Value = "ELIMINATED", Label = "Eliminated", Color = "success" },
                new() { Value = "CONSOLIDATED", Label = "Consolidated", Color = "success" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "translate", Label = "Translate Currencies", Icon = "bi-translate", Style = "primary", Handler = "translate", Confirm = true, ConfirmMessage = "Translate entity currencies?" },
            new() { Id = "eliminate", Label = "Eliminate Intercompany", Icon = "bi-x-octagon", Style = "default", Handler = "eliminate", Confirm = true, ConfirmMessage = "Eliminate intercompany balances?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig TAXRET() => new()
    {
        TCode = "TAXRET", Title = "Tax Return Filing", Module = "FI", Icon = "bi-file-earmark-text",
        ToolbarActions = new()
        {
            new() { Id = "new", Label = "New Return", Icon = "bi-plus-circle", Style = "success", Handler = "addRow" },
            new() { Id = "calculate", Label = "Calculate", Icon = "bi-calculator", Style = "primary", Handler = "calculate" },
            new() { Id = "validate", Label = "Validate", Icon = "bi-check-circle", Style = "default", Handler = "validate" },
            new() { Id = "file", Label = "File Return", Icon = "bi-send", Style = "success", Handler = "file" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Country Code", Value = "", Key = "countryCode", Editable = true },
            new() { Label = "Tax Type", Value = "", Key = "taxType", Editable = true },
            new() { Label = "Period", Value = "", Key = "period", Editable = true },
            new() { Label = "Return Number", Value = "", Key = "returnNumber" },
            new() { Label = "Total Output Tax", Value = "0", Key = "totalOutputTax" },
            new() { Label = "Total Input Tax", Value = "0", Key = "totalInputTax" },
            new() { Label = "Net Tax Payable", Value = "0", Key = "netTaxPayable" },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "transactions", Label = "Transactions", Icon = "bi-list-ol" },
            new() { Id = "filing", Label = "Filing", Icon = "bi-file-earmark-check" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "countryCode", Label = "Country", Type = "text", Width = 80, Required = true, Editable = true },
            new() { Key = "taxType", Label = "Tax Type", Type = "text", Width = 100, Editable = true },
            new() { Key = "period", Label = "Period", Type = "text", Width = 100, Editable = true },
            new() { Key = "taxableSales", Label = "Taxable Sales", Type = "currency", Width = 130 },
            new() { Key = "outputTax", Label = "Output Tax", Type = "currency", Width = 110 },
            new() { Key = "inputTax", Label = "Input Tax", Type = "currency", Width = 110 },
            new() { Key = "netPayable", Label = "Net Payable", Type = "currency", Width = 120 },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "DRAFT", Label = "Draft", Color = "secondary" },
                new() { Value = "CALCULATED", Label = "Calculated", Color = "info" },
                new() { Value = "FILED", Label = "Filed", Color = "success" },
                new() { Value = "REJECTED", Label = "Rejected", Color = "danger" },
            }},
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "calculate", Label = "Calculate Tax", Icon = "bi-calculator", Style = "primary", Handler = "calculate", Confirm = true, ConfirmMessage = "Calculate tax return amounts?" },
            new() { Id = "file", Label = "File Return", Icon = "bi-send", Style = "success", Handler = "file", Confirm = true, ConfirmMessage = "File this tax return?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig AIOCR() => new()
    {
        TCode = "AIOCR", Title = "Document OCR Processing", Module = "AI", Icon = "bi-eye",
        ToolbarActions = new()
        {
            new() { Id = "upload", Label = "Upload", Icon = "bi-cloud-upload", Style = "primary", Handler = "upload" },
            new() { Id = "process", Label = "Process", Icon = "bi-cpu", Style = "success", Handler = "process" },
            new() { Id = "validate", Label = "Validate", Icon = "bi-check-circle", Style = "default", Handler = "validate" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Document Type", Value = "", Key = "documentType" },
            new() { Label = "File Name", Value = "", Key = "fileName" },
            new() { Label = "File Size", Value = "", Key = "fileSize" },
            new() { Label = "Status", Value = "", Key = "status" },
            new() { Label = "Confidence Score", Value = "0", Key = "confidenceScore" },
            new() { Label = "OCR Provider", Value = "", Key = "ocrProvider" },
        },
        Tabs = new()
        {
            new() { Id = "upload", Label = "Upload", Icon = "bi-cloud-upload", Active = true },
            new() { Id = "extracted", Label = "Extracted Data", Icon = "bi-file-earmark-text" },
            new() { Id = "mapping", Label = "Field Mapping", Icon = "bi-arrow-left-right" },
            new() { Id = "review", Label = "Review", Icon = "bi-eye" },
        },
        Columns = new()
        {
            new() { Key = "select", Label = "", Type = "checkbox", Width = 40, Fixed = true },
            new() { Key = "documentType", Label = "Document Type", Type = "text", Width = 140, Required = true, Editable = true },
            new() { Key = "fileName", Label = "File Name", Type = "text", Width = 220 },
            new() { Key = "confidenceScore", Label = "Confidence %", Type = "number", Width = 110 },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 100, Options = new() {
                new() { Value = "UPLOADED", Label = "Uploaded", Color = "info" },
                new() { Value = "PROCESSING", Label = "Processing", Color = "warning" },
                new() { Value = "EXTRACTED", Label = "Extracted", Color = "success" },
                new() { Value = "REVIEWED", Label = "Reviewed", Color = "success" },
                new() { Value = "FAILED", Label = "Failed", Color = "danger" },
            }},
            new() { Key = "extractedAt", Label = "Extracted At", Type = "date", Width = 130 },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "process", Label = "Process OCR", Icon = "bi-cpu", Style = "primary", Handler = "process", Confirm = true, ConfirmMessage = "Process document with OCR?" },
            new() { Id = "validate", Label = "Validate Data", Icon = "bi-check-circle", Style = "success", Handler = "validate" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
    };
}
