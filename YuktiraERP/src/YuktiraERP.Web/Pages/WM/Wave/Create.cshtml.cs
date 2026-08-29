using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.WM.Wave;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<WaveEntity, Guid> _repo;
    public CreateModel(IRepository<WaveEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public WaveEntity Wave { get; set; } = new();
    public IActionResult OnGet() { Wave.CutoffTime = DateTime.UtcNow.AddHours(4); return Page(); }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Wave.TenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? "54e6957e-4db2-4731-a7f4-2e50e597bf91");
        if (string.IsNullOrEmpty(Wave.WaveNumber)) Wave.WaveNumber = "WV-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        await _repo.AddAsync(Wave);
        return RedirectToPage("/WM/Index");
    }
}
