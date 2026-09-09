using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.MB;

[Authorize]
public class MB51ReportModel : PageModel
{
    private readonly IInventoryReportingService _service;
    private readonly ITenantContext _tenant;

    public MB51ReportModel(IInventoryReportingService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [BindProperty(SupportsGet = true)]
    public Mb51FilterDto Filter { get; set; } = new();

    public List<Mb51DocumentDto>? Documents { get; set; }

    public async Task OnGetAsync()
    {
        Documents = await _service.GetMb51DocumentsAsync(Filter, _tenant.TenantId);
    }
}
