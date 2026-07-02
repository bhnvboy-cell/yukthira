using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CRM.Lead;
public class DisplayModel : PageModel
{
    private readonly IRepository<LeadEntity, Guid> _repo;
    public DisplayModel(IRepository<LeadEntity, Guid> repo) { _repo = repo; }
    public LeadEntity Lead { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CRM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Lead = entity;
        return Page();
    }
}
