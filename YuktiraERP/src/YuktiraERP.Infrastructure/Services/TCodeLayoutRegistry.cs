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
    }

    private static TCodeLayoutConfig QE51N() => new()
    {
        TCode = "QE51N", Title = "Record Results: Characteristic Overview", Module = "QM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
            new() { Id = "refresh", Label = "Refresh", Icon = "bi-arrow-clockwise", Style = "default", Handler = "refresh" },
            new() { Id = "print", Label = "Print", Icon = "bi-printer", Style = "default", Handler = "print" },
        },
        Metadata = new()
        {
            new() { Label = "Material", Value = "", Key = "material" },
            new() { Label = "Batch", Value = "", Key = "batch" },
            new() { Label = "Insp. Lot", Value = "", Key = "inspectionLot" },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Operation", Value = "", Key = "operation" },
            new() { Label = "Work Center", Value = "", Key = "workCenter" },
            new() { Label = "Status", Value = "", Key = "status" },
            new() { Label = "Created By", Value = "", Key = "createdBy" },
        },
        Tabs = new()
        {
            new() { Id = "general", Label = "General", Icon = "bi-list-ul", Active = true },
            new() { Id = "summarized", Label = "Summarized", Icon = "bi-bar-chart" },
            new() { Id = "indicators", Label = "Indicators", Icon = "bi-speedometer" },
        },
        Columns = new()
        {
            new() { Key = "shortText", Label = "Short Text for Characteristic", Type = "text", Width = 260, Required = true, Editable = true },
            new() { Key = "spec", Label = "Specifications", Type = "text", Width = 180, Editable = true },
            new() { Key = "result", Label = "Result", Type = "number", Editable = true, Width = 120, Validation = new() { Min = 0, Max = 99999, Required = true } },
            new() { Key = "valuation", Label = "Valuation", Type = "status_icon", Width = 100, Options = new() {
                new() { Value = "OK", Label = "OK", Color = "success" },
                new() { Value = "NOK", Label = "Not OK", Color = "danger" },
                new() { Value = "REVIEW", Label = "Review", Color = "warning" },
            }},
            new() { Key = "defectClass", Label = "Defect Class", Type = "dropdown", Width = 160, Options = new() {
                new() { Value = "MAJOR", Label = "Major defect" },
                new() { Value = "MINOR", Label = "Minor defect" },
                new() { Value = "VALID", Label = "Valid" },
            }},
            new() { Key = "measuredValue", Label = "Measured Value", Type = "number", Editable = true, Width = 130 },
            new() { Key = "targetValue", Label = "Target Value", Type = "number", Width = 120 },
            new() { Key = "tolerance", Label = "Tolerance", Type = "text", Width = 100, Editable = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save inspection results?" },
            new() { Id = "post", Label = "Post Results", Icon = "bi-send", Style = "success", Handler = "post" },
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
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Notification Type", Value = "Q1", Key = "notificationType", Editable = true },
            new() { Label = "Reference Doc (PO)", Value = "", Key = "referenceDocument", Editable = true },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Partner (Coordinator)", Value = "", Key = "partnerName", Editable = true },
            new() { Label = "Subject Coding", Value = "", Key = "subjectCoding", Editable = true },
        },
        Tabs = new()
        {
            new() { Id = "header", Label = "Header", Icon = "bi-card-heading", Active = true },
            new() { Id = "text", Label = "Description / Long Text", Icon = "bi-file-text" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 220 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 400, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Create Notification", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Create this quality notification?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = false, ShowAddRow = false, ShowDeleteRow = false }
    };

    private static TCodeLayoutConfig QM02() => new()
    {
        TCode = "QM02", Title = "Change Quality Notification", Module = "QM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
        },
        Metadata = new()
        {
            new() { Label = "Notification No.", Value = "", Key = "notificationNumber", Editable = true },
            new() { Label = "Description", Value = "", Key = "description" },
            new() { Label = "Plant", Value = "", Key = "plant", Editable = true },
            new() { Label = "Status", Value = "", Key = "status" },
        },
        Tabs = new()
        {
            new() { Id = "defect", Label = "Defect Assignment", Icon = "bi-exclamation-triangle", Active = true },
            new() { Id = "tasks", Label = "Tasks", Icon = "bi-list-check" },
        },
        Columns = new()
        {
            new() { Key = "field", Label = "Field", Type = "text", Width = 220 },
            new() { Key = "value", Label = "Value", Type = "text", Width = 400, Editable = true },
            new() { Key = "mandatory", Label = "", Type = "mandatory_icon", Width = 30, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "save", Label = "Save Changes", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Save notification changes?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = false, ShowFilter = false, ShowExport = false, ShowAddRow = true, ShowDeleteRow = true }
    };

    private static TCodeLayoutConfig QM03() => new()
    {
        TCode = "QM03", Title = "Quality Notification Tasks", Module = "QM", Icon = "bi-clipboard-check",
        ToolbarActions = new()
        {
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
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
            new() { Key = "taskNumber", Label = "Task No.", Type = "text", Width = 90 },
            new() { Key = "description", Label = "Description", Type = "text", Width = 280, Editable = true, Required = true },
            new() { Key = "userResponsible", Label = "User Responsible", Type = "text", Width = 160, Editable = true },
            new() { Key = "completionText", Label = "Completion Text", Type = "text", Width = 250, Editable = true },
            new() { Key = "status", Label = "Status", Type = "status_badge", Width = 110, Options = new() {
                new() { Value = "OPEN", Label = "Open", Color = "warning" },
                new() { Value = "COMPLETED", Label = "Completed", Color = "success" },
                new() { Value = "IN_PROCESS", Label = "In Process", Color = "info" },
            }},
            new() { Key = "completedAt", Label = "Completed At", Type = "date", Width = 130 },
            new() { Key = "validation", Label = "", Type = "validation_icon", Width = 40, Fixed = true },
        },
        FooterActions = new()
        {
            new() { Id = "complete", Label = "Complete Selected Tasks", Icon = "bi-check2-all", Style = "success", Handler = "completeTasks", Confirm = true, ConfirmMessage = "Mark selected tasks as completed?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new()
        {
            ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = true, ShowDeleteRow = true,
            CustomActions = new()
            {
                new() { Id = "complete", Label = "Complete", Icon = "bi-check2-all", Style = "success", Handler = "completeTasks" },
            }
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
            new() { Id = "save", Label = "Save", Icon = "bi-check-lg", Style = "primary", Handler = "save" },
            new() { Id = "back", Label = "Back", Icon = "bi-arrow-left", Style = "secondary", Handler = "back" },
            new() { Id = "confirm", Label = "Confirm Certificate", Icon = "bi-patch-check", Style = "success", Handler = "confirmCertificate" },
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
            new() { Key = "lotNumber", Label = "Inspection Lot", Type = "text", Width = 140 },
            new() { Key = "plant", Label = "Plant", Type = "text", Width = 80 },
            new() { Key = "origin", Label = "Origin", Type = "text", Width = 110 },
            new() { Key = "resultStatus", Label = "Result Status", Type = "status_badge", Width = 120, Options = new() {
                new() { Value = "RECORDED", Label = "Recorded", Color = "info" },
                new() { Value = "CERT_CONFIRMED", Label = "Cert. Confirmed", Color = "success" },
                new() { Value = "UD_RECORDED", Label = "UD Recorded", Color = "success" },
            }},
            new() { Key = "udCode", Label = "UD Code", Type = "dropdown", Width = 150, Editable = true, Options = new() {
                new() { Value = "A", Label = "A - Accept" },
                new() { Value = "R", Label = "R - Reject" },
                new() { Value = "R1", Label = "R1 - Rework" },
                new() { Value = "N", Label = "N - Return to Vendor" },
                new() { Value = "S", Label = "S - Scrap" },
            }},
            new() { Key = "stockProposal", Label = "Stock Proposal", Type = "dropdown", Width = 150, Editable = true, Options = new() {
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
            new() { Id = "confirm", Label = "Confirm Certificate", Icon = "bi-patch-check", Style = "success", Handler = "confirmCertificate", Confirm = true, ConfirmMessage = "Confirm certificate receipt?" },
            new() { Id = "save", Label = "Record Usage Decision", Icon = "bi-check-lg", Style = "primary", Handler = "save", Confirm = true, ConfirmMessage = "Record usage decision and post stock?" },
            new() { Id = "cancel", Label = "Cancel", Icon = "bi-x-lg", Style = "outline", Handler = "back" },
        },
        TableToolbar = new() { ShowSearch = true, ShowFilter = true, ShowExport = true, ShowAddRow = false, ShowDeleteRow = false }
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
}
