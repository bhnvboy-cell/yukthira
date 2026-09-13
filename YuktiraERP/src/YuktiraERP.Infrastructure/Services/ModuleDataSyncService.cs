using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ModuleDataSyncService : IModuleDataSyncService
{
    private readonly YuktiraDbContext _db;
    private readonly IEventStoreService _eventStore;
    private readonly ILogger<ModuleDataSyncService> _logger;
    private readonly ConcurrentDictionary<string, ModuleSyncSessionDto> _sessions = new();

    public ModuleDataSyncService(
        YuktiraDbContext db,
        IEventStoreService eventStore,
        ILogger<ModuleDataSyncService> logger)
    {
        _db = db;
        _eventStore = eventStore;
        _logger = logger;
    }

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

    private static readonly HashSet<string> ExcludedProperties = new()
    {
        "Id", "CreatedAt", "UpdatedAt", "TenantId"
    };

    public async Task<List<ModuleListItemDto>> GetAvailableModulesAsync()
    {
        return await Task.FromResult(ModuleMap.Select(kvp => new ModuleListItemDto
        {
            Value = kvp.Key.ToString(),
            Label = kvp.Value.DisplayName,
            Category = kvp.Value.Category,
            EntityTypeName = kvp.Value.EntityType.Name,
            ColumnCount = GetEntityProperties(kvp.Value.EntityType).Count
        }).ToList());
    }

    public async Task<ModuleEntityMetaDto> GetEntityMetadataAsync(DataSyncModule module)
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
            Columns = properties.Select(p =>
            {
                var colMeta = new ModuleColumnMetaDto
                {
                    PropertyName = p.Name,
                    DisplayName = FormatDisplayName(p.Name),
                    DataType = GetDataTypeName(p.PropertyType),
                    IsRequired = IsRequiredProperty(p),
                    IsPrimaryKey = false,
                    IsTenantScoped = p.Name == "TenantId",
                    MaxLength = GetMaxLength(p),
                    DefaultValue = GetDefaultDisplayValue(p),
                    AllowedValues = GetEnumValues(p.PropertyType).ToList(),
                    IsLookup = IsLookupProperty(p),
                    LookupSource = GetLookupSource(p)
                };
                return colMeta;
            }).ToList()
        };

        // Mark business keys
        foreach (var bk in mapping.BusinessKeys)
        {
            var col = meta.Columns.FirstOrDefault(c => c.PropertyName == bk);
            if (col != null) col.IsPrimaryKey = true;
        }

        return await Task.FromResult(meta);
    }

    public async Task<byte[]> GenerateTemplateAsync(DataSyncModule module, Guid tenantId)
    {
        if (!ModuleMap.TryGetValue(module, out var mapping))
            throw new ArgumentException($"Unknown module: {module}");

        var entityType = mapping.EntityType;
        var properties = GetEntityProperties(entityType);

        using var workbook = new XLWorkbook();

        // ── Instruction Sheet ──
        var instrWs = workbook.Worksheets.Add("Instructions");
        instrWs.Cell(1, 1).Value = $"YuktiraERP — {mapping.DisplayName} Master Template";
        instrWs.Cell(1, 1).Style.Font.Bold = true;
        instrWs.Cell(1, 1).Style.Font.FontSize = 16;
        instrWs.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1F497D");

        instrWs.Cell(3, 1).Value = "Formatting Guidelines";
        instrWs.Cell(3, 1).Style.Font.Bold = true;
        instrWs.Cell(3, 1).Style.Font.FontSize = 12;

        var guidelines = new[]
        {
            "1. Do NOT modify the header row (Row 1) — column names must match exactly.",
            "2. All dates must be in YYYY-MM-DD format.",
            "3. All GUIDs must be valid format (e.g. 00000000-0000-0000-0000-000000000000).",
            "4. Required columns are indicated with red header text.",
            "5. Dropdown columns must use values from the allowed list.",
            "6. Leave cells empty to accept the system default value.",
            "7. Each row must have a unique business key (see 'IsBusinessKey' in the schema).",
            "8. Duplicate business keys within the same file will cause a validation error.",
            $"9. Tenant ID is auto-stamped. Do NOT include a TenantId column.",
            "10. The 'Status' column accepts values: Active, Inactive, Pending.",
            $"11. Tenant scope: {tenantId}",
            $"12. Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
        };
        for (int i = 0; i < guidelines.Length; i++)
            instrWs.Cell(5 + i, 1).Value = guidelines[i];

        instrWs.Column(1).Width = 100;

        // ── Schema Sheet ──
        var schemaWs = workbook.Worksheets.Add("Schema");
        var schemaHeaders = new[] { "Column Name", "Data Type", "Required", "Business Key", "Max Length", "Default", "Allowed Values", "Lookup Source" };
        for (int c = 0; c < schemaHeaders.Length; c++)
        {
            schemaWs.Cell(1, c + 1).Value = schemaHeaders[c];
            schemaWs.Cell(1, c + 1).Style.Font.Bold = true;
            schemaWs.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1F497D");
            schemaWs.Cell(1, c + 1).Style.Font.FontColor = XLColor.White;
        }

        var businessKeys = mapping.BusinessKeys.ToHashSet();
        int row = 2;
        foreach (var prop in properties)
        {
            schemaWs.Cell(row, 1).Value = prop.Name;
            schemaWs.Cell(row, 2).Value = GetDataTypeName(prop.PropertyType);
            schemaWs.Cell(row, 3).Value = IsRequiredProperty(prop) ? "YES" : "no";
            schemaWs.Cell(row, 4).Value = businessKeys.Contains(prop.Name) ? "KEY" : "";
            schemaWs.Cell(row, 5).Value = GetMaxLength(prop)?.ToString() ?? "";
            schemaWs.Cell(row, 6).Value = GetDefaultDisplayValue(prop) ?? "";
            schemaWs.Cell(row, 7).Value = string.Join(", ", GetEnumValues(prop.PropertyType));
            schemaWs.Cell(row, 8).Value = GetLookupSource(prop) ?? "";
            row++;
        }

        // ── Data Sheet ──
        var dataWs = workbook.Worksheets.Add("Data");
        var dataProperties = properties.Where(p => !ExcludedProperties.Contains(p.Name)).ToList();
        var requiredProps = new HashSet<string>(properties.Where(p => IsRequiredProperty(p)).Select(p => p.Name));

        for (int c = 0; c < dataProperties.Count; c++)
        {
            var prop = dataProperties[c];
            var cell = dataWs.Cell(1, c + 1);
            cell.Value = prop.Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F497D");
            cell.Style.Font.FontColor = XLColor.White;

            if (requiredProps.Contains(prop.Name))
            {
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.FromHtml("#FF0000");
            }
        }

        // Data validation for enum columns
        for (int c = 0; c < dataProperties.Count; c++)
        {
            var prop = dataProperties[c];
            var enumValues = GetEnumValues(prop.PropertyType).ToList();
            if (enumValues.Count > 0 && enumValues.Count <= 50)
            {
                dataWs.Range(2, c + 1, 10000, c + 1).CreateDataValidation().List(string.Join(",", enumValues));
            }
        }

        dataWs.Columns().AdjustToContents();
        dataWs.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1F497D");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Task.FromResult(stream.ToArray());
    }

    public async Task<ImportValidationResultDto> ValidateUploadAsync(
        DataSyncModule module, List<Dictionary<string, string>> rows, Guid tenantId)
    {
        if (!ModuleMap.TryGetValue(module, out var mapping))
            throw new ArgumentException($"Unknown module: {module}");

        var entityType = mapping.EntityType;
        var properties = GetEntityProperties(entityType).Where(p => !ExcludedProperties.Contains(p.Name)).ToList();
        var businessKeys = mapping.BusinessKeys.ToHashSet();
        var requiredProps = properties.Where(p => IsRequiredProperty(p)).Select(p => p.Name).ToHashSet();
        var propertyDict = properties.ToDictionary(p => p.Name, p => p);

        var result = new ImportValidationResultDto
        {
            TotalRows = rows.Count,
            IsValid = true,
            StagedData = rows
        };

        var seenKeys = new Dictionary<string, int>();
        var errors = new List<ImportRowErrorDto>();

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            int rowNum = i + 2; // Excel row (1-based, header is row 1)
            bool rowHasError = false;

            // Check required fields
            foreach (var req in requiredProps)
            {
                if (!row.ContainsKey(req) || string.IsNullOrWhiteSpace(row[req]))
                {
                    errors.Add(new ImportRowErrorDto
                    {
                        RowNumber = rowNum,
                        ColumnName = req,
                        CellReference = GetCellReference(1, req, properties),
                        ErrorCode = "REQUIRED_MISSING",
                        ErrorMessage = $"Required field '{FormatDisplayName(req)}' is missing or empty",
                        Severity = "Error"
                    });
                    rowHasError = true;
                }
            }

            // Check business key uniqueness within file
            foreach (var bk in businessKeys)
            {
                if (row.TryGetValue(bk, out var bkValue) && !string.IsNullOrWhiteSpace(bkValue))
                {
                    var key = $"{bk}:{bkValue.Trim().ToUpperInvariant()}";
                    if (seenKeys.TryGetValue(key, out var firstRow))
                    {
                        errors.Add(new ImportRowErrorDto
                        {
                            RowNumber = rowNum,
                            ColumnName = bk,
                            CellReference = GetCellReference(i + 2, bk, properties),
                            ErrorCode = "DUPLICATE_KEY",
                            ErrorMessage = $"Duplicate value '{bkValue}' for business key '{FormatDisplayName(bk)}' (first seen in row {firstRow})",
                            Severity = "Error"
                        });
                        rowHasError = true;
                    }
                    else
                    {
                        seenKeys[key] = rowNum;
                    }
                }
            }

            // Validate each column
            foreach (var prop in properties)
            {
                if (!row.TryGetValue(prop.Name, out var cellValue) || string.IsNullOrWhiteSpace(cellValue))
                    continue;

                var validationError = ValidateCellValue(prop, cellValue, tenantId);
                if (validationError != null)
                {
                    errors.Add(new ImportRowErrorDto
                    {
                        RowNumber = rowNum,
                        ColumnName = prop.Name,
                        CellReference = GetCellReference(i + 2, prop.Name, properties),
                        ErrorCode = validationError.Value.Code,
                        ErrorMessage = validationError.Value.Message,
                        Severity = "Error"
                    });
                    rowHasError = true;
                }
            }

            if (rowHasError) result.ErrorRows++;
        }

        result.ValidRows = result.TotalRows - result.ErrorRows;
        result.RowErrors = errors;
        result.IsValid = errors.Count == 0;
        result.DuplicateRows = errors.Count(e => e.ErrorCode == "DUPLICATE_KEY");

        return await Task.FromResult(result);
    }

    public async Task<DataSyncExecuteResultDto> ExecuteSyncAsync(
        DataSyncModule module, List<Dictionary<string, string>> rows, bool skipErrors,
        Guid tenantId, Guid userId)
    {
        var sw = Stopwatch.StartNew();

        if (!ModuleMap.TryGetValue(module, out var mapping))
            throw new ArgumentException($"Unknown module: {module}");

        var entityType = mapping.EntityType;
        var properties = GetEntityProperties(entityType).Where(p => !ExcludedProperties.Contains(p.Name)).ToList();
        var businessKeys = mapping.BusinessKeys;
        var propertyDict = properties.ToDictionary(p => p.Name, p => p);

        var result = new DataSyncExecuteResultDto();
        var errors = new List<ImportRowErrorDto>();

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)!
                .MakeGenericMethod(entityType);
            var dbSet = setMethod.Invoke(_db, null)!;
            var dbSetAsQueryable = dbSet as IQueryable ?? ((System.Collections.IEnumerable)dbSet).Cast<object>().AsQueryable();
            var existingEntities = await LoadExistingEntitiesAsync(dbSetAsQueryable, entityType, businessKeys, tenantId);
            var primaryKeyProp = entityType.GetProperty("Id");

            int inserted = 0, updated = 0, skipped = 0, failed = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                int rowNum = i + 2;

                try
                {
                    var entity = Activator.CreateInstance(entityType)!;

                    // Stamp TenantId
                    var tenantProp = entityType.GetProperty("TenantId");
                    if (tenantProp != null && tenantProp.PropertyType == typeof(Guid))
                        tenantProp.SetValue(entity, tenantId);

                    // Map row data to entity
                    foreach (var prop in properties)
                    {
                        if (ExcludedProperties.Contains(prop.Name)) continue;
                        if (prop.Name == "TenantId") continue;

                        if (row.TryGetValue(prop.Name, out var cellValue) && !string.IsNullOrWhiteSpace(cellValue))
                        {
                            var convertedValue = ConvertValue(prop, cellValue);
                            if (convertedValue != null)
                                prop.SetValue(entity, convertedValue);
                        }
                        else
                        {
                            // Set default if not provided
                            var defaultVal = GetDefaultValue(prop);
                            if (defaultVal != null)
                                prop.SetValue(entity, defaultVal);
                        }
                    }

                    // UPSERT: find existing by business key
                    object? existingEntity = FindExistingByBusinessKey(existingEntities, entityType, businessKeys, row, tenantId);

                    if (existingEntity != null)
                    {
                        // UPDATE: copy all non-key properties
                        foreach (var prop in properties)
                        {
                            if (ExcludedProperties.Contains(prop.Name)) continue;
                            if (businessKeys.Contains(prop.Name)) continue;
                            if (prop.Name == "TenantId") continue;
                            if (!prop.CanWrite) continue;

                            var newVal = prop.GetValue(entity);
                            if (newVal != null)
                                prop.SetValue(existingEntity, newVal);
                        }

                        var updatedAtProp = entityType.GetProperty("UpdatedAt");
                        if (updatedAtProp != null)
                            updatedAtProp.SetValue(existingEntity, DateTime.UtcNow);

                        updated++;
                    }
                    else
                    {
                        // INSERT
                        var createdAtProp = entityType.GetProperty("CreatedAt");
                        if (createdAtProp != null)
                            createdAtProp.SetValue(entity, DateTime.UtcNow);

                        var dbSetType = dbSet.GetType();
                        var addMethod = dbSetType.GetMethod("Add", new[] { entityType });
                        addMethod?.Invoke(dbSet, new object[] { entity });
                        inserted++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add(new ImportRowErrorDto
                    {
                        RowNumber = rowNum,
                        ColumnName = "*",
                        CellReference = $"Row {rowNum}",
                        ErrorCode = "SYNC_ERROR",
                        ErrorMessage = ex.Message,
                        Severity = "Error"
                    });

                    if (!skipErrors)
                    {
                        await transaction.RollbackAsync();
                        return new DataSyncExecuteResultDto
                        {
                            Success = false,
                            Failed = failed,
                            Errors = errors,
                            ElapsedMs = sw.ElapsedMilliseconds,
                            Message = $"Sync aborted at row {rowNum}: {ex.Message}"
                        };
                    }
                }
            }

            if (failed > 0 && !skipErrors)
            {
                await transaction.RollbackAsync();
                return new DataSyncExecuteResultDto
                {
                    Success = false,
                    Failed = failed,
                    Errors = errors,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Message = $"Sync rolled back: {failed} errors encountered"
                };
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Append domain event for auditability
            try
            {
                await _eventStore.AppendEventAsync(new DomainEventEnvelope
                {
                    AggregateId = tenantId,
                    EventType = "DataSync.Completed",
                    AggregateType = AggregateType.System,
                    EventData = JsonSerializer.Serialize(new
                    {
                        Module = module.ToString(),
                        Inserted = inserted,
                        Updated = updated,
                        Skipped = skipped,
                        Failed = failed,
                        TotalRows = rows.Count
                    }),
                    Version = 1,
                    TenantId = tenantId,
                    UserId = userId.ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        ["ModuleName"] = module.ToString(),
                        ["Action"] = "BulkSync"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to append domain event for data sync");
            }

            sw.Stop();

            return new DataSyncExecuteResultDto
            {
                Success = true,
                Inserted = inserted,
                Updated = updated,
                Skipped = skipped,
                Failed = failed,
                Errors = errors,
                TransactionId = Guid.NewGuid().ToString(),
                ElapsedMs = sw.ElapsedMilliseconds,
                Message = $"Sync complete: {inserted} inserted, {updated} updated, {skipped} skipped, {failed} failed"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Data sync transaction failed for module {Module}", module);

            return new DataSyncExecuteResultDto
            {
                Success = false,
                Failed = rows.Count,
                ElapsedMs = sw.ElapsedMilliseconds,
                Message = $"Transaction failed: {ex.Message}"
            };
        }
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

    private static object? GetDefaultValue(PropertyInfo prop)
    {
        try
        {
            var instance = Activator.CreateInstance(prop.DeclaringType!);
            return prop.GetValue(instance);
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

    private string GetCellReference(int rowNum, string propName, List<PropertyInfo> properties)
    {
        var colIdx = properties.IndexOf(properties.First(p => p.Name == propName));
        return $"{(char)('A' + colIdx)}{rowNum}";
    }

    private (string Code, string Message)? ValidateCellValue(PropertyInfo prop, string cellValue, Guid tenantId)
    {
        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (type == typeof(Guid))
        {
            if (!Guid.TryParse(cellValue, out _))
                return ("INVALID_GUID", $"'{cellValue}' is not a valid GUID");
        }
        else if (type == typeof(int))
        {
            if (!int.TryParse(cellValue, out _))
                return ("INVALID_INT", $"'{cellValue}' is not a valid integer");
        }
        else if (type == typeof(long))
        {
            if (!long.TryParse(cellValue, out _))
                return ("INVALID_LONG", $"'{cellValue}' is not a valid long integer");
        }
        else if (type == typeof(decimal))
        {
            if (!decimal.TryParse(cellValue, out _))
                return ("INVALID_DECIMAL", $"'{cellValue}' is not a valid decimal");
        }
        else if (type == typeof(double))
        {
            if (!double.TryParse(cellValue, out _))
                return ("INVALID_DOUBLE", $"'{cellValue}' is not a valid number");
        }
        else if (type == typeof(bool))
        {
            if (!bool.TryParse(cellValue, out _))
                return ("INVALID_BOOL", $"'{cellValue}' is not a valid boolean (use true/false)");
        }
        else if (type == typeof(DateTime))
        {
            if (!DateTime.TryParse(cellValue, out _))
                return ("INVALID_DATE", $"'{cellValue}' is not a valid date (use YYYY-MM-DD)");
        }
        else if (type.IsEnum)
        {
            if (!Enum.TryParse(type, cellValue, true, out _))
                return ("INVALID_ENUM", $"'{cellValue}' is not a valid value. Allowed: {string.Join(", ", Enum.GetNames(type))}");
        }

        var maxLength = GetMaxLength(prop);
        if (maxLength.HasValue && cellValue.Length > maxLength.Value)
            return ("STRING_TOO_LONG", $"Value exceeds max length of {maxLength.Value} characters");

        return null;
    }

    private static object? ConvertValue(PropertyInfo prop, string cellValue)
    {
        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (type == typeof(string)) return cellValue;
        if (type == typeof(Guid)) return Guid.TryParse(cellValue, out var g) ? g : null;
        if (type == typeof(int)) return int.TryParse(cellValue, out var i) ? i : null;
        if (type == typeof(long)) return long.TryParse(cellValue, out var l) ? l : null;
        if (type == typeof(decimal)) return decimal.TryParse(cellValue, out var d) ? d : null;
        if (type == typeof(double)) return double.TryParse(cellValue, out var dl) ? dl : null;
        if (type == typeof(bool)) return bool.TryParse(cellValue, out var b) ? b : null;
        if (type == typeof(DateTime)) return DateTime.TryParse(cellValue, out var dt) ? dt : null;
        if (type.IsEnum) return Enum.TryParse(type, cellValue, true, out var e) ? e : null;

        return cellValue;
    }

    private async Task<Dictionary<string, List<object>>> LoadExistingEntitiesAsync(
        IQueryable dbSet, Type entityType, string[] businessKeys, Guid tenantId)
    {
        var result = new Dictionary<string, List<object>>();
        try
        {
            var tenantProp = entityType.GetProperty("TenantId");
            IQueryable query = dbSet;
            if (tenantProp != null)
            {
                var asQueryableMethod = typeof(Enumerable).GetMethod("AsQueryable")!
                    .MakeGenericMethod(entityType);
                var typedQuery = asQueryableMethod.Invoke(null, new object[] { dbSet }) as IQueryable;

                var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
                var tenantAccess = System.Linq.Expressions.Expression.Property(parameter, tenantProp);
                var tenantConstant = System.Linq.Expressions.Expression.Constant(tenantId);
                var equality = System.Linq.Expressions.Expression.Equal(tenantAccess, tenantConstant);
                var lambda = System.Linq.Expressions.Expression.Lambda(equality, parameter);

                var whereMethod = typeof(Queryable).GetMethods()
                    .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(entityType);
                query = whereMethod.Invoke(null, new object[] { typedQuery!, lambda }) as IQueryable;
            }

            var toListMethod = typeof(Enumerable).GetMethod("ToList")!
                .MakeGenericMethod(entityType);
            var entities = toListMethod.Invoke(null, new object[] { query })! as System.Collections.IList;

            if (entities == null) return result;

            foreach (var entity in entities)
            {
                foreach (var bk in businessKeys)
                {
                    var prop = entityType.GetProperty(bk);
                    if (prop == null) continue;
                    var keyVal = prop.GetValue(entity)?.ToString()?.ToUpperInvariant();
                    if (string.IsNullOrEmpty(keyVal)) continue;

                    if (!result.ContainsKey(bk))
                        result[bk] = new List<object>();
                    result[bk].Add(entity);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load existing entities for upsert");
        }

        return await Task.FromResult(result);
    }

    private static object? FindExistingByBusinessKey(
        Dictionary<string, List<object>> existingEntities,
        Type entityType,
        string[] businessKeys,
        Dictionary<string, string> row,
        Guid tenantId)
    {
        foreach (var bk in businessKeys)
        {
            if (!row.TryGetValue(bk, out var bkValue) || string.IsNullOrWhiteSpace(bkValue))
                continue;

            if (!existingEntities.TryGetValue(bk, out var entities))
                continue;

            var prop = entityType.GetProperty(bk);
            if (prop == null) continue;

            foreach (var entity in entities)
            {
                var existingVal = prop.GetValue(entity)?.ToString()?.Trim();
                if (string.Equals(existingVal, bkValue.Trim(), StringComparison.OrdinalIgnoreCase))
                    return entity;
            }
        }

        return null;
    }
}

public class SyncSessionStore : ISyncSessionStore
{
    private readonly ConcurrentDictionary<string, ModuleSyncSessionDto> _sessions = new();

    public Task<ModuleSyncSessionDto?> GetSessionAsync(string sessionToken)
    {
        _sessions.TryGetValue(sessionToken, out var session);
        return Task.FromResult(session);
    }

    public Task<string> CreateSessionAsync(ModuleSyncSessionDto session)
    {
        var token = Guid.NewGuid().ToString("N");
        session.SessionToken = token;
        _sessions[token] = session;
        return Task.FromResult(token);
    }

    public Task RemoveSessionAsync(string sessionToken)
    {
        _sessions.TryRemove(sessionToken, out _);
        return Task.CompletedTask;
    }
}
