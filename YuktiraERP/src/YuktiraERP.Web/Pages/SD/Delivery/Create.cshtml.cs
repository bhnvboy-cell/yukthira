using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.SD.Delivery;
public class CreateModel : PageModel
{
    private readonly IRepository<DeliveryEntity, Guid> _repo;
    public CreateModel(IRepository<DeliveryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public DeliveryEntity Delivery { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Delivery); return RedirectToPage("/SD/Index"); }
}
