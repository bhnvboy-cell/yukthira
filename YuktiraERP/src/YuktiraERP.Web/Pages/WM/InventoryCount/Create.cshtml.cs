using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.WM.InventoryCount;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<InventoryCountEntity, Guid> _repo;
    public CreateModel(IRepository<InventoryCountEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InventoryCountEntity Count { get; set; } = new();
    public IActionResult OnGet() { Count.ScheduledDate = DateTime.UtcNow.Date.AddDays(1); return Page(); }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Count.TenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? "54e6957e-4db2-4731-a7f4-2e50e597bf91");
        if (string.IsNullOrEmpty(Count.CountNumber)) Count.CountNumber = "IC-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        await _repo.AddAsync(Count);
        return RedirectToPage("/WM/Index");
    }
}
