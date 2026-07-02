using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/tcode-customize")]
[Authorize(Roles = "SUPER_USER,ADMIN")]
public class TCodeCustomizationController : ControllerBase
{
    private readonly ITCodeCustomizationService _service;
    private readonly ITenantContext _tenant;

    public TCodeCustomizationController(ITCodeCustomizationService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("{tcode}/fields")]
    public async Task<IActionResult> GetFields(string tcode)
        => Ok(await _service.GetCustomFieldsAsync(_tenant.TenantId, tcode));

    [HttpPost("{tcode}/fields")]
    public async Task<IActionResult> AddField(string tcode, [FromBody] CustomFieldDto field)
        => Ok(await _service.AddCustomFieldAsync(_tenant.TenantId, tcode, field));

    [HttpPut("fields/{id}")]
    public async Task<IActionResult> UpdateField(Guid id, [FromBody] CustomFieldDto field)
    {
        var ok = await _service.UpdateCustomFieldAsync(_tenant.TenantId, id, field);
        return ok ? Ok() : NotFound();
    }

    [HttpDelete("fields/{id}")]
    public async Task<IActionResult> DeleteField(Guid id)
    {
        var ok = await _service.DeleteCustomFieldAsync(_tenant.TenantId, id);
        return ok ? Ok() : NotFound();
    }

    [HttpGet("{tcode}/layout")]
    public async Task<IActionResult> GetLayout(string tcode)
        => Ok(await _service.GetLayoutAsync(_tenant.TenantId, tcode));

    [HttpPost("{tcode}/layout")]
    public async Task<IActionResult> SaveLayout(string tcode, [FromBody] List<LayoutFieldDto> layout)
    {
        var ok = await _service.SaveLayoutAsync(_tenant.TenantId, tcode, layout);
        return ok ? Ok() : BadRequest();
    }
}
