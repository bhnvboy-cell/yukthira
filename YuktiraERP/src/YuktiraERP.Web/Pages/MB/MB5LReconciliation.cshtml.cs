using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MB;

[Authorize]
public class MB5LReconciliationModel : PageModel
{
    private readonly IInventoryReportingService _service;
    private readonly ITenantContext _tenant;

    public MB5LReconciliationModel(IInventoryReportingService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty(SupportsGet = true)]
    public string? MaterialCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Plant { get; set; }

    public List<Mb5LReconciliationDto>? Data { get; set; }

    public async Task OnGetAsync()
    {
        Data = await _service.GetMb5LReconciliationAsync(MaterialCode, Plant, _tenant.TenantId);
    }
}
