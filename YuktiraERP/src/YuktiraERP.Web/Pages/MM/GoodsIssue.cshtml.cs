using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM;
public class GoodsIssueModel : PageModel
{
    private readonly IRepository<GoodsReceiptEntity, Guid> _repo;
    public GoodsIssueModel(IRepository<GoodsReceiptEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public GoodsReceiptEntity GoodsReceipt { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        GoodsReceipt.GrnNumber = "GI-" + DateTime.Now.Ticks;
        GoodsReceipt.Date = DateTime.Now;
        GoodsReceipt.Status = "Issued";
        await _repo.AddAsync(GoodsReceipt);
        return RedirectToPage("/MM/Index");
    }
}
