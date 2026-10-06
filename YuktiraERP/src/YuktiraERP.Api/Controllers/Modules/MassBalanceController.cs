using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/pp/mass-balance")]
[Authorize]
public class MassBalanceController : ControllerBase
{
    private readonly IMassBalanceCalculator _massBalance;
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public MassBalanceController(IMassBalanceCalculator massBalance, YuktiraDbContext db, ITenantContext tenant)
    {
        _massBalance = massBalance;
        _db = db;
        _tenant = tenant;
    }

    [HttpPost("calculate")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Calculate([FromBody] MassBalanceInput input, CancellationToken ct)
    {
        var result = await _massBalance.CalculateAdHocAsync(input ?? new MassBalanceInput(), User.Identity?.Name ?? "system", ct);
        return Ok(new { success = true, data = result, id = result.Id, tenantId = _tenant.TenantId });
    }

    [HttpGet("order/{productionOrderId}")]
    public async Task<IActionResult> GetForOrder(Guid productionOrderId, CancellationToken ct)
    {
        var result = await _db.MassBalanceResults.AsNoTracking()
            .Where(r => r.TenantId == _tenant.TenantId && r.ProductionOrderId == productionOrderId)
            .OrderByDescending(r => r.CalculatedAt)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (result == null)
            return NotFound(new { success = false, error = "No mass balance result was found for this production order", productionOrderId });

        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 20)
    {
        var resolved = limit < 1 ? 1 : (limit > 100 ? 100 : limit);
        var results = await _db.MassBalanceResults.AsNoTracking()
            .Where(r => r.TenantId == _tenant.TenantId)
            .OrderByDescending(r => r.CalculatedAt)
            .ThenByDescending(r => r.Id)
            .Take(resolved)
            .ToListAsync();

        return Ok(new { data = results, count = results.Count, tenantId = _tenant.TenantId });
    }

    [HttpPost("order/{productionOrderId}/recalculate")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Recalculate(Guid productionOrderId, CancellationToken ct)
    {
        try
        {
            var result = await _massBalance.CalculateForOrderAsync(productionOrderId, User.Identity?.Name ?? "system", ct);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, error = ex.Message, productionOrderId });
        }
    }
}
