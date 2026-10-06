namespace YuktiraERP.Core.Interfaces;

public class WorkflowThresholdLevelDto
{
    public int Level { get; set; } = 1;
    public string ApproverRole { get; set; } = string.Empty;
}

public class WorkflowThresholdDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public string Plant { get; set; } = string.Empty;
    public string DepartmentKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<WorkflowThresholdLevelDto> Levels { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class WorkflowThresholdCreateRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public string Plant { get; set; } = string.Empty;
    public string DepartmentKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string ApproverRole { get; set; } = string.Empty;
    public List<WorkflowThresholdLevelDto>? Levels { get; set; }
}

public class WorkflowThresholdUpdateRequest : WorkflowThresholdCreateRequest
{
}

public class WorkflowThresholdSaveResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public WorkflowThresholdDto? Threshold { get; set; }
}

public class WorkflowThresholdValidationResult
{
    public bool IsValid { get; set; }
    public string Error { get; set; } = string.Empty;
}

public interface IWorkflowThresholdService
{
    Task<List<WorkflowThresholdDto>> ListAsync(Guid tenantId, string? documentType = null);
    Task<WorkflowThresholdDto?> GetAsync(Guid tenantId, Guid id);
    Task<WorkflowThresholdSaveResult> CreateAsync(Guid tenantId, WorkflowThresholdCreateRequest request);
    Task<WorkflowThresholdSaveResult> UpdateAsync(Guid tenantId, Guid id, WorkflowThresholdUpdateRequest request);
    Task<bool> DeleteAsync(Guid tenantId, Guid id);
    Task<WorkflowThresholdDto?> PreviewAsync(Guid tenantId, string documentType, decimal amount, string plant = "", string departmentKey = "");
    Task<WorkflowThresholdValidationResult> ValidateNoOverlapAsync(Guid tenantId, string documentType, decimal minAmount, decimal maxAmount, Guid? excludeId = null);
}
