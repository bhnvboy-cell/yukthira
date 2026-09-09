using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MM;

[Authorize]
public class StockOverviewModel : PageModel
{
    private readonly IStockOverviewService _service;
    private readonly ITenantContext _tenant;

    public StockOverviewModel(IStockOverviewService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty]
    public StockOverviewFilterDto Filter { get; set; } = new();

    public StockOverviewResultDto Result { get; set; } = new();

    public async Task OnGetAsync()
    {
        Filter = new StockOverviewFilterDto { IncludeZeroStocks = false };
        Result = await _service.GetStockOverviewHierarchyAsync(Filter, _tenant.TenantId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Result = await _service.GetStockOverviewHierarchyAsync(Filter, _tenant.TenantId);
        return Page();
    }

    public async Task<IActionResult> OnPostSearchAsync([FromBody] StockOverviewFilterDto filter)
    {
        var result = await _service.GetStockOverviewHierarchyAsync(filter, _tenant.TenantId);
        return new JsonResult(result);
    }
}
