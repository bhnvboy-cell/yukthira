using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.SD;
public class CreateCustomerModel : PageModel
{
    private readonly IRepository<CustomerEntity, Guid> _repo;
    public CreateCustomerModel(IRepository<CustomerEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public CustomerEntity Customer { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Customer); return RedirectToPage("/SD/Index"); }
}
