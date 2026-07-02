using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI;
public class CreateModel : PageModel
{
    private readonly IRepository<JournalEntryEntity, Guid> _repo;
    public CreateModel(IRepository<JournalEntryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public JournalEntryEntity Entry { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Entry); return RedirectToPage("/FI/Index"); }
}
