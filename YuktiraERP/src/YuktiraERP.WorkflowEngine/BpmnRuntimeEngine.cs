using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.WorkflowEngine;

public class WorkflowEngineService : IWorkflowEngine
{
    private static readonly List<WorkflowInstance> _instances = new();
    private static readonly Dictionary<Guid, List<WorkflowNodeConfig>> _definitions = new();
    private static readonly object _lock = new();

    public async Task<Guid> StartWorkflowAsync(Guid workflowId, Guid tenantId, string entityName, string entityId, Guid startedBy, Dictionary<string, object>? variables = null)
    {
        var instanceId = Guid.NewGuid();
        var nodes = await GetWorkflowNodesAsync(workflowId);
        var startNode = nodes.FirstOrDefault(n => n.NodeType == WorkflowNodeType.Start);

        lock (_lock)
        {
            _definitions[workflowId] = nodes;
            _instances.Add(new WorkflowInstance
            {
                Id = instanceId,
                WorkflowId = workflowId,
                TenantId = tenantId,
                EntityName = entityName,
                EntityId = entityId,
                CurrentNodeId = startNode?.NodeId,
                Status = "ACTIVE",
                Variables = variables ?? new(),
                StartedBy = startedBy,
                StartedAt = DateTime.UtcNow
            });
        }
        return instanceId;
    }

    public Task ProcessNodeAsync(Guid instanceId, Guid nodeId, Dictionary<string, object>? data = null)
    {
        lock (_lock)
        {
            var instance = _instances.FirstOrDefault(i => i.Id == instanceId);
            if (instance == null) throw new InvalidOperationException("Workflow instance not found");

            var workflowNodes = _definitions.GetValueOrDefault(instance.WorkflowId, new());
            var currentNode = workflowNodes.FirstOrDefault(n => n.NodeId == nodeId);
            if (currentNode == null) throw new InvalidOperationException("Node not found");

            if (data != null)
            {
                foreach (var kv in data)
                    instance.Variables[kv.Key] = kv.Value;
            }

            var nextEdge = currentNode.OutgoingEdges.FirstOrDefault();
            if (nextEdge != null)
            {
                var nextNode = workflowNodes.FirstOrDefault(n => n.NodeId == nextEdge.ToNodeId);
                instance.CurrentNodeId = nextNode?.NodeId;
            }
            else
            {
                instance.CurrentNodeId = null;
            }

            if (currentNode.NodeType == WorkflowNodeType.End || instance.CurrentNodeId == null)
            {
                instance.Status = "COMPLETED";
                instance.CompletedAt = DateTime.UtcNow;
            }
        }
        return Task.CompletedTask;
    }

    public Task CompleteWorkflowAsync(Guid instanceId)
    {
        lock (_lock)
        {
            var instance = _instances.FirstOrDefault(i => i.Id == instanceId);
            if (instance != null)
            {
                instance.Status = "COMPLETED";
                instance.CompletedAt = DateTime.UtcNow;
            }
        }
        return Task.CompletedTask;
    }

    public Task TerminateWorkflowAsync(Guid instanceId)
    {
        lock (_lock)
        {
            var instance = _instances.FirstOrDefault(i => i.Id == instanceId);
            if (instance != null)
            {
                instance.Status = "TERMINATED";
                instance.CompletedAt = DateTime.UtcNow;
            }
        }
        return Task.CompletedTask;
    }

    public Task<WorkflowNodeConfig?> GetNextNodeAsync(Guid instanceId, Guid currentNodeId, Dictionary<string, object>? context = null)
    {
        lock (_lock)
        {
            var instance = _instances.FirstOrDefault(i => i.Id == instanceId);
            if (instance == null) return Task.FromResult<WorkflowNodeConfig?>(null);

            var nodes = _definitions.GetValueOrDefault(instance.WorkflowId, new());
            var currentNode = nodes.FirstOrDefault(n => n.NodeId == currentNodeId);
            if (currentNode == null) return Task.FromResult<WorkflowNodeConfig?>(null);

            foreach (var edge in currentNode.OutgoingEdges)
            {
                if (string.IsNullOrEmpty(edge.ConditionExpression))
                {
                    var nextNode = nodes.FirstOrDefault(n => n.NodeId == edge.ToNodeId);
                    return Task.FromResult(nextNode);
                }

                if (context != null && EvaluateCondition(edge.ConditionExpression, context))
                {
                    var nextNode = nodes.FirstOrDefault(n => n.NodeId == edge.ToNodeId);
                    return Task.FromResult(nextNode);
                }
            }
            return Task.FromResult<WorkflowNodeConfig?>(null);
        }
    }

