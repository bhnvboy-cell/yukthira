using System.ComponentModel.DataAnnotations;

namespace YuktiraERP.Core.Dtos;

public enum DataSyncModule
{
    MM_MaterialMaster,
    MM_Vendor,
    SD_Customer,
    SD_SalesOrder,
    SD_Inquiry,
    SD_Quotation,
    SD_Delivery,
    SD_BillingDocument,
    FI_GeneralLedger,
    FI_APEntry,
    FI_AREntry,
    FI_FixedAsset,
    FI_Account,
    FI_TaxCode,
    FI_Currency,
    FI_ExchangeRate,
    CO_CostCenter,
    CO_CostElement,
    CO_ProfitCenter,
    CO_InternalOrder,
    PP_ProductionPlan,
    PP_BillOfMaterial,
    PP_WorkCenter,
    PP_ProductionRouting,
    PP_ProductionOrder,
    QM_InspectionPlan,
    QM_InspectionLot,
    QM_QualityNotification,
    WM_StorageLocation,
    WM_Bin,
    WM_WarehouseTransfer,
    HR_Employee,
    HR_OrgUnit,
    HR_LeaveRequest,
    HR_PayrollEntry,
    HR_Attendance,
    LIMS_Sample,
    LIMS_Specification,
    LIMS_Instrument,
    PM_Equipment,
    PM_MaintenancePlan,
    PM_MaintenanceOrder,
    CRM_Lead,
    CRM_Opportunity,
    CRM_Contact,
    CRM_Campaign,
    PS_Project,
    PS_ProjectTask,
    BI_Report,
    Admin_Tenant,
    Admin_User
}

public class ModuleEntityMetaDto
{
    public string ModuleName { get; set; } = "";
    public string EntityTypeName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<ModuleColumnMetaDto> Columns { get; set; } = new();
}

public class ModuleColumnMetaDto
{
    public string PropertyName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool IsRequired { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsTenantScoped { get; set; }
    public int? MaxLength { get; set; }
    public string? DefaultValue { get; set; }
    public List<string> AllowedValues { get; set; } = new();
    public bool IsLookup { get; set; }
    public string? LookupSource { get; set; }
}

public class UploadPreviewRequest
{
    public DataSyncModule Module { get; set; }
    public string FileName { get; set; } = "";
    public int SheetIndex { get; set; }
    public List<Dictionary<string, string>> Rows { get; set; } = new();
}

public class ImportValidationResultDto
{
    public bool IsValid { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public int DuplicateRows { get; set; }
    public List<ImportRowErrorDto> RowErrors { get; set; } = new();
    public List<Dictionary<string, string>> StagedData { get; set; } = new();
    public string? SessionToken { get; set; }
}

public class ImportRowErrorDto
{
    public int RowNumber { get; set; }
    public string ColumnName { get; set; } = "";
    public string CellReference { get; set; } = "";
    public string ErrorCode { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public string Severity { get; set; } = "Error";
}

public class DataSyncExecuteRequest
{
    public DataSyncModule Module { get; set; }
    public string SessionToken { get; set; } = "";
    public bool SkipErrors { get; set; }
}

public class DataSyncExecuteResultDto
{
    public bool Success { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<ImportRowErrorDto> Errors { get; set; } = new();
    public string? TransactionId { get; set; }
    public long ElapsedMs { get; set; }
    public string Message { get; set; } = "";
}

public class ModuleSyncSessionDto
{
    public string SessionToken { get; set; } = "";
    public DataSyncModule Module { get; set; }
    public string FileName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public ImportValidationResultDto? ValidationResult { get; set; }
    public List<Dictionary<string, string>> Rows { get; set; } = new();
}

public class AvailableModulesResultDto
{
    public List<ModuleListItemDto> Modules { get; set; } = new();
}

public class ModuleListItemDto
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public string Category { get; set; } = "";
    public string EntityTypeName { get; set; } = "";
    public int ColumnCount { get; set; }
}
