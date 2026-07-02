using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CRM.Contact;
public class CreateModel : PageModel
{
    private readonly IRepository<ContactEntity, Guid> _repo;
    public CreateModel(IRepository<ContactEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ContactEntity Contact { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Contact); return RedirectToPage("/CRM/Index"); }
}
