using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.Vendor;
public class DisplayModel : PageModel
{
    private readonly IRepository<VendorEntity, Guid> _repo;
    public DisplayModel(IRepository<VendorEntity, Guid> repo) { _repo = repo; }
    public VendorEntity Vendor { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Vendor = entity;
        return Page();
    }
}
