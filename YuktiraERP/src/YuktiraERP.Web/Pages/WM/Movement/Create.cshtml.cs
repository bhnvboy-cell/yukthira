using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.WM.Movement;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<StockMovementEntity, Guid> _repo;
    public CreateModel(IRepository<StockMovementEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public StockMovementEntity Movement { get; set; } = new();
    public IActionResult OnGet() { Movement.MovementDate = DateTime.UtcNow; return Page(); }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Movement.TenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? "54e6957e-4db2-4731-a7f4-2e50e597bf91");
        if (string.IsNullOrEmpty(Movement.MovementNumber)) Movement.MovementNumber = "MV-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        await _repo.AddAsync(Movement);
        return RedirectToPage("/WM/Index");
    }
}
