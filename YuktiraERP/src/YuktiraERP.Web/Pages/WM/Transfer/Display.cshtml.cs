using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.WM.Transfer;
public class DisplayModel : PageModel
{
    private readonly IRepository<WarehouseTransferEntity, Guid> _repo;
    public DisplayModel(IRepository<WarehouseTransferEntity, Guid> repo) { _repo = repo; }
    public WarehouseTransferEntity Transfer { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/WM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Transfer = entity;
        return Page();
    }
}
