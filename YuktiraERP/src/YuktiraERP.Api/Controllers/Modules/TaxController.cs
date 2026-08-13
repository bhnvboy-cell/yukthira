using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/fi/[controller]")]
[Authorize]
public class TaxController : ControllerBase
{
    private readonly ITaxService _tax;
    private readonly ITenantContext _tenant;

    public TaxController(ITaxService tax, ITenantContext tenant)
    {
        _tax = tax;
        _tenant = tenant;
    }

    [HttpGet("codes")]
    public async Task<IActionResult> GetTaxCodes()
    {
        var result = await _tax.GetTaxCodesAsync(_tenant.TenantId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("codes")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateTaxCode([FromBody] TaxCodeDto request)
    {
        try
        {
            var result = await _tax.CreateTaxCodeAsync(_tenant.TenantId, request);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("codes/{id:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateTaxCode(Guid id, [FromBody] TaxCodeDto request)
    {
        try
        {
            var result = await _tax.UpdateTaxCodeAsync(_tenant.TenantId, id, request);
            if (result == null) return NotFound();
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("codes/{id:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteTaxCode(Guid id)
    {
        await _tax.DeleteTaxCodeAsync(_tenant.TenantId, id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] TaxCalculationRequest request)
    {
        try
        {
            var result = await _tax.CalculateAsync(_tenant.TenantId, request);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("post-invoice")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PostInvoice([FromBody] TaxCalculationRequest request)
    {
        try
        {
            var result = await _tax.PostInvoiceAsync(_tenant.TenantId, request);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int limit = 100)
    {
        var result = await _tax.GetTaxTransactionsAsync(_tenant.TenantId, limit);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }
}