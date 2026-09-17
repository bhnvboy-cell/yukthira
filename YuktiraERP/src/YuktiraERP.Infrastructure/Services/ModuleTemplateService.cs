using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ModuleTemplateService : IModuleTemplateService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<ModuleTemplateService> _logger;

    private static readonly HashSet<string> ExcludedProperties = new()
    {
        "Id", "CreatedAt", "UpdatedAt", "TenantId"
    };

    private static readonly Dictionary<DataSyncModule, (Type EntityType, string DisplayName, string Category, string[] BusinessKeys)> ModuleMap = new()
    {
        [DataSyncModule.MM_MaterialMaster] = (typeof(MaterialMasterEntity), "Material Master", "MM", new[] { "Code" }),
        [DataSyncModule.MM_Vendor] = (typeof(VendorEntity), "Vendor Master", "MM", new[] { "Code" }),
        [DataSyncModule.SD_Customer] = (typeof(CustomerEntity), "Customer Master", "SD", new[] { "Code" }),
        [DataSyncModule.SD_SalesOrder] = (typeof(SalesOrderEntity), "Sales Order", "SD", new[] { "OrderNumber" }),
        [DataSyncModule.SD_Inquiry] = (typeof(InquiryEntity), "Inquiry", "SD", new[] { "InquiryNumber" }),
        [DataSyncModule.SD_Quotation] = (typeof(QuotationEntity), "Quotation", "SD", new[] { "QuotationNumber" }),
        [DataSyncModule.SD_Delivery] = (typeof(DeliveryEntity), "Delivery", "SD", new[] { "DeliveryNumber" }),
        [DataSyncModule.SD_BillingDocument] = (typeof(BillingDocumentEntity), "Billing Document", "SD", new[] { "DocumentNumber" }),
        [DataSyncModule.FI_GeneralLedger] = (typeof(GeneralLedgerEntryEntity), "General Ledger Entry", "FI", new[] { "AccountNumber" }),
        [DataSyncModule.FI_APEntry] = (typeof(APEntryEntity), "AP Entry", "FI", new[] { "InvoiceNumber" }),
        [DataSyncModule.FI_AREntry] = (typeof(AREntryEntity), "AR Entry", "FI", new[] { "InvoiceNumber" }),
        [DataSyncModule.FI_FixedAsset] = (typeof(FixedAssetEntity), "Fixed Asset", "FI", new[] { "AssetNumber" }),
        [DataSyncModule.FI_Account] = (typeof(AccountEntity), "GL Account", "FI", new[] { "AccountNumber" }),
        [DataSyncModule.FI_TaxCode] = (typeof(TaxCodeEntity), "Tax Code", "FI", new[] { "Code" }),
        [DataSyncModule.FI_Currency] = (typeof(CurrencyEntity), "Currency", "FI", new[] { "Code" }),
        [DataSyncModule.FI_ExchangeRate] = (typeof(ExchangeRateEntity), "Exchange Rate", "FI", new[] { "FromCurrency", "ToCurrency" }),
        [DataSyncModule.CO_CostCenter] = (typeof(CostCenterEntity), "Cost Center", "CO", new[] { "Code" }),
        [DataSyncModule.CO_CostElement] = (typeof(CostElementEntity), "Cost Element", "CO", new[] { "Code" }),
        [DataSyncModule.CO_ProfitCenter] = (typeof(ProfitCenterEntity), "Profit Center", "CO", new[] { "Code" }),
        [DataSyncModule.CO_InternalOrder] = (typeof(InternalOrderEntity), "Internal Order", "CO", new[] { "OrderNumber" }),
        [DataSyncModule.PP_ProductionPlan] = (typeof(ProductionPlanEntity), "Production Plan", "PP", new[] { "PlanNumber" }),
        [DataSyncModule.PP_BillOfMaterial] = (typeof(BillOfMaterialEntity), "Bill of Material", "PP", new[] { "BomNumber" }),
        [DataSyncModule.PP_WorkCenter] = (typeof(WorkCenterEntity), "Work Center", "PP", new[] { "Code" }),
        [DataSyncModule.PP_ProductionRouting] = (typeof(ProductionRoutingEntity), "Production Routing", "PP", new[] { "RoutingNumber" }),
        [DataSyncModule.PP_ProductionOrder] = (typeof(ProductionOrderEntity), "Production Order", "PP", new[] { "OrderNumber" }),
        [DataSyncModule.QM_InspectionPlan] = (typeof(InspectionPlanEntity), "Inspection Plan", "QM", new[] { "PlanNumber" }),
        [DataSyncModule.QM_InspectionLot] = (typeof(InspectionLotEntity), "Inspection Lot", "QM", new[] { "LotNumber" }),
        [DataSyncModule.QM_QualityNotification] = (typeof(QualityNotificationEntity), "Quality Notification", "QM", new[] { "NotificationNumber" }),
        [DataSyncModule.WM_StorageLocation] = (typeof(StorageLocationEntity), "Storage Location", "WM", new[] { "Code" }),
        [DataSyncModule.WM_Bin] = (typeof(BinEntity), "Bin Location", "WM", new[] { "BinCode" }),
        [DataSyncModule.WM_WarehouseTransfer] = (typeof(WarehouseTransferEntity), "Warehouse Transfer", "WM", new[] { "TransferNumber" }),
        [DataSyncModule.HR_Employee] = (typeof(EmployeeEntity), "Employee", "HR", new[] { "EmployeeNumber" }),
        [DataSyncModule.HR_OrgUnit] = (typeof(OrgUnitEntity), "Org Unit", "HR", new[] { "Code" }),
        [DataSyncModule.HR_LeaveRequest] = (typeof(LeaveRequestEntity), "Leave Request", "HR", new[] { "RequestNumber" }),
        [DataSyncModule.HR_PayrollEntry] = (typeof(PayrollEntryEntity), "Payroll Entry", "HR", new[] { "PayrollNumber" }),
        [DataSyncModule.HR_Attendance] = (typeof(AttendanceEntity), "Attendance", "HR", new[] { "AttendanceNumber" }),
        [DataSyncModule.LIMS_Sample] = (typeof(SampleEntity), "LIMS Sample", "LIMS", new[] { "SampleNumber" }),
        [DataSyncModule.LIMS_Specification] = (typeof(SpecificationEntity), "Specification", "LIMS", new[] { "SpecificationNumber" }),
        [DataSyncModule.LIMS_Instrument] = (typeof(InstrumentEntity), "Instrument", "LIMS", new[] { "InstrumentNumber" }),
        [DataSyncModule.PM_Equipment] = (typeof(EquipmentEntity), "Equipment", "PM", new[] { "EquipmentNumber" }),
        [DataSyncModule.PM_MaintenancePlan] = (typeof(MaintenancePlanEntity), "Maintenance Plan", "PM", new[] { "PlanNumber" }),
        [DataSyncModule.PM_MaintenanceOrder] = (typeof(MaintenanceOrderEntity), "Maintenance Order", "PM", new[] { "OrderNumber" }),
        [DataSyncModule.CRM_Lead] = (typeof(LeadEntity), "Lead", "CRM", new[] { "LeadNumber" }),
        [DataSyncModule.CRM_Opportunity] = (typeof(OpportunityEntity), "Opportunity", "CRM", new[] { "OpportunityNumber" }),
        [DataSyncModule.CRM_Contact] = (typeof(ContactEntity), "Contact", "CRM", new[] { "ContactNumber" }),
        [DataSyncModule.CRM_Campaign] = (typeof(CampaignEntity), "Campaign", "CRM", new[] { "CampaignNumber" }),
        [DataSyncModule.PS_Project] = (typeof(ProjectEntity), "Project", "PS", new[] { "ProjectNumber" }),
        [DataSyncModule.PS_ProjectTask] = (typeof(ProjectTaskEntity), "Project Task", "PS", new[] { "TaskNumber" }),
        [DataSyncModule.BI_Report] = (typeof(BIReportEntity), "BI Report", "BI", new[] { "ReportCode" }),
        [DataSyncModule.Admin_Tenant] = (typeof(TenantEntity), "Tenant", "Admin", new[] { "Code" }),
        [DataSyncModule.Admin_User] = (typeof(AdminUserEntity), "Admin User", "Admin", new[] { "Username" }),
    };

    private static readonly HashSet<string> DropdownColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Plant", "Status", "Type", "UOM", "UnitOfMeasure", "Currency", "Priority",
        "Category", "ValuationClass", "StorageLocation", "Department", "PaymentTerms",
        "OrderType", "BaseUOM", "BOMUsage", "ItemCategory", "ControlKey",
        "WorkCenterCategory", "MRPController"
    };

    public ModuleTemplateService(YuktiraDbContext db, ILogger<ModuleTemplateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<byte[]> GenerateEnhancedTemplateAsync(DataSyncModule module, Guid tenantId)
    {
        if (!ModuleMap.TryGetValue(module, out var mapping))
            throw new ArgumentException($"Unknown module: {module}");

        var entityType = mapping.EntityType;
        var properties = GetEntityProperties(entityType);
        var businessKeys = mapping.BusinessKeys.ToHashSet();
        var requiredProps = properties.Where(p => IsRequiredProperty(p)).Select(p => p.Name).ToHashSet();
        var dataProperties = properties.Where(p => !ExcludedProperties.Contains(p.Name)).ToList();

        using var workbook = new XLWorkbook();

        // ── Instructions Sheet ──
        var instrWs = workbook.Worksheets.Add("Instructions");

        instrWs.Cell(1, 1).Value = $"YuktiraERP Enhanced Template — {mapping.DisplayName}";
        instrWs.Cell(1, 1).Style.Font.Bold = true;
        instrWs.Cell(1, 1).Style.Font.FontSize = 18;
        instrWs.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1F497D");

        instrWs.Cell(2, 1).Value = $"Category: {mapping.Category}  |  Entity: {entityType.Name}  |  Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        instrWs.Cell(2, 1).Style.Font.FontSize = 10;
        instrWs.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        instrWs.Cell(4, 1).Value = "FORMAT GUIDELINES";
        instrWs.Cell(4, 1).Style.Font.Bold = true;
        instrWs.Cell(4, 1).Style.Font.FontSize = 14;
        instrWs.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml("#1F497D");

        var guidelines = new[]
        {
            "1. Do NOT modify the header row (Row 1) — column names must match exactly.",
            "2. All dates must be in YYYY-MM-DD format (e.g. 2025-01-15).",
            "3. All decimals must use dot as separator (e.g. 1234.56), no thousand separators.",
            "4. All GUIDs must be valid format (00000000-0000-0000-0000-000000000000).",
            "5. Required columns are shown with RED text in the header row.",
            "6. Dropdown columns restrict input to allowed values — use the dropdown list.",
            "7. Leave cells empty to accept the system default value.",
            "8. Each row must have a unique business key — duplicates cause validation errors.",
            "9. String columns have maximum lengths — see the Schema sheet for limits.",
            "10. Tenant ID is auto-stamped. Do NOT include a TenantId column.",
            $"11. Tenant scope: {tenantId}",
        };

        for (int i = 0; i < guidelines.Length; i++)
        {
            instrWs.Cell(6 + i, 1).Value = guidelines[i];
            instrWs.Cell(6 + i, 1).Style.Font.FontSize = 11;
        }

        instrWs.Column(1).Width = 110;

        // ── Schema Sheet ──
        var schemaWs = workbook.Worksheets.Add("Schema");
        var schemaHeaders = new[]
        {
            "Column Name", "Display Name", "Data Type", "Required", "Business Key",
            "Max Length", "Default", "Allowed Values", "Lookup Source", "IsNullable"
        };

        for (int c = 0; c < schemaHeaders.Length; c++)
        {
            var cell = schemaWs.Cell(1, c + 1);
            cell.Value = schemaHeaders[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F497D");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int schemaRow = 2;
        foreach (var prop in properties)
        {
            var isRequired = requiredProps.Contains(prop.Name);
            var isBK = businessKeys.Contains(prop.Name);
            var maxLen = GetMaxLength(prop);
            var enumVals = GetEnumValues(prop.PropertyType);
            var lookupSrc = GetLookupSource(prop);
            var isNullable = Nullable.GetUnderlyingType(prop.PropertyType) != null
                             || prop.PropertyType == typeof(string);

            schemaWs.Cell(schemaRow, 1).Value = prop.Name;
            schemaWs.Cell(schemaRow, 2).Value = FormatDisplayName(prop.Name);
            schemaWs.Cell(schemaRow, 3).Value = GetDataTypeName(prop.PropertyType);
            schemaWs.Cell(schemaRow, 4).Value = isRequired ? "YES" : "no";
            schemaWs.Cell(schemaRow, 5).Value = isBK ? "KEY" : "";
            schemaWs.Cell(schemaRow, 6).Value = maxLen?.ToString() ?? "";
            schemaWs.Cell(schemaRow, 7).Value = GetDefaultDisplayValue(prop) ?? "";
            schemaWs.Cell(schemaRow, 8).Value = enumVals.Length > 0 ? string.Join(", ", enumVals) : "";
            schemaWs.Cell(schemaRow, 9).Value = lookupSrc ?? "";
            schemaWs.Cell(schemaRow, 10).Value = isNullable ? "Yes" : "No";

            if (isRequired)
            {
                schemaWs.Cell(schemaRow, 4).Style.Font.FontColor = XLColor.Red;
                schemaWs.Cell(schemaRow, 4).Style.Font.Bold = true;
            }

            schemaRow++;
        }

        schemaWs.Columns().AdjustToContents();

        // ── Data Sheet ──
        var dataWs = workbook.Worksheets.Add("Data");

        // Header row
        for (int c = 0; c < dataProperties.Count; c++)
        {
            var prop = dataProperties[c];
            var cell = dataWs.Cell(1, c + 1);
            cell.Value = prop.Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F497D");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.PatternType = XLFillPatternValues.Solid;

            if (requiredProps.Contains(prop.Name))
            {
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.FromHtml("#FF4444");
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F497D");
            }
        }

        // Instruction row (row 2) — read-only guidance
        for (int c = 0; c < dataProperties.Count; c++)
        {
            var prop = dataProperties[c];
            var instruction = BuildColumnInstruction(prop, requiredProps.Contains(prop.Name), businessKeys.Contains(prop.Name));
            var cell = dataWs.Cell(2, c + 1);
            cell.Value = instruction;
            cell.Style.Font.Italic = true;
            cell.Style.Font.FontColor = XLColor.Gray;
            cell.Style.Font.FontSize = 9;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        }

        // Auto-inject DataValidation dropdowns
        for (int c = 0; c < dataProperties.Count; c++)
        {
            var prop = dataProperties[c];
            var allowedValues = GetAllowedValuesForDropdown(prop);
            if (allowedValues.Count > 0 && allowedValues.Count <= 50)
            {
                var validation = dataWs.Range(3, c + 1, 10000, c + 1).CreateDataValidation();
                validation.List(string.Join(",", allowedValues));
                validation.InCellDropdown = true;
                validation.ErrorMessage = $"Please select from allowed values: {string.Join(", ", allowedValues)}";
                validation.ErrorTitle = "Invalid Value";
            }
        }

        // Column widths
        for (int c = 0; c < dataProperties.Count; c++)
        {
            var propName = dataProperties[c].Name;
            dataWs.Column(c + 1).Width = Math.Max(propName.Length + 4, 16);
        }

        // Auto-size based on header + instruction row content
        dataWs.Columns().AdjustToContents(1, 2);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Task.FromResult(stream.ToArray());
    }

    public async Task<ModuleEntityMetaDto> GetEntityMetadataWithRelationsAsync(DataSyncModule module, Guid tenantId)
    {
        if (!ModuleMap.TryGetValue(module, out var mapping))
            throw new ArgumentException($"Unknown module: {module}");

        var entityType = mapping.EntityType;
        var properties = GetEntityProperties(entityType);

        var meta = new ModuleEntityMetaDto
        {
            ModuleName = module.ToString(),
            EntityTypeName = entityType.Name,
            DisplayName = mapping.DisplayName,
            Columns = new List<ModuleColumnMetaDto>()
        };

        var businessKeys = mapping.BusinessKeys.ToHashSet();

        foreach (var prop in properties)
        {
            var colMeta = new ModuleColumnMetaDto
            {
                PropertyName = prop.Name,
                DisplayName = FormatDisplayName(prop.Name),
                DataType = GetDataTypeName(prop.PropertyType),
                IsRequired = IsRequiredProperty(prop),
                IsPrimaryKey = businessKeys.Contains(prop.Name),
                IsTenantScoped = prop.Name == "TenantId",
                MaxLength = GetMaxLength(prop),
                DefaultValue = GetDefaultDisplayValue(prop),
                AllowedValues = GetEnumValues(prop.PropertyType).ToList(),
                IsLookup = IsLookupProperty(prop),
                LookupSource = GetLookupSource(prop)
            };

            // Enhance with FK lookup info from EF Core model
            try
            {
                var efEntityType = _db.Model.FindEntityType(entityType);
                if (efEntityType != null)
                {
                    // Check if the property is a navigation property's FK
                    var navigations = efEntityType.GetNavigations();
                    foreach (var nav in navigations)
                    {
                        var foreignKey = nav.ForeignKey;
                        if (foreignKey.Properties.Any(p => p.Name == prop.Name))
                        {
                            colMeta.IsLookup = true;
                            var principalType = foreignKey.PrincipalEntityType;
                            colMeta.LookupSource = $"{principalType.ClrType.Name} ({GetEntityDisplayName(principalType.ClrType)})";
                            break;
                        }
                    }

                    // Also check skip navigations
                    if (!colMeta.IsLookup)
                    {
                        var skipNavs = efEntityType.GetSkipNavigations();
                        foreach (var nav in skipNavs)
                        {
                            if (nav.Name == prop.Name || prop.Name.EndsWith("Id"))
                            {
                                colMeta.IsLookup = true;
                                var principalType = nav.TargetEntityType;
                                colMeta.LookupSource = $"{principalType.ClrType.Name} ({GetEntityDisplayName(principalType.ClrType)})";
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read EF metadata for {Property}", prop.Name);
            }

            meta.Columns.Add(colMeta);
        }

        return await Task.FromResult(meta);
    }

    private string BuildColumnInstruction(PropertyInfo prop, bool isRequired, bool isBusinessKey)
    {
        var parts = new List<string>();

        if (isRequired) parts.Add("REQUIRED");
        if (isBusinessKey) parts.Add("Business Key");

        var dataType = GetDataTypeName(prop.PropertyType);
        var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        switch (dataType)
        {
            case "DateTime":
                parts.Add("Format: YYYY-MM-DD");
                break;
            case "Decimal":
            case "Double":
                parts.Add("Use dot decimal separator, no thousand separators");
                break;
            case "Boolean":
                parts.Add("Values: true / false");
                break;
            case "GUID":
                parts.Add("Full GUID format required");
                break;
        }

        var maxLen = GetMaxLength(prop);
        if (maxLen.HasValue)
            parts.Add($"Max {maxLen.Value} chars");

        var enumValues = GetEnumValues(underlyingType);
        if (enumValues.Length > 0)
            parts.Add($"Allowed: {string.Join(", ", enumValues)}");

        var lookupSrc = GetLookupSource(prop);
        if (lookupSrc != null)
            parts.Add($"Ref: {lookupSrc}");

        return parts.Count > 0 ? string.Join(" | ", parts) : "";
    }

    private List<string> GetAllowedValuesForDropdown(PropertyInfo prop)
    {
        var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        var values = new List<string>();

        // Enum values
        var enumValues = GetEnumValues(underlyingType);
        if (enumValues.Length > 0)
        {
            values.AddRange(enumValues);
            return values;
        }

        // Known FK patterns for string properties
        if (underlyingType == typeof(string) && DropdownColumns.Contains(prop.Name))
        {
            values.AddRange(GetKnownDropdownValues(prop.Name));
            return values;
        }

        return values;
    }

    private static List<string> GetKnownDropdownValues(string propertyName)
    {
        return propertyName switch
        {
            "Status" => new List<string> { "Active", "Inactive", "Pending", "Draft", "Open", "Closed", "Cancelled", "Planned", "Released", "In Progress", "Completed", "PLANNED", "RELEASED", "IN_PROGRESS", "TECO" },
            "Type" or "MaterialType" or "DocumentType" or "OrderType" => new List<string> { "RAW", "FG", "SFG", "PACK", "SERVICE", "PP01", "PP02" },
            "UOM" or "UnitOfMeasure" or "BaseUOM" => new List<string> { "EA", "KG", "L", "M", "M2", "M3", "PC", "SET", "BOX", "PAL" },
            "Plant" => new List<string> { "1000", "2000", "3000" },
            "StorageLocation" => new List<string> { "RM01", "RM02", "FG01", "FG02", "SP01" },
            "Currency" or "CurrencyCode" => new List<string> { "USD", "EUR", "GBP", "INR", "JPY" },
            "Priority" => new List<string> { "Low", "Medium", "High", "Critical" },
            "PaymentTerms" => new List<string> { "Net 15", "Net 30", "Net 45", "Net 60", "COD", "Prepaid" },
            "Department" => new List<string> { "PROD", "WHSE", "QA", "MAINT", "PROC", "SALES", "FIN" },
            "BOMUsage" => new List<string> { "Production", "Engineering", "Costing" },
            "ItemCategory" => new List<string> { "L", "R", "T", "K" },
            "ControlKey" => new List<string> { "PP01", "PP02", "PP03", "PM01" },
            "WorkCenterCategory" => new List<string> { "Machine", "Labor", "Pool", "Shift" },
            "Category" => new List<string> { "A", "B", "C", "D" },
            "ValuationClass" => new List<string> { "3000", "3001", "3100", "7900" },
            _ => new List<string>()
        };
    }

    private static string GetEntityDisplayName(Type entityType)
    {
        var name = entityType.Name.Replace("Entity", "");
        return string.Concat(name.Select((ch, i) =>
            i > 0 && char.IsUpper(ch) && !char.IsUpper(name[i - 1]) ? " " + ch : ch.ToString()));
    }

    private List<PropertyInfo> GetEntityProperties(Type entityType)
    {
        return entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.Name != "EntityState" && p.Name != "Navigation")
            .OrderBy(p => ExcludedProperties.Contains(p.Name) ? 1 : 0)
            .ThenBy(p => p.Name)
            .ToList();
    }

    private static string FormatDisplayName(string name)
    {
        return string.Concat(name.Select((ch, i) =>
            i > 0 && char.IsUpper(ch) && !char.IsUpper(name[i - 1]) ? " " + ch : ch.ToString()));
    }

    private static string GetDataTypeName(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.Name switch
        {
            nameof(String) => "String",
            nameof(Int32) => "Integer",
            nameof(Int64) => "Long",
            nameof(Decimal) => "Decimal",
            nameof(Double) => "Double",
            nameof(Boolean) => "Boolean",
            nameof(DateTime) => "DateTime",
            nameof(Guid) => "GUID",
            _ => underlying.Name
        };
    }

    private static bool IsRequiredProperty(PropertyInfo prop)
    {
        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        if (type == typeof(string))
        {
            var attr = prop.GetCustomAttribute<RequiredAttribute>();
            return attr != null;
        }
        if (type.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null)
            return true;
        return false;
    }

    private static int? GetMaxLength(PropertyInfo prop)
    {
        var attr = prop.GetCustomAttribute<MaxLengthAttribute>();
        return attr?.Length;
    }

    private static string[] GetEnumValues(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsEnum)
            return Enum.GetNames(underlying);
        return Array.Empty<string>();
    }

    private static string? GetDefaultDisplayValue(PropertyInfo prop)
    {
        try
        {
            var instance = Activator.CreateInstance(prop.DeclaringType!);
            var val = prop.GetValue(instance);
            if (val == null) return null;
            if (val is string s) return s == "" ? null : s;
            if (val is DateTime dt) return dt == default ? null : dt.ToString("yyyy-MM-dd");
            if (val is bool b) return b.ToString();
            return val.ToString();
        }
        catch { return null; }
    }

    private static bool IsLookupProperty(PropertyInfo prop)
    {
        var name = prop.Name;
        return name.EndsWith("Id") || name.EndsWith("Code") ||
               name is "PlantId" or "StorageLocationId" or "Plant" or "ValuationClass" or "UOM"
               or "Currency" or "Status" or "Type" or "Priority" or "Category";
    }

    private static string? GetLookupSource(PropertyInfo prop)
    {
        var name = prop.Name;
        return name switch
        {
            "PlantId" or "Plant" => "Plant Master",
            "StorageLocationId" or "StorageLocationCode" => "Storage Location Master",
            "Currency" or "CurrencyCode" => "Currency Master",
            "ValuationClass" => "Valuation Class",
            "UOM" or "UnitOfMeasure" => "UoM Master",
            "Status" => "Status (Active/Inactive/Pending)",
            "Type" or "MaterialType" or "DocumentType" => "Type Master",
            "Priority" => "Priority (Low/Medium/High/Critical)",
            "Category" => "Category Master",
            _ when name.EndsWith("Id") => $"{name[..^2]} Master",
            _ when name.EndsWith("Code") => $"{name[..^4]} Master",
            _ => null
        };
    }
}
