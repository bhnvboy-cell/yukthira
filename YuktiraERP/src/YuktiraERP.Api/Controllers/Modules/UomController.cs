using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/core/uom")]
[Authorize]
public class UomController : ControllerBase
{
    private readonly IUomConversionService _uom;
    private readonly ITenantContext _tenant;

    public UomController(IUomConversionService uom, ITenantContext tenant)
    {
        _uom = uom;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveUoms()
    {
        var result = await _uom.GetActiveUomsAsync(_tenant.TenantId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] UomConversionRequest request)
    {
        try
        {
            var result = await _uom.ConvertAsync(request, _tenant.TenantId);
            return Ok(new { data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost]
    [Authorize(Policy = "CORE_ADMIN")]
    public async Task<IActionResult> CreateOrUpdateUom([FromBody] UomCreateUpdateRequest request)
    {
        try
        {
            var result = await _uom.CreateOrUpdateUomAsync(request, _tenant.TenantId);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("material/{materialCode}/conversions")]
    public async Task<IActionResult> GetMaterialConversions(string materialCode)
    {
        var result = await _uom.GetMaterialConversionsAsync(materialCode, _tenant.TenantId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("validate")]
    public IActionResult ValidateDecimalPrecision([FromQuery] string uomCode, [FromQuery] decimal value)
    {
        var result = _uom.ValidateDecimalPrecision(uomCode, value);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }
}