    public Task<List<WorkflowNodeConfig>> GetWorkflowNodesAsync(Guid workflowId)
    {
        var nodes = GenerateSampleWorkflow(workflowId);
        lock (_lock) { _definitions[workflowId] = nodes; }
        return Task.FromResult(nodes);
    }

    public Task<WorkflowValidationResult> ValidateWorkflowDefinitionAsync(Guid workflowId)
    {
        var nodes = GenerateSampleWorkflow(workflowId);
        var result = new WorkflowValidationResult();

        var validTypes = new HashSet<string> { "START", "APPROVAL", "TASK", "DECISION", "EMAIL", "END", "TIMER", "API_CALL" };

        foreach (var node in nodes)
        {
            if (!validTypes.Contains(node.NodeType.ToString().ToUpperInvariant()))
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = node.NodeId.ToString(),
                    Field = "NodeType",
                    Message = $"Invalid node type '{node.NodeType}'",
                    Severity = "Error"
                });
            }
        }

        var startNodes = nodes.Where(n => n.NodeType == WorkflowNodeType.Start).ToList();
        if (startNodes.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add(new WorkflowValidationError { Field = "Workflow", Message = "No START node found" });
        }
        else if (startNodes.Count > 1)
        {
            result.IsValid = false;
            result.Errors.Add(new WorkflowValidationError { Field = "Workflow", Message = "Multiple START nodes found" });
        }

        var endNodes = nodes.Where(n => n.NodeType == WorkflowNodeType.End).ToList();
        foreach (var end in endNodes)
        {
            if (end.OutgoingEdges.Count > 0)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = end.NodeId.ToString(),
                    Field = "OutgoingEdges",
                    Message = $"END node '{end.Label}' has outgoing edges"
                });
            }
        }

        var decisionNodes = nodes.Where(n => n.NodeType == WorkflowNodeType.Decision).ToList();
        foreach (var dec in decisionNodes)
        {
            if (dec.OutgoingEdges.Count < 2)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = dec.NodeId.ToString(),
                    Field = "OutgoingEdges",
                    Message = $"DECISION node '{dec.Label}' must have at least 2 outgoing edges"
                });
            }

            var emptyCond = dec.OutgoingEdges.Where(e => string.IsNullOrWhiteSpace(e.ConditionExpression)).ToList();
            if (emptyCond.Count != 1)
            {
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = dec.NodeId.ToString(),
                    Field = "ConditionExpression",
                    Message = $"DECISION node '{dec.Label}' should have exactly one default edge",
                    Severity = "Warning"
                });
            }
        }

        return Task.FromResult(result);
    }

    public Task<WorkflowSimulationResult> SimulateWorkflowAsync(Guid workflowId, Dictionary<string, object> variables)
    {
        var nodes = GenerateSampleWorkflow(workflowId);
        var result = new WorkflowSimulationResult();
        var startNode = nodes.FirstOrDefault(n => n.NodeType == WorkflowNodeType.Start);
        if (startNode == null)
        {
            result.Status = "Failed: No START node";
            return Task.FromResult(result);
        }

        var visited = new HashSet<Guid>();
        var current = startNode;
        result.TotalNodes = nodes.Count;
        var maxIter = 100;
        var iter = 0;

        while (current != null && iter < maxIter)
        {
            iter++;
            if (!visited.Add(current.NodeId))
            {
                result.Status = "Failed: Cycle detected";
                return Task.FromResult(result);
            }

            result.ExecutionPath.Add(current.Label);

            if (current.NodeType == WorkflowNodeType.End)
            {
                result.CompletedNodes = visited.Count;
                result.Status = "Completed";
                return Task.FromResult(result);
            }

            if (current.OutgoingEdges.Count == 0)
            {
                result.Status = $"Stopped: No outgoing edges from '{current.Label}'";
                return Task.FromResult(result);
            }

            WorkflowNodeConfig? next = null;

            if (current.NodeType == WorkflowNodeType.Decision)
            {
                foreach (var edge in current.OutgoingEdges)
                {
                    if (!string.IsNullOrWhiteSpace(edge.ConditionExpression))
                    {
                        var condResult = EvaluateCondition(edge.ConditionExpression, variables);
                        if (!string.IsNullOrEmpty(edge.Label))
                            result.Decisions.Add($"{current.Label} -> {edge.Label}: {(condResult ? "Taken" : "Skipped")}");
                        if (condResult)
                        {
                            next = nodes.FirstOrDefault(n => n.NodeId == edge.ToNodeId);
                            break;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(edge.Label))
                            result.Decisions.Add($"{current.Label} -> {edge.Label}: Default");
                        next = nodes.FirstOrDefault(n => n.NodeId == edge.ToNodeId);
                    }
                }

                if (next == null)
                {
                    result.Status = $"Stopped: No matching condition on DECISION '{current.Label}'";
                    return Task.FromResult(result);
                }
            }
            else
            {
                var firstEdge = current.OutgoingEdges.First();
                next = nodes.FirstOrDefault(n => n.NodeId == firstEdge.ToNodeId);
            }

            current = next;
        }

        result.Status = iter >= maxIter ? "Failed: Max iterations" : "Completed";
        result.CompletedNodes = visited.Count;
        return Task.FromResult(result);
    }

    public Task<bool> EvaluateConditionAsync(string expression, Dictionary<string, object> variables)
    {
        var result = EvaluateCondition(expression, variables);
        return Task.FromResult(result);
    }

    public Task<Dictionary<string, object>> ExecuteApiCallAsync(WorkflowApiCallConfig config, Dictionary<string, object> variables)
    {
        throw new NotSupportedException("ExecuteApiCallAsync is not supported in the in-memory workflow engine. Use the database-backed WorkflowService.");
    }

    private static readonly ConcurrentDictionary<Guid, Timer> _timers = new();

    public Task ScheduleTimerAsync(Guid workflowInstanceId, TimeSpan delay, string action)
    {
        var timerId = Guid.NewGuid();
        var timer = new Timer(_ =>
        {
            if (_timers.TryRemove(timerId, out Timer? _))
            {
                Console.WriteLine($"[Timer Fired] Instance={workflowInstanceId}, Action={action}");
            }
        }, null, delay, Timeout.InfiniteTimeSpan);

        _timers[timerId] = timer;
        return Task.CompletedTask;
    }

    private static bool EvaluateCondition(string expression, Dictionary<string, object> context)
    {
        try
        {
            var parts = expression.Split(new[] { "==", "!=", ">", "<", ">=", "<=", " AND ", " OR " }, StringSplitOptions.None);
            if (parts.Length >= 2)
            {
                var key = parts[0].Trim().TrimStart('{').TrimEnd('}').Trim();
                var value = parts[1].Trim().Trim('\'');
                return context.TryGetValue(key, out var contextValue) && contextValue?.ToString() == value;
            }
        }
        catch { }
        return true;
    }

    private static List<WorkflowNodeConfig> GenerateSampleWorkflow(Guid workflowId)
    {
        var startId = Guid.NewGuid();
        var approvalId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var decisionId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var endId = Guid.NewGuid();

        return new List<WorkflowNodeConfig>
        {
            new() { NodeId = startId, NodeType = WorkflowNodeType.Start, Label = "Start", ConfigJson = "{}" },
            new() { NodeId = approvalId, NodeType = WorkflowNodeType.Approval, Label = "Manager Approval", ConfigJson = "{\"role\":\"POWER_USER\",\"level\":1}" },
            new() { NodeId = taskId, NodeType = WorkflowNodeType.Task, Label = "Process Document", ConfigJson = "{\"action\":\"create_doc\"}" },
            new() { NodeId = decisionId, NodeType = WorkflowNodeType.Decision, Label = "Amount > 10000?", ConfigJson = "{\"expression\":\"{amount} > 10000\"}" },
            new() { NodeId = emailId, NodeType = WorkflowNodeType.Email, Label = "Send Notification", ConfigJson = "{\"template\":\"DOCUMENT_POSTED\"}" },
            new() { NodeId = endId, NodeType = WorkflowNodeType.End, Label = "End", ConfigJson = "{}" }
        };
    }
}

internal class WorkflowInstance
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid TenantId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Guid? CurrentNodeId { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public Dictionary<string, object> Variables { get; set; } = new();
    public Guid? StartedBy { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
