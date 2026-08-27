using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CRM.Campaign;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<CampaignEntity, Guid> _repo;
    public EditModel(IRepository<CampaignEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public CampaignEntity Campaign { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CRM/Campaign/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Campaign = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Campaign);
        return RedirectToPage("/CRM/Campaign/List");
    }
}
