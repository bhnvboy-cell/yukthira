using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/mm/stock-overview")]
[Authorize]
public class StockOverviewController : ControllerBase
{
    private readonly IStockOverviewService _service;
    private readonly ITenantContext _tenant;

    public StockOverviewController(IStockOverviewService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpPost("mmbe")]
    public async Task<IActionResult> GetStockOverview([FromBody] StockOverviewFilterDto filter)
    {
        var result = await _service.GetStockOverviewHierarchyAsync(filter, _tenant.TenantId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }
}
