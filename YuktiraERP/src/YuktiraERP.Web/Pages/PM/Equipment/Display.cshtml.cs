using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PM.Equipment;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<EquipmentEntity, Guid> _repo;
    public DisplayModel(IRepository<EquipmentEntity, Guid> repo) { _repo = repo; }
    public EquipmentEntity Equipment { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        Equipment = item;
        return Page();
    }
}
