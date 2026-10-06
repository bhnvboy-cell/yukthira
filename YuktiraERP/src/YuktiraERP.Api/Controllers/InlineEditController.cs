using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/{module}/{entity}/inline-update")]
[Authorize]
public class InlineEditController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public InlineEditController(YuktiraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPatch]
    public async Task<IActionResult> InlineUpdate(string module, string entity, [FromBody] InlineUpdateRequest request)
    {
        if (request?.Id == null || request.Id == Guid.Empty)
            return BadRequest(new { success = false, message = "Invalid record ID" });

        if (request.Fields == null || request.Fields.Count == 0)
            return BadRequest(new { success = false, message = "No fields to update" });

        var modelEntity = _db.Model.GetEntityTypes()
            .FirstOrDefault(t => !t.IsOwned()
                && string.Equals(t.ClrType.Name, entity, StringComparison.OrdinalIgnoreCase));
        var primaryKey = modelEntity?.FindPrimaryKey();
        if (modelEntity == null || primaryKey == null || primaryKey.Properties.Count != 1
            || primaryKey.Properties[0].ClrType != typeof(Guid))
            return NotFound(new { success = false, message = $"Unknown entity '{entity}'" });

        var entityType = modelEntity.ClrType;
        var record = _db.Find(entityType, request.Id.Value);
        if (record == null)
            return NotFound(new { success = false, message = "Record not found" });

        var tenantProp = entityType.GetProperty("TenantId");
        if (tenantProp?.PropertyType == typeof(Guid))
        {
            var recordTenant = (Guid)(tenantProp.GetValue(record) ?? Guid.Empty);
            if (_tenant.TenantId != Guid.Empty && recordTenant != Guid.Empty && recordTenant != _tenant.TenantId)
                return Forbid();
        }

        var changed = 0;
        foreach (var field in request.Fields)
        {
            var prop = entityType.GetProperty(field.Key);
            if (prop == null || !prop.CanWrite) continue;

            try
            {
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                object? raw = field.Value;
                if (raw is System.Text.Json.JsonElement je)
                {
                    raw = je.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.Null => null,
                        System.Text.Json.JsonValueKind.String => je.GetString(),
                        System.Text.Json.JsonValueKind.True => true,
                        System.Text.Json.JsonValueKind.False => false,
                        System.Text.Json.JsonValueKind.Number => targetType == typeof(string)
                            ? je.GetRawText()
                            : (object)je.GetDouble(),
                        _ => je.GetRawText()
                    };
                }
                if (raw == null) continue;
                var value = Convert.ChangeType(raw, targetType);
                prop.SetValue(record, value);
                changed++;
            }
            catch
            {
                // Skip fields that can't be converted
            }
        }

        if (changed == 0)
            return BadRequest(new { success = false, message = "No fields could be updated" });

        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Record updated", id = request.Id, fieldsUpdated = changed });
    }
}

public class InlineUpdateRequest
{
    public Guid? Id { get; set; }
    public Dictionary<string, object>? Fields { get; set; }
}
