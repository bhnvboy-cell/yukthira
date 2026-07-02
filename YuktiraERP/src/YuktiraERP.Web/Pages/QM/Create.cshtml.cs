using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.QM;
public class CreateModel : PageModel
{
    private readonly IRepository<InspectionLotEntity, Guid> _repo;
    public CreateModel(IRepository<InspectionLotEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InspectionLotEntity Lot { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Lot); return RedirectToPage("/QM/Index"); }
}
