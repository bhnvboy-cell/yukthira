namespace YuktiraERP.Core.Interfaces;

public interface ITCodeGeneratorService
{
    Task<List<TCodeDefinitionDto>> GetAllDefinitionsAsync(Guid tenantId);
    Task<TCodeDefinitionDto?> GetDefinitionAsync(Guid tenantId, string code);
    Task<TCodeDefinitionDto> CreateDefinitionAsync(Guid tenantId, string code, string name, string module, string description, Guid userId);
    Task<bool> DeleteDefinitionAsync(Guid tenantId, Guid id);
    Task<List<TCodeFieldDto>> GetFieldsAsync(Guid tenantId, Guid tcodeId);
    Task<TCodeFieldDto> AddFieldAsync(Guid tenantId, Guid tcodeId, TCodeFieldDto field);
    Task<bool> RemoveFieldAsync(Guid tenantId, Guid fieldId);
    Task<List<TCodeDataDto>> GetRecordsAsync(Guid tenantId, Guid tcodeId, string? search = null, int page = 1, int pageSize = 50);
    Task<TCodeDataDto> CreateRecordAsync(Guid tenantId, Guid tcodeId, Dictionary<string, object> data, Guid userId);
    Task<TCodeDataDto> UpdateRecordAsync(Guid tenantId, Guid recordId, Dictionary<string, object> data, Guid userId);
    Task<bool> DeleteRecordAsync(Guid tenantId, Guid recordId);
    Task<TCodeDataDto?> GetRecordAsync(Guid tenantId, Guid recordId);
    Task AutoGeneratePermissionsAsync(Guid tenantId, Guid tcodeId, string code);
    Task<TCodeDataDto> AdvanceWorkflowAsync(Guid tenantId, Guid recordId, string targetNode, Guid userId);
    Task<TCodeDataDto> RejectWorkflowAsync(Guid tenantId, Guid recordId, Guid userId);
    Task<List<string>> GetAvailableTransitionsAsync(Guid tenantId, Guid recordId);
    Task<TCodeWorkflowDto> GetRecordWorkflowAsync(Guid tenantId, Guid recordId);
    TCodeWorkflowDto GetWorkflowDefinition();
}

public class TCodeWorkflowDto
{
    public string CurrentNode { get; set; } = "START";
    public string Status { get; set; } = "Draft";
    public bool CanEdit { get; set; } = true;
    public bool CanSubmit { get; set; }
    public List<WorkflowNodeDto> Nodes { get; set; } = new();
    public List<WorkflowTransitionDto> AvailableTransitions { get; set; } = new();
}

public class WorkflowNodeDto
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string Color { get; set; } = "secondary";
    public string Icon { get; set; } = "bi-circle";
    public string Role { get; set; } = "USER";
}

public class WorkflowTransitionDto
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "bi-arrow-right";
    public string? RequiredRole { get; set; }
}

public class TCodeDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Module { get; set; } = "";
    public string Description { get; set; } = "";
    public bool HasWorkflow { get; set; }
    public bool HasNumberRange { get; set; }
    public string Prefix { get; set; } = "";
    public string Status { get; set; } = "Active";
    public List<TCodeFieldDto> Fields { get; set; } = new();
}

public class TCodeFieldDto
{
    public Guid Id { get; set; }
    public string FieldName { get; set; } = "";
    public string FieldLabel { get; set; } = "";
    public string DataType { get; set; } = "TEXT";
    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; } = true;
    public string DefaultValue { get; set; } = "";
    public string ValidationRuleJson { get; set; } = "{}";
    public string ConditionalVisibilityJson { get; set; } = "{}";
    public int OrderIndex { get; set; }
    public int Width { get; set; } = 200;
    public string SectionName { get; set; } = "General";
}

public class TCodeDataDto
{
    public Guid Id { get; set; }
    public Guid TCodeId { get; set; }
    public string RecordId { get; set; } = "";
    public Dictionary<string, object> Data { get; set; } = new();
    public string Status { get; set; } = "Draft";
    public string WorkflowNode { get; set; } = "START";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
