using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CRM.Contact;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<ContactEntity, Guid> _repo;
    public EditModel(IRepository<ContactEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ContactEntity Contact { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CRM/Contact/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Contact = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Contact);
        return RedirectToPage("/CRM/Contact/List");
    }
}
