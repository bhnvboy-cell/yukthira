using YuktiraERP.Core.Domain.Transaction;

namespace YuktiraERP.Core.Interfaces;

public interface ITransactionSequenceService
{
    Task<List<SequenceChainDto>> GetWorkflowChainsAsync();
    Task<SequenceChainDto?> GetChainByIdAsync(string chainId);
    Task<SequenceValidationResult> ValidateStepAsync(string chainId, string tcode, Dictionary<string, object>? context = null);
    Task<SequenceExecutionResult> ExecuteStepAsync(string chainId, string tcode, Guid? userId, Dictionary<string, object>? parameters = null);
    Task<List<SequenceStepStatusDto>> GetChainProgressAsync(string chainId, string? instanceId = null);
    Task<List<SequenceInstanceDto>> GetActiveInstancesAsync(string? chainId = null);
    Task<SequenceInstanceDto?> GetInstanceAsync(string instanceId);
}

public class SequenceChainDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Module { get; set; } = "";
    public List<SequenceStepDto> Steps { get; set; } = new();
    public string StartTCode { get; set; } = "";
    public string EndTCode { get; set; } = "";
}

public class SequenceStepDto
{
    public int Order { get; set; }
    public string TCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Module { get; set; } = "";
    public string Status { get; set; } = "PENDING";
    public List<string> Prerequisites { get; set; } = new();
    public List<string> PostConditions { get; set; } = new();
    public string MovementType { get; set; } = "";
    public bool Required { get; set; } = true;
    public Dictionary<string, object> DefaultParams { get; set; } = new();
}

public class SequenceValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public List<string> MissingPrerequisites { get; set; } = new();
    public List<string> CompletedSteps { get; set; } = new();
    public string CurrentStatus { get; set; } = "";
}

public class SequenceExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? DocumentId { get; set; }
    public string? NextStep { get; set; }
    public List<string> CompletedSteps { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
}

public class SequenceStepStatusDto
{
    public int Order { get; set; }
    public string TCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "PENDING";
    public bool IsCurrent { get; set; }
    public bool IsCompleted { get; set; }
    public string? CompletedAt { get; set; }
    public string? DocumentId { get; set; }
}

public class SequenceInstanceDto
{
    public string InstanceId { get; set; } = "";
    public string ChainId { get; set; } = "";
    public string ChainName { get; set; } = "";
    public string CurrentStep { get; set; } = "";
    public int CurrentStepOrder { get; set; }
    public string Status { get; set; } = "IN_PROGRESS";
    public string StartedAt { get; set; } = "";
    public string? CompletedAt { get; set; }
    public string? ReferenceDocument { get; set; }
    public List<SequenceStepStatusDto> Steps { get; set; } = new();
}
