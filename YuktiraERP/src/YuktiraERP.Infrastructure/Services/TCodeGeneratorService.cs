using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class TCodeGeneratorService : ITCodeGeneratorService
{
    private readonly YuktiraDbContext _db;
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public TCodeGeneratorService(YuktiraDbContext db) => _db = db;

    public async Task<List<TCodeDefinitionDto>> GetAllDefinitionsAsync(Guid tenantId)
    {
        var defs = await _db.TCodeDefinitions
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Module).ThenBy(d => d.Code)
            .ToListAsync();
        var result = new List<TCodeDefinitionDto>();
        foreach (var d in defs)
        {
            var fields = await _db.TCodeFields.Where(f => f.TCodeId == d.Id).OrderBy(f => f.OrderIndex).ToListAsync();
            result.Add(MapDefinition(d, fields));
        }
        return result;
    }

    public async Task<TCodeDefinitionDto?> GetDefinitionAsync(Guid tenantId, string code)
    {
        var d = await _db.TCodeDefinitions.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code);
        if (d == null) return null;
        var fields = await _db.TCodeFields.Where(f => f.TCodeId == d.Id).OrderBy(f => f.OrderIndex).ToListAsync();
        return MapDefinition(d, fields);
    }

    public async Task<TCodeDefinitionDto> CreateDefinitionAsync(Guid tenantId, string code, string name, string module, string description, Guid userId)
    {
        var def = new TCodeDefinitionEntity
        {
            TenantId = tenantId,
            Code = code.ToUpper().Replace(" ", "_"),
            Name = name,
            Module = module,
            Description = description,
            HasWorkflow = true,
            HasNumberRange = true,
            Prefix = code.Split('-').LastOrDefault()?.Substring(0, Math.Min(3, code.Split('-').Last().Length)).ToUpper() ?? code[..3].ToUpper(),
            Status = "Active",
            CreatedBy = userId.ToString()
        };
        _db.TCodeDefinitions.Add(def);
        await _db.SaveChangesAsync();

        var defaultFields = new List<(string name, string label, string type, bool req, int order)>
        {
            ("document_number", "Document Number", "TEXT", true, 1),
            ("document_date", "Date", "DATE", true, 2),
            ("reference", "Reference", "TEXT", false, 3),
            ("amount", "Amount", "DECIMAL", false, 4),
            ("remarks", "Remarks", "TEXTAREA", false, 5)
        };
        foreach (var (fn, fl, dt, rq, ord) in defaultFields)
        {
            _db.TCodeFields.Add(new TCodeFieldEntity
            {
                TenantId = tenantId,
                TCodeId = def.Id,
                FieldName = fn,
                FieldLabel = fl,
                DataType = dt,
                IsRequired = rq,
                IsVisible = true,
                IsSystem = true,
                OrderIndex = ord
            });
        }
        await _db.SaveChangesAsync();

        await AutoGeneratePermissionsAsync(tenantId, def.Id, def.Code);

        return (await GetDefinitionAsync(tenantId, def.Code))!;
    }

    public async Task<bool> DeleteDefinitionAsync(Guid tenantId, Guid id)
    {
        var def = await _db.TCodeDefinitions.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
        if (def == null) return false;
        var fields = await _db.TCodeFields.Where(f => f.TCodeId == id).ToListAsync();
        _db.TCodeFields.RemoveRange(fields);
        _db.TCodeDefinitions.Remove(def);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<TCodeFieldDto>> GetFieldsAsync(Guid tenantId, Guid tcodeId)
    {
        return await _db.TCodeFields
            .Where(f => f.TenantId == tenantId && f.TCodeId == tcodeId)
            .OrderBy(f => f.OrderIndex)
            .Select(f => new TCodeFieldDto
            {
                Id = f.Id,
                FieldName = f.FieldName,
                FieldLabel = f.FieldLabel,
                DataType = f.DataType,
                IsRequired = f.IsRequired,
                IsVisible = f.IsVisible,
                DefaultValue = f.DefaultValue,
                ValidationRuleJson = f.ValidationRuleJson,
                ConditionalVisibilityJson = f.ConditionalVisibilityJson,
                OrderIndex = f.OrderIndex,
                Width = f.Width,
                SectionName = f.SectionName
            }).ToListAsync();
    }

    public async Task<TCodeFieldDto> AddFieldAsync(Guid tenantId, Guid tcodeId, TCodeFieldDto field)
    {
        var maxOrder = await _db.TCodeFields.Where(f => f.TCodeId == tcodeId).MaxAsync(f => (int?)f.OrderIndex) ?? 0;
        var entity = new TCodeFieldEntity
        {
            TenantId = tenantId,
            TCodeId = tcodeId,
            FieldName = field.FieldName.ToLower().Replace(" ", "_"),
            FieldLabel = field.FieldLabel,
            DataType = field.DataType,
            IsRequired = field.IsRequired,
            IsVisible = field.IsVisible,
            DefaultValue = field.DefaultValue,
            ValidationRuleJson = field.ValidationRuleJson,
            ConditionalVisibilityJson = field.ConditionalVisibilityJson,
            OrderIndex = maxOrder + 1,
            Width = field.Width,
            SectionName = field.SectionName
        };
        _db.TCodeFields.Add(entity);
        await _db.SaveChangesAsync();
        field.Id = entity.Id;
        field.OrderIndex = entity.OrderIndex;
        return field;
    }

    public async Task<bool> RemoveFieldAsync(Guid tenantId, Guid fieldId)
    {
        var field = await _db.TCodeFields.FirstOrDefaultAsync(f => f.Id == fieldId && f.TenantId == tenantId);
        if (field == null || field.IsSystem) return false;
        _db.TCodeFields.Remove(field);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<TCodeDataDto>> GetRecordsAsync(Guid tenantId, Guid tcodeId, string? search = null, int page = 1, int pageSize = 50)
    {
        var query = _db.TCodeData.Where(r => r.TenantId == tenantId && r.TCodeId == tcodeId);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(r => r.RecordId.Contains(search) || r.DataJson.Contains(search));
        var records = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return records.Select(r => MapData(r)).ToList();
    }

    public async Task<TCodeDataDto> CreateRecordAsync(Guid tenantId, Guid tcodeId, Dictionary<string, object> data, Guid userId)
    {
        var def = await _db.TCodeDefinitions.FirstOrDefaultAsync(d => d.Id == tcodeId && d.TenantId == tenantId)
            ?? throw new InvalidOperationException("T-Code not found");

        var recordId = def.HasNumberRange
            ? $"{def.Prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}"
            : Guid.NewGuid().ToString()[..8].ToUpper();

        data["document_number"] = recordId;
        data["document_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var entity = new TCodeDataEntity
        {
            TenantId = tenantId,
            TCodeId = tcodeId,
            RecordId = recordId,
            DataJson = JsonSerializer.Serialize(data),
            Status = "Draft",
            WorkflowNode = def.HasWorkflow ? "START" : "COMPLETED",
            CreatedBy = userId,
            UpdatedBy = userId
        };
        _db.TCodeData.Add(entity);
        await _db.SaveChangesAsync();
        return MapData(entity);
    }

    public async Task<TCodeDataDto> UpdateRecordAsync(Guid tenantId, Guid recordId, Dictionary<string, object> data, Guid userId)
    {
        var entity = await _db.TCodeData.FirstOrDefaultAsync(r => r.Id == recordId && r.TenantId == tenantId)
            ?? throw new InvalidOperationException("Record not found");
        entity.DataJson = JsonSerializer.Serialize(data);
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapData(entity);
    }

    public async Task<bool> DeleteRecordAsync(Guid tenantId, Guid recordId)
    {
        var entity = await _db.TCodeData.FirstOrDefaultAsync(r => r.Id == recordId && r.TenantId == tenantId);
        if (entity == null) return false;
        _db.TCodeData.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TCodeDataDto?> GetRecordAsync(Guid tenantId, Guid recordId)
    {
        var entity = await _db.TCodeData.FirstOrDefaultAsync(r => r.Id == recordId && r.TenantId == tenantId);
        return entity == null ? null : MapData(entity);
    }

    public async Task AutoGeneratePermissionsAsync(Guid tenantId, Guid tcodeId, string code)
    {
        var tcode = code.ToUpper();
        var perms = new[] { "CREATE", "EDIT", "DELETE", "VIEW" };
        int order = 0;
        foreach (var perm in perms)
        {
            if (!await _db.TransactionCodes.AnyAsync(t => t.Code == $"{tcode}_{perm}"))
            {
                _db.TransactionCodes.Add(new TransactionCodeEntity
                {
                    Code = $"{tcode}_{perm}",
                    Name = $"{tcode} {perm}",
                    Description = $"{perm} permission for {tcode}",
                    Module = "TCODE",
                    GroupName = tcode,
                    Route = $"/tcode/{tcode}/{perm.ToLower()}",
                    Icon = perm switch { "CREATE" => "bi-plus-circle", "EDIT" => "bi-pencil", "DELETE" => "bi-trash", _ => "bi-eye" },
                    SortOrder = order++,
                    Status = "Active",
                    IsSystem = true,
                    RequiredRole = perm == "DELETE" ? "ADMIN" : "USER"
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    private static readonly Dictionary<string, List<(string to, string label, string? role)>> _workflowGraph = new()
    {
        ["START"] = new() { ("DATA_ENTRY", "Begin Data Entry", null) },
        ["DATA_ENTRY"] = new() { ("APPROVAL", "Submit for Approval", null), ("START", "Revise", null) },
        ["APPROVAL"] = new() { ("POSTING", "Approve & Post", "ADMIN"), ("DATA_ENTRY", "Send Back", "ADMIN") },
        ["POSTING"] = new() { ("END", "Complete", null) },
        ["END"] = new() { ("DATA_ENTRY", "Reopen", "ADMIN") }
    };

    public async Task<TCodeDataDto> AdvanceWorkflowAsync(Guid tenantId, Guid recordId, string targetNode, Guid userId)
    {
        var entity = await _db.TCodeData.FirstOrDefaultAsync(r => r.Id == recordId && r.TenantId == tenantId)
            ?? throw new InvalidOperationException("Record not found");

        var current = entity.WorkflowNode;
        if (string.IsNullOrEmpty(current)) current = "START";

        var valid = _workflowGraph.ContainsKey(current) && _workflowGraph[current].Any(t => t.to == targetNode);
        if (!valid) throw new InvalidOperationException($"Invalid transition from {current} to {targetNode}");

        entity.WorkflowNode = targetNode;
        entity.Status = targetNode switch
        {
            "END" => "Completed",
            "START" => "Draft",
            "DATA_ENTRY" => "In Progress",
            "APPROVAL" => "Pending Approval",
            "POSTING" => "Pending Posting",
            _ => "Draft"
        };
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapData(entity);
    }

    public async Task<TCodeDataDto> RejectWorkflowAsync(Guid tenantId, Guid recordId, Guid userId)
    {
        return await AdvanceWorkflowAsync(tenantId, recordId, "DATA_ENTRY", userId);
    }

    public async Task<List<string>> GetAvailableTransitionsAsync(Guid tenantId, Guid recordId)
    {
        var entity = await _db.TCodeData.FirstOrDefaultAsync(r => r.Id == recordId && r.TenantId == tenantId)
            ?? throw new InvalidOperationException("Record not found");
        var current = entity.WorkflowNode;
        if (string.IsNullOrEmpty(current)) current = "START";
        if (!_workflowGraph.ContainsKey(current)) return new();
        return _workflowGraph[current].Select(t => t.to).ToList();
    }

    public TCodeWorkflowDto GetWorkflowDefinition()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Name = "START", Label = "Start", Color = "secondary", Icon = "bi-play-circle" },
            new() { Name = "DATA_ENTRY", Label = "Data Entry", Color = "info", Icon = "bi-pencil-square" },
            new() { Name = "APPROVAL", Label = "Approval", Color = "warning", Icon = "bi-check2-circle" },
            new() { Name = "POSTING", Label = "Posting", Color = "primary", Icon = "bi-cloud-upload" },
            new() { Name = "END", Label = "Completed", Color = "success", Icon = "bi-check-circle" }
        };
        var transitions = new List<WorkflowTransitionDto>();
        foreach (var (from, tos) in _workflowGraph)
        {
            foreach (var (to, label, role) in tos)
                transitions.Add(new() { From = from, To = to, Label = label, RequiredRole = role });
        }
        return new() { Nodes = nodes, AvailableTransitions = transitions };
    }

    public async Task<TCodeWorkflowDto> GetRecordWorkflowAsync(Guid tenantId, Guid recordId)
    {
        var entity = await _db.TCodeData.FirstOrDefaultAsync(r => r.Id == recordId && r.TenantId == tenantId)
            ?? throw new InvalidOperationException("Record not found");
        var current = string.IsNullOrEmpty(entity.WorkflowNode) ? "START" : entity.WorkflowNode;
        var wf = GetWorkflowDefinition();
        wf.CurrentNode = current;
        wf.Status = entity.Status;
        wf.CanEdit = current is "START" or "DATA_ENTRY";
        wf.CanSubmit = current is "DATA_ENTRY";
        var transitions = _workflowGraph.ContainsKey(current) ? _workflowGraph[current] : new();
        wf.AvailableTransitions = transitions.Select(t => new WorkflowTransitionDto
        {
            From = current, To = t.to, Label = t.label, RequiredRole = t.role
        }).ToList();
        return wf;
    }

    private static TCodeDefinitionDto MapDefinition(TCodeDefinitionEntity d, List<TCodeFieldEntity> fields) => new()
    {
        Id = d.Id,
        Code = d.Code,
        Name = d.Name,
        Module = d.Module,
        Description = d.Description,
        HasWorkflow = d.HasWorkflow,
        HasNumberRange = d.HasNumberRange,
        Prefix = d.Prefix,
        Status = d.Status,
        Fields = fields.Select(f => new TCodeFieldDto
        {
            Id = f.Id,
            FieldName = f.FieldName,
            FieldLabel = f.FieldLabel,
            DataType = f.DataType,
            IsRequired = f.IsRequired,
            IsVisible = f.IsVisible,
            DefaultValue = f.DefaultValue,
            ValidationRuleJson = f.ValidationRuleJson,
            ConditionalVisibilityJson = f.ConditionalVisibilityJson,
            OrderIndex = f.OrderIndex,
            Width = f.Width,
            SectionName = f.SectionName
        }).ToList()
    };

    private static TCodeDataDto MapData(TCodeDataEntity e) => new()
    {
        Id = e.Id,
        TCodeId = e.TCodeId,
        RecordId = e.RecordId,
        Data = JsonSerializer.Deserialize<Dictionary<string, object>>(e.DataJson, _jsonOpts) ?? new(),
        Status = e.Status,
        WorkflowNode = e.WorkflowNode,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
