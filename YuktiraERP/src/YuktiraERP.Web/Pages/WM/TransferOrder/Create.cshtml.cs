using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.WM.TransferOrder;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<TransferOrderEntity, Guid> _repo;
    public CreateModel(IRepository<TransferOrderEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public TransferOrderEntity Order { get; set; } = new();
    public IActionResult OnGet() { Order.OrderDate = DateTime.UtcNow; return Page(); }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Order.TenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? "54e6957e-4db2-4731-a7f4-2e50e597bf91");
        if (string.IsNullOrEmpty(Order.OrderNumber)) Order.OrderNumber = "TO-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        await _repo.AddAsync(Order);
        return RedirectToPage("/WM/Index");
    }
}
