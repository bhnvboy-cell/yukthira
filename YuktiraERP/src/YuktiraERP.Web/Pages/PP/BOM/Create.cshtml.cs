using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.BOM;
public class CreateModel : PageModel
{
    private readonly IRepository<BillOfMaterialEntity, Guid> _repo;
    public CreateModel(IRepository<BillOfMaterialEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public BillOfMaterialEntity Bom { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Bom); return RedirectToPage("/PP/Index"); }
}
