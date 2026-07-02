using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.LIMS.Instrument;
public class CreateModel : PageModel
{
    private readonly IRepository<InstrumentEntity, Guid> _repo;
    public CreateModel(IRepository<InstrumentEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InstrumentEntity Instrument { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Instrument); return RedirectToPage("/LIMS/Index"); }
}
