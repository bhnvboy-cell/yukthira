using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.LIMS;
public class CreateModel : PageModel
{
    private readonly IRepository<SampleEntity, Guid> _repo;
    public CreateModel(IRepository<SampleEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public SampleEntity Sample { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Sample); return RedirectToPage("/LIMS/Index"); }
}
