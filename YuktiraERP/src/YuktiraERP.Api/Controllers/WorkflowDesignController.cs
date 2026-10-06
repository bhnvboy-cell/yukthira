using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/workflow-design")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class WorkflowDesignController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly IWorkflowEngine _engine;
    private readonly IWorkflowThresholdService _thresholds;
    private readonly IReleaseStrategyService _releaseStrategies;
    private readonly IAuditService _audit;
    private readonly ITenantContext _tenant;

    public WorkflowDesignController(
        YuktiraDbContext db,
        IWorkflowEngine engine,
        IWorkflowThresholdService thresholds,
        IReleaseStrategyService releaseStrategies,
        IAuditService audit,
        ITenantContext tenant)
    {
        _db = db;
        _engine = engine;
        _thresholds = thresholds;
        _releaseStrategies = releaseStrategies;
        _audit = audit;
        _tenant = tenant;
    }

    [HttpGet("definitions")]
    public async Task<IActionResult> GetDefinitions()
    {
        var tenantId = _tenant.TenantId;
        var definitions = await _db.WorkflowDefinitions.AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .ToListAsync();
        var ids = definitions.Select(d => d.Id).ToList();
        var nodeCounts = ids.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.WorkflowNodes.AsNoTracking().Where(n => ids.Contains(n.WorkflowId))
                .GroupBy(n => n.WorkflowId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

        var rows = definitions.Select(d => new
        {
            d.Id,
            d.Code,
            d.Name,
            d.Module,
            d.Description,
            d.IsActive,
            d.Version,
            d.CreatedAt,
            d.UpdatedAt,
            NodeCount = nodeCounts.TryGetValue(d.Id, out var count) ? count : 0
        }).ToList();

        return Ok(new { definitions = rows, totalCount = rows.Count, tenantId });
    }

    [HttpGet("definitions/{id:guid}")]
    public async Task<IActionResult> GetDefinition(Guid id)
    {
        var definition = await _db.WorkflowDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == _tenant.TenantId);
        if (definition == null) return NotFound(new { error = "Workflow definition not found" });

        var nodes = await _db.WorkflowNodes.AsNoTracking()
            .Where(n => n.WorkflowId == id)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new { n.Id, n.NodeType, n.Label, n.Description, n.ConfigJson, n.PositionX, n.PositionY })
            .ToListAsync();
        var edges = await _db.WorkflowEdges.AsNoTracking()
            .Where(e => e.WorkflowId == id)
            .OrderBy(e => e.SequenceOrder)
            .Select(e => new { e.Id, e.FromNodeId, e.ToNodeId, e.ConditionExpression, e.Label, e.SequenceOrder, e.BranchType })
            .ToListAsync();

        return Ok(new
        {
            definition = new
            {
                definition.Id,
                definition.Code,
                definition.Name,
                definition.Module,
                definition.Description,
                definition.IsActive,
                definition.Version,
                definition.BpmnXml,
                definition.CreatedAt,
                definition.UpdatedAt
            },
            nodes,
            edges
        });
    }

    [HttpPost("definitions")]
    public async Task<IActionResult> SaveDefinition([FromBody] WorkflowDefinitionSaveRequest request)
    {
        var tenantId = _tenant.TenantId;
        if (string.IsNullOrWhiteSpace(request.Code) && string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Workflow code or name is required" });

        var resolvedNodes = new List<(WorkflowDesignNodeRequest Request, Guid Id)>();
        foreach (var node in request.Nodes ?? new List<WorkflowDesignNodeRequest>())
        {
            var nodeId = node.Id == Guid.Empty ? Guid.NewGuid() : node.Id;
            resolvedNodes.Add((node, nodeId));
        }

        var nodeIds = resolvedNodes.Select(r => r.Id).ToHashSet();
        foreach (var edge in request.Edges ?? new List<WorkflowDesignEdgeRequest>())
        {
            if (!nodeIds.Contains(edge.FromNodeId) || !nodeIds.Contains(edge.ToNodeId))
                return BadRequest(new { error = "Every sequence flow must connect two nodes that exist in this workflow" });
        }

        WorkflowDefinitionEntity definition;
        if (request.Id.HasValue)
        {
            definition = await _db.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Id == request.Id.Value && d.TenantId == tenantId);
            if (definition == null) return NotFound(new { error = "Workflow definition not found" });
        }
        else
        {
            definition = new WorkflowDefinitionEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CreatedBy = GetUserId(),
                IsActive = false,
                Version = 1
            };
            _db.WorkflowDefinitions.Add(definition);
        }

        definition.Code = string.IsNullOrWhiteSpace(request.Code) ? definition.Code : request.Code.Trim();
        definition.Name = string.IsNullOrWhiteSpace(request.Name) ? definition.Name : request.Name.Trim();
        definition.Module = request.Module ?? definition.Module;
        definition.Description = request.Description ?? definition.Description;
        if (!string.IsNullOrWhiteSpace(request.BpmnXml)) definition.BpmnXml = request.BpmnXml;
        if (request.IsActive.HasValue) definition.IsActive = request.IsActive.Value;
        definition.UpdatedAt = DateTime.UtcNow;

        var existingNodes = await _db.WorkflowNodes.Where(n => n.WorkflowId == definition.Id).ToListAsync();
        var staleNodes = existingNodes.Where(n => !nodeIds.Contains(n.Id)).ToList();
        if (staleNodes.Count > 0) _db.WorkflowNodes.RemoveRange(staleNodes);

        foreach (var (nodeRequest, nodeId) in resolvedNodes)
        {
            var node = existingNodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null)
            {
                node = new WorkflowNodeEntity { Id = nodeId, WorkflowId = definition.Id, CreatedAt = DateTime.UtcNow };
                _db.WorkflowNodes.Add(node);
            }
            node.NodeType = NormalizeNodeType(nodeRequest.Type);
            node.Label = string.IsNullOrWhiteSpace(nodeRequest.Label) ? nodeRequest.Type : nodeRequest.Label.Trim();
            node.Description = nodeRequest.Description ?? string.Empty;
            node.ConfigJson = string.IsNullOrWhiteSpace(nodeRequest.ConfigJson) ? "{}" : nodeRequest.ConfigJson;
            node.PositionX = nodeRequest.PositionX;
            node.PositionY = nodeRequest.PositionY;
            node.UpdatedAt = DateTime.UtcNow;
        }

        var resolvedEdges = new List<(WorkflowDesignEdgeRequest Request, Guid Id)>();
        foreach (var edge in request.Edges ?? new List<WorkflowDesignEdgeRequest>())
            resolvedEdges.Add((edge, edge.Id == Guid.Empty ? Guid.NewGuid() : edge.Id));

        var edgeIds = resolvedEdges.Select(e => e.Id).ToHashSet();
        var existingEdges = await _db.WorkflowEdges.Where(e => e.WorkflowId == definition.Id).ToListAsync();
        var staleEdges = existingEdges.Where(e => !edgeIds.Contains(e.Id)).ToList();
        if (staleEdges.Count > 0) _db.WorkflowEdges.RemoveRange(staleEdges);

        var order = 0;
        foreach (var (edgeRequest, edgeId) in resolvedEdges)
        {
            var edge = existingEdges.FirstOrDefault(e => e.Id == edgeId);
            if (edge == null)
            {
                edge = new WorkflowEdgeEntity { Id = edgeId, WorkflowId = definition.Id, CreatedAt = DateTime.UtcNow };
                _db.WorkflowEdges.Add(edge);
            }
            edge.FromNodeId = edgeRequest.FromNodeId;
            edge.ToNodeId = edgeRequest.ToNodeId;
            edge.ConditionExpression = edgeRequest.ConditionExpression ?? string.Empty;
            edge.Label = edgeRequest.Label ?? string.Empty;
            edge.BranchType = string.IsNullOrWhiteSpace(edgeRequest.BranchType) ? "SEQUENTIAL" : edgeRequest.BranchType.Trim().ToUpperInvariant();
            edge.SequenceOrder = edgeRequest.SequenceOrder != 0 ? edgeRequest.SequenceOrder : order;
            edge.UpdatedAt = DateTime.UtcNow;
            order++;
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = tenantId,
            ModuleName = "Workflow",
            ActionType = ActionType.Update,
            EntityName = "WorkflowDefinition",
            EntityId = definition.Id.ToString(),
            NewValue = definition.Code,
            Details = $"Workflow '{definition.Name}' saved with {resolvedNodes.Count} nodes and {resolvedEdges.Count} sequence flows"
        });

        return Ok(new
        {
            id = definition.Id,
            code = definition.Code,
            name = definition.Name,
            version = definition.Version,
            isActive = definition.IsActive,
            nodeCount = resolvedNodes.Count,
            edgeCount = resolvedEdges.Count,
            saved = true
        });
    }

    [HttpPost("definitions/{id:guid}/validate")]
    public async Task<IActionResult> ValidateDefinition(Guid id)
    {
        var exists = await _db.WorkflowDefinitions.AsNoTracking().AnyAsync(d => d.Id == id && d.TenantId == _tenant.TenantId);
        if (!exists) return NotFound(new { error = "Workflow definition not found" });

        var validation = await _engine.ValidateWorkflowDefinitionAsync(id);
        return Ok(new { isValid = validation.IsValid, errors = validation.Errors });
    }

    [HttpPost("definitions/{id:guid}/publish")]
    public async Task<IActionResult> PublishDefinition(Guid id)
    {
        var tenantId = _tenant.TenantId;
        var definition = await _db.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
        if (definition == null) return NotFound(new { error = "Workflow definition not found" });

        var validation = await _engine.ValidateWorkflowDefinitionAsync(id);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                published = false,
                isValid = false,
                message = "Workflow validation failed",
                errors = validation.Errors
            });
        }

        definition.IsActive = true;
        definition.Version = definition.Version + 1;
        definition.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = tenantId,
            ModuleName = "Workflow",
            ActionType = ActionType.Workflow,
            EntityName = "WorkflowDefinition",
            EntityId = definition.Id.ToString(),
            NewValue = definition.Code,
            Details = $"Workflow '{definition.Name}' published as version {definition.Version}"
        });

        return Ok(new { published = true, isValid = true, version = definition.Version, errors = validation.Errors });
    }

    [HttpDelete("definitions/{id:guid}")]
    public async Task<IActionResult> DeleteDefinition(Guid id)
    {
        var tenantId = _tenant.TenantId;
        var definition = await _db.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
        if (definition == null) return NotFound(new { error = "Workflow definition not found" });

        var runningInstances = await _db.WorkflowInstances.AsNoTracking()
            .CountAsync(i => i.WorkflowId == id && i.Status == "ACTIVE");
        if (runningInstances > 0)
        {
            return Conflict(new
            {
                error = $"Cannot delete a workflow with {runningInstances} running instance(s)",
                runningInstances
            });
        }

        var edges = await _db.WorkflowEdges.Where(e => e.WorkflowId == id).ToListAsync();
        var nodes = await _db.WorkflowNodes.Where(n => n.WorkflowId == id).ToListAsync();
        if (edges.Count > 0) _db.WorkflowEdges.RemoveRange(edges);
        if (nodes.Count > 0) _db.WorkflowNodes.RemoveRange(nodes);
        _db.WorkflowDefinitions.Remove(definition);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = tenantId,
            ModuleName = "Workflow",
            ActionType = ActionType.Delete,
            EntityName = "WorkflowDefinition",
            EntityId = definition.Id.ToString(),
            OldValue = definition.Code,
            Details = $"Workflow '{definition.Name}' deleted"
        });

        return Ok(new { deleted = true, id });
    }

    [HttpGet("thresholds")]
    public async Task<IActionResult> GetThresholds([FromQuery] string? documentType)
    {
        var thresholds = await _thresholds.ListAsync(_tenant.TenantId, documentType);
        return Ok(new { thresholds, totalCount = thresholds.Count, tenantId = _tenant.TenantId });
    }

    [HttpPost("thresholds")]
    public async Task<IActionResult> CreateThreshold([FromBody] WorkflowThresholdCreateRequest request)
    {
        var result = await _thresholds.CreateAsync(_tenant.TenantId, request);
        if (!result.Success) return BadRequest(new { error = result.Error });

        await LogThresholdAudit(ActionType.Create, result.Threshold!, "created");
        return Ok(new { saved = true, threshold = result.Threshold });
    }

    [HttpPut("thresholds/{id:guid}")]
    public async Task<IActionResult> UpdateThreshold(Guid id, [FromBody] WorkflowThresholdUpdateRequest request)
    {
        var result = await _thresholds.UpdateAsync(_tenant.TenantId, id, request);
        if (!result.Success) return BadRequest(new { error = result.Error });

        await LogThresholdAudit(ActionType.Update, result.Threshold!, "updated");
        return Ok(new { saved = true, threshold = result.Threshold });
    }

    [HttpDelete("thresholds/{id:guid}")]
    public async Task<IActionResult> DeleteThreshold(Guid id)
    {
        var deleted = await _thresholds.DeleteAsync(_tenant.TenantId, id);
        if (!deleted) return NotFound(new { error = "Approval threshold not found" });

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = _tenant.TenantId,
            ModuleName = "Workflow",
            ActionType = ActionType.Delete,
            EntityName = "ReleaseStrategy",
            EntityId = id.ToString(),
            Details = $"Approval threshold {id} deleted"
        });

        return Ok(new { deleted = true, id });
    }

    [HttpGet("thresholds/preview")]
    public async Task<IActionResult> PreviewThreshold([FromQuery] string documentType, [FromQuery] decimal amount, [FromQuery] string? plant, [FromQuery] string? departmentKey)
    {
        if (string.IsNullOrWhiteSpace(documentType))
            return BadRequest(new { error = "documentType is required" });

        var tenantId = _tenant.TenantId;
        var match = await _releaseStrategies.FindMatchingStrategyAsync(documentType, amount, plant ?? string.Empty, departmentKey ?? string.Empty);
        if (match != null && match.TenantId != tenantId)
            match = null;
        if (match == null)
        {
            match = await _db.ReleaseStrategies.AsNoTracking().FirstOrDefaultAsync(s =>
                s.TenantId == tenantId &&
                s.IsActive &&
                s.DocumentType == documentType &&
                amount >= s.MinAmount &&
                amount <= s.MaxAmount &&
                (string.IsNullOrEmpty(s.Plant) || s.Plant == plant) &&
                (string.IsNullOrEmpty(s.DepartmentKey) || s.DepartmentKey == departmentKey));
        }

        if (match == null)
            return Ok(new { matched = false, documentType, amount, threshold = (WorkflowThresholdDto?)null });

        var codes = await _db.ReleaseCodes.AsNoTracking()
            .Where(c => c.ReleaseStrategyId == match.Id)
            .OrderBy(c => c.Level)
            .ToListAsync();

        var threshold = new WorkflowThresholdDto
        {
            Id = match.Id,
            Code = match.Code,
            Name = match.Name,
            Description = match.Description,
            DocumentType = match.DocumentType,
            MinAmount = match.MinAmount,
            MaxAmount = match.MaxAmount,
            Plant = match.Plant,
            DepartmentKey = match.DepartmentKey,
            IsActive = match.IsActive,
            Levels = codes.Select(c => new WorkflowThresholdLevelDto { Level = c.Level, ApproverRole = c.ApproverRole }).ToList(),
            CreatedAt = match.CreatedAt,
            UpdatedAt = match.UpdatedAt
        };

        return Ok(new { matched = true, documentType, amount, threshold });
    }

    private async Task LogThresholdAudit(ActionType actionType, WorkflowThresholdDto threshold, string verb)
    {
        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = _tenant.TenantId,
            ModuleName = "Workflow",
            ActionType = actionType,
            EntityName = "ReleaseStrategy",
            EntityId = threshold.Id.ToString(),
            NewValue = $"{threshold.DocumentType} {threshold.MinAmount} - {threshold.MaxAmount}",
            Details = $"Approval threshold {threshold.Code} {verb} ({threshold.DocumentType} {threshold.MinAmount} - {threshold.MaxAmount})"
        });
    }

    private Guid GetUserId() =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    private static string NormalizeNodeType(string? type)
    {
        var value = (type ?? string.Empty).Trim().ToUpperInvariant().Replace('-', '_');
        return value switch
        {
            "" => "TASK",
            "START" or "STARTEVENT" => "START",
            "APPROVAL" or "MANUAL" or "MANUALTASK" => "APPROVAL",
            "DECISION" or "EXCLUSIVE" or "EXCLUSIVEGATEWAY" or "EXCLUSIVE_GATEWAY" => "DECISION",
            "PARALLEL" or "PARALLELGATEWAY" or "PARALLEL_GATEWAY" => "TASK",
            "END" or "ENDEVENT" => "END",
            "TIMER" or "TIMEREVENT" => "TIMER",
            "API" or "APICALL" or "API_CALL" => "API_CALL",
            "EMAIL" => "EMAIL",
            "TASK" => "TASK",
            _ => value
        };
    }
}

public class WorkflowDefinitionSaveRequest
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
    public string BpmnXml { get; set; } = string.Empty;
    public List<WorkflowDesignNodeRequest>? Nodes { get; set; }
    public List<WorkflowDesignEdgeRequest>? Edges { get; set; }
}

public class WorkflowDesignNodeRequest
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "TASK";
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}";
    public double PositionX { get; set; }
    public double PositionY { get; set; }
}

public class WorkflowDesignEdgeRequest
{
    public Guid Id { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string ConditionExpression { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public string BranchType { get; set; } = "SEQUENTIAL";
}
