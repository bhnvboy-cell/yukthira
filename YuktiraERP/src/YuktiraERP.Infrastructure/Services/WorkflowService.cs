using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class WorkflowService : IWorkflowEngine
{
    private readonly YuktiraDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorkflowService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public WorkflowService(YuktiraDbContext db, IHttpClientFactory httpClientFactory, ILogger<WorkflowService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Guid> StartWorkflowAsync(Guid workflowId, Guid tenantId, string entityName, string entityId, Guid startedBy, Dictionary<string, object>? variables = null)
    {
        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(w => w.Id == workflowId);
        if (wf == null) throw new InvalidOperationException($"Workflow {workflowId} not found.");
        if (!wf.IsActive) throw new InvalidOperationException($"Workflow {wf.Code} is not active.");

        var nodes = await GetWorkflowNodesAsync(workflowId);
        var startNode = nodes.FirstOrDefault(n => n.NodeType == WorkflowNodeType.Start);

        var instance = new WorkflowInstanceEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            TenantId = tenantId,
            EntityName = entityName,
            EntityId = entityId,
            CurrentNodeId = startNode?.NodeId,
            Status = "ACTIVE",
            Variables = JsonSerializer.Serialize(variables ?? new Dictionary<string, object>(), JsonOpts),
            StartedBy = startedBy,
            ActiveTokens = startNode != null ? JsonSerializer.Serialize(new List<Guid> { startNode.NodeId }, JsonOpts) : "[]",
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkflowInstances.Add(instance);
        await _db.SaveChangesAsync();
        return instance.Id;
    }

    public async Task ProcessNodeAsync(Guid instanceId, Guid nodeId, Dictionary<string, object>? data = null)
    {
        var instance = await _db.WorkflowInstances.FirstOrDefaultAsync(i => i.Id == instanceId);
        if (instance == null) throw new InvalidOperationException("Workflow instance not found");

        var edges = await _db.WorkflowEdges.Where(e => e.WorkflowId == instance.WorkflowId && e.FromNodeId == nodeId).OrderBy(e => e.SequenceOrder).ToListAsync();
        var currentNode = await _db.WorkflowNodes.FirstOrDefaultAsync(n => n.Id == nodeId);

        if (data != null)
        {
            var vars = JsonSerializer.Deserialize<Dictionary<string, object>>(instance.Variables, JsonOpts) ?? new();
            foreach (var kv in data) vars[kv.Key] = kv.Value;
            instance.Variables = JsonSerializer.Serialize(vars, JsonOpts);
        }

        var activeTokens = JsonSerializer.Deserialize<List<Guid>>(instance.ActiveTokens, JsonOpts) ?? new();

        if (currentNode?.NodeType == "END")
        {
            activeTokens.Remove(nodeId);
            if (activeTokens.Count == 0)
            {
                instance.CurrentNodeId = null;
                instance.Status = "COMPLETED";
                instance.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                instance.CurrentNodeId = null;
            }
            instance.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return;
        }

        var parallelOutgoing = edges.Where(e => e.BranchType == "PARALLEL").ToList();
        var isAndSplit = parallelOutgoing.Count > 1;

        if (isAndSplit)
        {
            activeTokens.Remove(nodeId);
            foreach (var edge in parallelOutgoing)
            {
                if (!activeTokens.Contains(edge.ToNodeId))
                    activeTokens.Add(edge.ToNodeId);
            }
            instance.CurrentNodeId = null;
        }
        else
        {
            var nextEdge = edges.FirstOrDefault();
            if (nextEdge != null)
            {
                var nextNode = await _db.WorkflowNodes.FirstOrDefaultAsync(n => n.Id == nextEdge.ToNodeId);
                var incomingParallelEdges = await _db.WorkflowEdges
                    .Where(e => e.WorkflowId == instance.WorkflowId && e.ToNodeId == nextEdge.ToNodeId && e.BranchType == "PARALLEL")
                    .ToListAsync();

                if (incomingParallelEdges.Count > 1)
                {
                    activeTokens.Remove(nodeId);

                    var arrivalKey = $"__join_arrival:{nextEdge.ToNodeId}";
                    var currentArrivals = 0;
                    if (instance.Variables != null)
                    {
                        var vars = JsonSerializer.Deserialize<Dictionary<string, object>>(instance.Variables, JsonOpts) ?? new();
                        if (vars.TryGetValue(arrivalKey, out var existing) && existing is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
                            currentArrivals = je.GetInt32();
                        currentArrivals++;
                        vars[arrivalKey] = currentArrivals;
                        instance.Variables = JsonSerializer.Serialize(vars, JsonOpts);
                    }

                    if (currentArrivals >= incomingParallelEdges.Count)
                    {
                        var vars2 = JsonSerializer.Deserialize<Dictionary<string, object>>(instance.Variables ?? "{}", JsonOpts) ?? new();
                        vars2.Remove(arrivalKey);
                        instance.Variables = JsonSerializer.Serialize(vars2, JsonOpts);
                        instance.CurrentNodeId = nextEdge.ToNodeId;
                    }
                    else
                    {
                        instance.CurrentNodeId = null;
                    }
                }
                else
                {
                    activeTokens.Remove(nodeId);
                    instance.CurrentNodeId = nextNode?.Id;
                }
            }
            else
            {
                activeTokens.Remove(nodeId);
                instance.CurrentNodeId = null;
            }
        }

        if (instance.CurrentNodeId == null && activeTokens.Count == 0)
        {
            instance.Status = "COMPLETED";
            instance.CompletedAt = DateTime.UtcNow;
        }

        instance.ActiveTokens = JsonSerializer.Serialize(activeTokens, JsonOpts);
        instance.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task CompleteWorkflowAsync(Guid instanceId)
    {
        var instance = await _db.WorkflowInstances.FirstOrDefaultAsync(i => i.Id == instanceId);
        if (instance != null)
        {
            instance.Status = "COMPLETED";
            instance.CompletedAt = DateTime.UtcNow;
            instance.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task TerminateWorkflowAsync(Guid instanceId)
    {
        var instance = await _db.WorkflowInstances.FirstOrDefaultAsync(i => i.Id == instanceId);
        if (instance != null)
        {
            instance.Status = "TERMINATED";
            instance.CompletedAt = DateTime.UtcNow;
            instance.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<WorkflowNodeConfig?> GetNextNodeAsync(Guid instanceId, Guid currentNodeId, Dictionary<string, object>? context = null)
    {
        var edges = await _db.WorkflowEdges.Where(e => e.WorkflowId == _db.WorkflowInstances.Where(i => i.Id == instanceId).Select(i => i.WorkflowId).FirstOrDefault() && e.FromNodeId == currentNodeId).OrderBy(e => e.SequenceOrder).ToListAsync();
        foreach (var edge in edges)
        {
            if (string.IsNullOrEmpty(edge.ConditionExpression))
            {
                var nextNode = await _db.WorkflowNodes.FirstOrDefaultAsync(n => n.Id == edge.ToNodeId);
                if (nextNode == null) continue;
                return ToNodeConfig(nextNode, edges);
            }
            if (context != null && EvaluateCondition(edge.ConditionExpression, context))
            {
                var nextNode = await _db.WorkflowNodes.FirstOrDefaultAsync(n => n.Id == edge.ToNodeId);
                if (nextNode == null) continue;
                return ToNodeConfig(nextNode, edges);
            }
        }
        return null;
    }

    public async Task<List<WorkflowNodeConfig>> GetWorkflowNodesAsync(Guid workflowId)
    {
        var nodes = await _db.WorkflowNodes.Where(n => n.WorkflowId == workflowId).OrderBy(n => n.CreatedAt).ToListAsync();
        var edges = await _db.WorkflowEdges.Where(e => e.WorkflowId == workflowId).OrderBy(e => e.SequenceOrder).ToListAsync();
        return nodes.Select(n => ToNodeConfig(n, edges)).ToList();
    }

    private static WorkflowNodeConfig ToNodeConfig(WorkflowNodeEntity node, List<WorkflowEdgeEntity> edges)
    {
        return new WorkflowNodeConfig
        {
            NodeId = node.Id,
            NodeType = Enum.TryParse<WorkflowNodeType>(node.NodeType, true, out var t) ? t : WorkflowNodeType.Task,
            Label = node.Label,
            ConfigJson = node.ConfigJson,
            OutgoingEdges = edges.Where(e => e.FromNodeId == node.Id).Select(e => new WorkflowEdgeConfig
            {
                EdgeId = e.Id,
                ToNodeId = e.ToNodeId,
                ConditionExpression = e.ConditionExpression,
                Label = e.Label,
                BranchType = e.BranchType
            }).ToList()
        };
    }

    private bool EvaluateCondition(string expression, Dictionary<string, object> context)
    {
        try
        {
            return EvaluateSimpleExpression(expression, context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate condition expression '{Expression}', defaulting to false", expression);
            return false;
        }
    }

    public async Task<WorkflowValidationResult> ValidateWorkflowDefinitionAsync(Guid workflowId)
    {
        var result = new WorkflowValidationResult();
        var nodes = await _db.WorkflowNodes.Where(n => n.WorkflowId == workflowId).ToListAsync();
        var edges = await _db.WorkflowEdges.Where(e => e.WorkflowId == workflowId).ToListAsync();

        var validTypes = new HashSet<string> { "START", "APPROVAL", "TASK", "DECISION", "EMAIL", "END", "TIMER", "API_CALL" };

        foreach (var node in nodes)
        {
            if (!validTypes.Contains(node.NodeType.ToUpperInvariant()))
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = node.Id.ToString(),
                    Field = "NodeType",
                    Message = $"Invalid node type '{node.NodeType}' on node '{node.Label}'",
                    Severity = "Error"
                });
            }
        }

        var startNodes = nodes.Where(n => n.NodeType.Equals("START", StringComparison.OrdinalIgnoreCase)).ToList();
        if (startNodes.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add(new WorkflowValidationError
            {
                NodeId = "",
                Field = "Workflow",
                Message = "Workflow must have exactly one START node, but none found",
                Severity = "Error"
            });
        }
        else if (startNodes.Count > 1)
        {
            result.IsValid = false;
            result.Errors.Add(new WorkflowValidationError
            {
                NodeId = "",
                Field = "Workflow",
                Message = "Workflow must have exactly one START node, but found multiple",
                Severity = "Error"
            });
        }

        var endNodes = nodes.Where(n => n.NodeType.Equals("END", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var endNode in endNodes)
        {
            var outgoing = edges.Where(e => e.FromNodeId == endNode.Id).ToList();
            if (outgoing.Count > 0)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = endNode.Id.ToString(),
                    Field = "OutgoingEdges",
                    Message = $"END node '{endNode.Label}' should have no outgoing edges",
                    Severity = "Error"
                });
            }
        }

        var decisionNodes = nodes.Where(n => n.NodeType.Equals("DECISION", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var decNode in decisionNodes)
        {
            var outgoing = edges.Where(e => e.FromNodeId == decNode.Id).ToList();
            if (outgoing.Count < 2)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = decNode.Id.ToString(),
                    Field = "OutgoingEdges",
                    Message = $"DECISION node '{decNode.Label}' must have at least 2 outgoing edges",
                    Severity = "Error"
                });
            }

            var emptyConditionEdges = outgoing.Where(e => string.IsNullOrWhiteSpace(e.ConditionExpression)).ToList();
            if (emptyConditionEdges.Count != 1)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = decNode.Id.ToString(),
                    Field = "ConditionExpression",
                    Message = $"DECISION node '{decNode.Label}' must have exactly one default edge (no condition) and the rest with conditions",
                    Severity = "Warning"
                });
            }

            var parallelEdges = outgoing.Where(e => e.BranchType == "PARALLEL").ToList();
            if (parallelEdges.Count > 0)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = decNode.Id.ToString(),
                    Field = "BranchType",
                    Message = $"DECISION node '{decNode.Label}' must not have PARALLEL branch type edges; use CONDITIONAL instead",
                    Severity = "Error"
                });
            }
        }

        var parallelSplitNodes = nodes.Where(n => edges.Count(e => e.FromNodeId == n.Id && e.BranchType == "PARALLEL") > 1).ToList();
        foreach (var splitNode in parallelSplitNodes)
        {
            var parallelOutgoing = edges.Where(e => e.FromNodeId == splitNode.Id && e.BranchType == "PARALLEL").ToList();
            var parallelJoinTargets = new List<Guid>();
            foreach (var edge in parallelOutgoing)
            {
                var incomingParallelCount = edges.Count(e => e.ToNodeId == edge.ToNodeId && e.BranchType == "PARALLEL");
                if (incomingParallelCount > 1)
                    parallelJoinTargets.Add(edge.ToNodeId);
            }

            if (parallelJoinTargets.Count > 0 && parallelJoinTargets.Count != parallelOutgoing.Count)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    NodeId = splitNode.Id.ToString(),
                    Field = "BranchType",
                    Message = $"AND-split node '{splitNode.Label}' must have all parallel branches converge at an AND-join node",
                    Severity = "Error"
                });
            }
        }

        return result;
    }

    public async Task<WorkflowSimulationResult> SimulateWorkflowAsync(Guid workflowId, Dictionary<string, object> variables)
    {
        var result = new WorkflowSimulationResult();
        var nodes = await _db.WorkflowNodes.Where(n => n.WorkflowId == workflowId).ToListAsync();
        var edges = await _db.WorkflowEdges.Where(e => e.WorkflowId == workflowId).ToListAsync();

        var nodeLookup = nodes.ToDictionary(n => n.Id);
        var edgeLookup = edges.GroupBy(e => e.FromNodeId).ToDictionary(g => g.Key, g => g.OrderBy(e => e.SequenceOrder).ToList());

        var startNode = nodes.FirstOrDefault(n => n.NodeType.Equals("START", StringComparison.OrdinalIgnoreCase));
        if (startNode == null)
        {
            result.Status = "Failed: No START node found";
            return result;
        }

        var visited = new HashSet<Guid>();
        var currentId = startNode.Id;
        result.TotalNodes = nodes.Count;
        var maxIterations = 100;
        var iterations = 0;

        while (currentId != Guid.Empty && iterations < maxIterations)
        {
            iterations++;
            if (!visited.Add(currentId))
            {
                result.Status = "Failed: Cycle detected";
                return result;
            }

            if (!nodeLookup.TryGetValue(currentId, out var currentNode))
            {
                result.Status = $"Failed: Node {currentId} not found";
                return result;
            }

            result.ExecutionPath.Add(currentNode.Label);

            if (currentNode.NodeType.Equals("END", StringComparison.OrdinalIgnoreCase))
            {
                result.CompletedNodes = visited.Count;
                result.Status = "Completed";
                return result;
            }

            if (!edgeLookup.TryGetValue(currentId, out var outgoingEdges) || outgoingEdges.Count == 0)
            {
                result.Status = $"Stopped: No outgoing edges from '{currentNode.Label}'";
                return result;
            }

            Guid? nextId = null;

            if (currentNode.NodeType.Equals("DECISION", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var edge in outgoingEdges)
                {
                    if (!string.IsNullOrWhiteSpace(edge.ConditionExpression))
                    {
                        var condResult = await EvaluateConditionAsync(edge.ConditionExpression, variables);
                        if (!string.IsNullOrEmpty(edge.Label))
                            result.Decisions.Add($"{currentNode.Label} -> {edge.Label}: {(condResult ? "Taken" : "Skipped")}");
                        if (condResult)
                        {
                            nextId = edge.ToNodeId;
                            break;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(edge.Label))
                            result.Decisions.Add($"{currentNode.Label} -> {edge.Label}: Default");
                        nextId = edge.ToNodeId;
                    }
                }

                if (nextId == null)
                {
                    result.Status = $"Stopped: No matching condition on DECISION '{currentNode.Label}'";
                    return result;
                }
            }
            else
            {
                nextId = outgoingEdges[0].ToNodeId;
            }

            currentId = nextId.Value;
        }

        if (iterations >= maxIterations)
            result.Status = "Failed: Maximum iterations reached";
        else
        {
            result.CompletedNodes = visited.Count;
            result.Status = "Completed";
        }

        return result;
    }

    public Task<bool> EvaluateConditionAsync(string expression, Dictionary<string, object> variables)
    {
        var result = EvaluateSimpleExpression(expression, variables);
        return Task.FromResult(result);
    }

    public async Task<Dictionary<string, object>> ExecuteApiCallAsync(WorkflowApiCallConfig config, Dictionary<string, object> variables)
    {
        using var client = _httpClientFactory.CreateClient();
        var body = config.BodyTemplate;

        body = Regex.Replace(body, @"\{\{(\w+)\}\}", m =>
        {
            var key = m.Groups[1].Value;
            return variables.TryGetValue(key, out var val) ? (val?.ToString() ?? "") : m.Value;
        });

        var url = Regex.Replace(config.Url, @"\{\{(\w+)\}\}", m =>
        {
            var key = m.Groups[1].Value;
            return variables.TryGetValue(key, out var val) ? (val?.ToString() ?? "") : m.Value;
        });

        foreach (var header in config.Headers)
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);

        HttpResponseMessage response;
        if (config.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            response = await client.GetAsync(url);
        }
        else if (config.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            response = await client.PostAsync(url, content);
        }
        else if (config.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
        {
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            response = await client.PutAsync(url, content);
        }
        else
        {
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(config.Method), url) { Content = content });
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = new Dictionary<string, object>
        {
            ["StatusCode"] = (int)response.StatusCode,
            ["IsSuccess"] = response.IsSuccessStatusCode,
            ["Body"] = responseBody
        };

        if (!string.IsNullOrEmpty(config.OutputVariable) && response.IsSuccessStatusCode)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody, JsonOpts);
                if (parsed != null)
                    result[config.OutputVariable] = parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse API call response body as JSON");
            }
        }

        return result;
    }

    private static readonly ConcurrentDictionary<Guid, Timer> _timers = new();

    public async Task ScheduleTimerAsync(Guid workflowInstanceId, TimeSpan delay, string action)
    {
        var instance = await _db.WorkflowInstances.FirstOrDefaultAsync(i => i.Id == workflowInstanceId);
        if (instance == null) return;

        var timerId = Guid.NewGuid();
        var timer = new Timer(async _ =>
        {
            if (_timers.TryRemove(timerId, out Timer? _))
            {
                try
                {
                    var vars = JsonSerializer.Deserialize<Dictionary<string, object>>(instance.Variables, JsonOpts) ?? new();
                    if (action == "ESCALATE" && Guid.TryParse(vars.GetValueOrDefault("approverId")?.ToString(), out var approverId))
                    {
                        instance.CurrentNodeId = null;
                        instance.Status = "ESCALATED";
                        instance.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                    else if (action == "RETRY")
                    {
                        instance.Status = "ACTIVE";
                        instance.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Timer Error] Instance={workflowInstanceId}: {ex.Message}");
                }
            }
        }, null, delay, Timeout.InfiniteTimeSpan);

        _timers[timerId] = timer;
    }

    private static bool EvaluateSimpleExpression(string expression, Dictionary<string, object> variables)
    {
        try
        {
            var substituted = Regex.Replace(expression, @"\{(\w+)\}", m =>
            {
                var key = m.Groups[1].Value;
                if (variables.TryGetValue(key, out var val))
                {
                    if (val is string s) return $"\"{s}\"";
                    return val?.ToString() ?? "null";
                }
                return m.Value;
            });

            return EvaluateBool(substituted);
        }
        catch
        {
            return false;
        }
    }

    private static bool EvaluateBool(string expr)
    {
        expr = expr.Trim();
        while (expr.StartsWith('(') && expr.EndsWith(')') && BalancedParens(expr))
            expr = expr.Substring(1, expr.Length - 2).Trim();

        var orIdx = FindTopLevelOperator(expr, "||");
        if (orIdx >= 0)
        {
            var left = expr.Substring(0, orIdx).Trim();
            var right = expr.Substring(orIdx + 2).Trim();
            return EvaluateBool(left) || EvaluateBool(right);
        }

        var andIdx = FindTopLevelOperator(expr, "&&");
        if (andIdx >= 0)
        {
            var left = expr.Substring(0, andIdx).Trim();
            var right = expr.Substring(andIdx + 2).Trim();
            return EvaluateBool(left) && EvaluateBool(right);
        }

        return EvaluateComparison(expr);
    }

    private static bool EvaluateComparison(string expr)
    {
        expr = expr.Trim();

        if (expr.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (expr.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;

        string op;
        int idx;

        idx = expr.IndexOf("!=", StringComparison.Ordinal);
        if (idx >= 0) { op = "!="; }
        else
        {
            idx = expr.IndexOf("==", StringComparison.Ordinal);
            if (idx >= 0) { op = "=="; }
            else
            {
                idx = expr.IndexOf(">=", StringComparison.Ordinal);
                if (idx >= 0) { op = ">="; }
                else
                {
                    idx = expr.IndexOf("<=", StringComparison.Ordinal);
                    if (idx >= 0) { op = "<="; }
                    else
                    {
                        idx = expr.IndexOf('>');
                        if (idx >= 0) { op = ">"; }
                        else
                        {
                            idx = expr.IndexOf('<');
                            if (idx >= 0) { op = "<"; }
                            else return false;
                        }
                    }
                }
            }
        }

        var leftStr = expr.Substring(0, idx).Trim().Trim('"');
        var rightStr = expr.Substring(idx + op.Length).Trim().Trim('"');

        if (double.TryParse(leftStr, out var leftNum) && double.TryParse(rightStr, out var rightNum))
        {
            return op switch
            {
                "==" => leftNum == rightNum,
                "!=" => leftNum != rightNum,
                ">" => leftNum > rightNum,
                "<" => leftNum < rightNum,
                ">=" => leftNum >= rightNum,
                "<=" => leftNum <= rightNum,
                _ => false
            };
        }

        return op switch
        {
            "==" => leftStr == rightStr,
            "!=" => leftStr != rightStr,
            ">" => string.Compare(leftStr, rightStr, StringComparison.Ordinal) > 0,
            "<" => string.Compare(leftStr, rightStr, StringComparison.Ordinal) < 0,
            ">=" => string.Compare(leftStr, rightStr, StringComparison.Ordinal) >= 0,
            "<=" => string.Compare(leftStr, rightStr, StringComparison.Ordinal) <= 0,
            _ => false
        };
    }

    private static int FindTopLevelOperator(string expr, string op)
    {
        var depth = 0;
        for (var i = 0; i <= expr.Length - op.Length; i++)
        {
            if (expr[i] == '(') depth++;
            else if (expr[i] == ')') depth--;
            else if (depth == 0 && i + op.Length <= expr.Length && expr.Substring(i, op.Length) == op)
                return i;
        }
        return -1;
    }

    private static bool BalancedParens(string expr)
    {
        var depth = 0;
        for (var i = 0; i < expr.Length; i++)
        {
            if (expr[i] == '(') depth++;
            else if (expr[i] == ')') depth--;
            if (depth < 0) return false;
        }
        return depth == 0;
    }
}
