using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CRM.Pipeline;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<SalesPipelineEntity, Guid> _repo;
    private readonly ITenantContext _tenant;

    public CreateModel(IRepository<SalesPipelineEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }

    [BindProperty] public SalesPipelineEntity Pipeline { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Pipeline.PipelineId = "SPL-" + DateTime.Now.Ticks;
        Pipeline.TenantId = _tenant.TenantId;
        await _repo.AddAsync(Pipeline);
        return RedirectToPage("/CRM/Index");
    }
}
