using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MB;

[Authorize]
public class MB52StockOverviewModel : PageModel
{
    private readonly IInventoryReportingService _service;
    private readonly ITenantContext _tenant;

    public MB52StockOverviewModel(IInventoryReportingService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty(SupportsGet = true)]
    public Mb52StockOverviewFilterDto Filter { get; set; } = new()
    {
        IncludeZeroStock = true,
        GroupBy = MbStockOverviewLevel.Material
    };

    public List<Mb52StockOverviewDto>? Stocks { get; set; }

    public async Task OnGetAsync()
    {
        Stocks = await _service.GetMb52StockOverviewAsync(Filter, _tenant.TenantId);
    }
}
