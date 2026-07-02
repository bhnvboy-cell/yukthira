using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CRM.Opportunity;
public class DisplayModel : PageModel
{
    private readonly IRepository<OpportunityEntity, Guid> _repo;
    public DisplayModel(IRepository<OpportunityEntity, Guid> repo) { _repo = repo; }
    public OpportunityEntity Opportunity { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CRM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Opportunity = entity;
        return Page();
    }
}
