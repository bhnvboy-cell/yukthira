using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MB;

[Authorize]
public class MB5BHistoricalModel : PageModel
{
    private readonly IInventoryReportingService _service;
    private readonly ITenantContext _tenant;

    public MB5BHistoricalModel(IInventoryReportingService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty(SupportsGet = true)]
    public string? MaterialCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Plant { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-30);

    [BindProperty(SupportsGet = true)]
    public DateTime ToDate { get; set; } = DateTime.Today;

    public List<Mb5BHistoricalStockDto>? DataList { get; set; }
    public Mb5BHistoricalStockDto? Data { get; set; }

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(MaterialCode) && !string.IsNullOrWhiteSpace(Plant))
        {
            DataList = await _service.GetMb5BHistoricalStockAsync(MaterialCode, Plant, FromDate, ToDate, _tenant.TenantId);
            Data = DataList.FirstOrDefault();
        }
    }
}
