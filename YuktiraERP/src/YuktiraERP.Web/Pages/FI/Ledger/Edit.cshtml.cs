using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.FI.Ledger;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<JournalEntryEntity, Guid> _repo;
    public EditModel(IRepository<JournalEntryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public JournalEntryEntity Entry { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/FI/Ledger/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Entry = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Entry);
        return RedirectToPage("/FI/Ledger/List");
    }
}
