using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.QM.InspectionLot;
public class DisplayModel : PageModel
{
    private readonly IRepository<InspectionLotEntity, Guid> _repo;
    public DisplayModel(IRepository<InspectionLotEntity, Guid> repo) { _repo = repo; }
    public InspectionLotEntity Lot { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/QM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Lot = entity;
        return Page();
    }
}
