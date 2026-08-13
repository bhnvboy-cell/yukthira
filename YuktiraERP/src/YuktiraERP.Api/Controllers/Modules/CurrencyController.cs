using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/fi/[controller]")]
[Authorize]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currency;
    private readonly ITenantContext _tenant;

    public CurrencyController(ICurrencyService currency, ITenantContext tenant)
    {
        _currency = currency;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrencies()
    {
        var result = await _currency.GetCurrenciesAsync(_tenant.TenantId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateCurrency([FromBody] CurrencyDto request)
    {
        try
        {
            var result = await _currency.CreateCurrencyAsync(_tenant.TenantId, request);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateCurrency(Guid id, [FromBody] CurrencyDto request)
    {
        try
        {
            var result = await _currency.UpdateCurrencyAsync(_tenant.TenantId, id, request);
            if (result == null) return NotFound();
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteCurrency(Guid id)
    {
        try
        {
            await _currency.DeleteCurrencyAsync(_tenant.TenantId, id);
            return Ok(new { success = true, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("base")]
    public async Task<IActionResult> GetBaseCurrency()
    {
        var result = await _currency.GetBaseCurrencyAsync(_tenant.TenantId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("rates")]
    public async Task<IActionResult> GetExchangeRates([FromQuery] string? from = null, [FromQuery] string? to = null)
    {
        var result = await _currency.GetExchangeRatesAsync(_tenant.TenantId, from, to);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("rates")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> SetExchangeRate([FromBody] ExchangeRateDto request)
    {
        try
        {
            var result = await _currency.SetExchangeRateAsync(_tenant.TenantId, request);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] CurrencyConversionRequest request)
    {
        try
        {
            var result = await _currency.ConvertAsync(_tenant.TenantId, request);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("revaluate")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Revaluate([FromBody] CurrencyRevaluationRequest request)
    {
        try
        {
            var result = await _currency.RevaluateAsync(_tenant.TenantId, request);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}