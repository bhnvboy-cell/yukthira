using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Interfaces;

public class WorkflowNodeConfig
{
    public Guid NodeId { get; set; }
    public WorkflowNodeType NodeType { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? ConfigJson { get; set; }
    public List<WorkflowEdgeConfig> OutgoingEdges { get; set; } = new();
}

public class WorkflowEdgeConfig
{
    public Guid EdgeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string? ConditionExpression { get; set; }
    public string? Label { get; set; }
}

public class WorkflowValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<WorkflowValidationError> Errors { get; set; } = new();
}

public class WorkflowValidationError
{
    public string NodeId { get; set; } = "";
    public string Field { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "Error";
}

public class WorkflowSimulationResult
{
    public string Status { get; set; } = "";
    public List<string> ExecutionPath { get; set; } = new();
    public List<string> Decisions { get; set; } = new();
    public int TotalNodes { get; set; }
    public int CompletedNodes { get; set; }
    public string EstimatedCompletion { get; set; } = "";
}

public class TimerScheduleEntity
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public DateTime TriggerAt { get; set; }
    public string Action { get; set; } = "";
    public bool IsFired { get; set; }
}

public class WorkflowApiCallConfig
{
    public string Url { get; set; } = "";
    public string Method { get; set; } = "POST";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string BodyTemplate { get; set; } = "{}";
    public string? SuccessCondition { get; set; }
    public string? OutputVariable { get; set; }
}

public interface IWorkflowEngine
{
    Task<Guid> StartWorkflowAsync(Guid workflowId, Guid tenantId, string entityName, string entityId, Guid startedBy, Dictionary<string, object>? variables = null);
    Task ProcessNodeAsync(Guid instanceId, Guid nodeId, Dictionary<string, object>? data = null);
    Task CompleteWorkflowAsync(Guid instanceId);
    Task TerminateWorkflowAsync(Guid instanceId);
    Task<WorkflowNodeConfig?> GetNextNodeAsync(Guid instanceId, Guid currentNodeId, Dictionary<string, object>? context = null);
    Task<List<WorkflowNodeConfig>> GetWorkflowNodesAsync(Guid workflowId);
    Task<WorkflowValidationResult> ValidateWorkflowDefinitionAsync(Guid workflowId);
    Task<WorkflowSimulationResult> SimulateWorkflowAsync(Guid workflowId, Dictionary<string, object> variables);
    Task<bool> EvaluateConditionAsync(string expression, Dictionary<string, object> variables);
    Task<Dictionary<string, object>> ExecuteApiCallAsync(WorkflowApiCallConfig config, Dictionary<string, object> variables);
    Task ScheduleTimerAsync(Guid workflowInstanceId, TimeSpan delay, string action);
}
