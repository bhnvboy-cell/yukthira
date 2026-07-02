using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CRM.Campaign;
public class CreateModel : PageModel
{
    private readonly IRepository<CampaignEntity, Guid> _repo;
    public CreateModel(IRepository<CampaignEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public CampaignEntity Campaign { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Campaign); return RedirectToPage("/CRM/Index"); }
}
