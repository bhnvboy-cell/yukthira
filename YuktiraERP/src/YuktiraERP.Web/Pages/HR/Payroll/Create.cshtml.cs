using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR.Payroll;
public class CreateModel : PageModel
{
    private readonly IRepository<PayrollEntryEntity, Guid> _repo;
    public CreateModel(IRepository<PayrollEntryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public PayrollEntryEntity Entry { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Entry); return RedirectToPage("/HR/Index"); }
}
