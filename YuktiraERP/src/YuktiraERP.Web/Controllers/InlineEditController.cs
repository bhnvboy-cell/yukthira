using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Controllers;

[ApiController]
[Route("api/v1/{module}/{entity}/inline-update")]
[Authorize]
public class InlineEditController : ControllerBase
{
    private readonly IRepository<Infrastructure.Data.Entities.EntityBase, Guid> _repo;

    public InlineEditController(IServiceProvider serviceProvider)
    {
        _repo = serviceProvider.GetRequiredService<IRepository<Infrastructure.Data.Entities.EntityBase, Guid>>();
    }

    [HttpPatch]
    public async Task<IActionResult> InlineUpdate(string module, string entity, [FromBody] InlineUpdateRequest request)
    {
        if (request?.Id == null || request.Id == Guid.Empty)
            return BadRequest(new { success = false, message = "Invalid record ID" });

        if (request.Fields == null || request.Fields.Count == 0)
            return BadRequest(new { success = false, message = "No fields to update" });

        var record = await _repo.GetByIdAsync(request.Id.Value);
        if (record == null)
            return NotFound(new { success = false, message = "Record not found" });

        var entityType = record.GetType();
        foreach (var field in request.Fields)
        {
            var prop = entityType.GetProperty(field.Key);
            if (prop == null || !prop.CanWrite) continue;

            try
            {
                var value = Convert.ChangeType(field.Value, prop.PropertyType);
                prop.SetValue(record, value);
            }
            catch
            {
                // Skip fields that can't be converted
            }
        }

        await _repo.UpdateAsync(record);
        return Ok(new { success = true, message = "Record updated", id = request.Id });
    }
}

public class InlineUpdateRequest
{
    public Guid? Id { get; set; }
    public Dictionary<string, object>? Fields { get; set; }
}
