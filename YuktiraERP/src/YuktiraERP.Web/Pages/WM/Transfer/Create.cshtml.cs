using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.WM.Transfer;
public class CreateModel : PageModel
{
    private readonly IRepository<WarehouseTransferEntity, Guid> _repo;
    public CreateModel(IRepository<WarehouseTransferEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public WarehouseTransferEntity Transfer { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Transfer); return RedirectToPage("/WM/Index"); }
}
