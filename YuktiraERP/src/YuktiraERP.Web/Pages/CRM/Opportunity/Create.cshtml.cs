using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CRM.Opportunity;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<OpportunityEntity, Guid> _repo;
    private readonly ITenantContext _tenant;

    public CreateModel(IRepository<OpportunityEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }

    [BindProperty] public OpportunityEntity Opportunity { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Opportunity.OppId = "OPP-" + DateTime.Now.Ticks;
        await _repo.AddAsync(Opportunity);
        return RedirectToPage("/CRM/Index");
    }
}
