using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI.AP;
public class InvoiceCreateModel : PageModel
{
    private readonly IRepository<APEntryEntity, Guid> _repo;
    public InvoiceCreateModel(IRepository<APEntryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public APEntryEntity Entry { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Entry.DocumentNumber = "AP-" + DateTime.Now.Ticks;
        await _repo.AddAsync(Entry);
        return RedirectToPage("/FI/Index");
    }
}
