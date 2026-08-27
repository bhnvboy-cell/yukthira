using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.SD.Billing;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<BillingDocumentEntity, Guid> _repo;
    public DisplayModel(IRepository<BillingDocumentEntity, Guid> repo) { _repo = repo; }
    public BillingDocumentEntity Invoice { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/SD/Billing/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Invoice = entity;
        return Page();
    }
}
