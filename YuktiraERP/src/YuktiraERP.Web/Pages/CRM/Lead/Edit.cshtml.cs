using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CRM.Lead;
public class EditModel : PageModel
{
    private readonly IRepository<LeadEntity, Guid> _repo;
    public EditModel(IRepository<LeadEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public LeadEntity Lead { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CRM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Lead = entity;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Lead);
        return RedirectToPage("/CRM/Index");
    }
}
