using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.PR;
public class CreateModel : PageModel
{
    private readonly IRepository<PurchaseRequisitionEntity, Guid> _repo;
    public CreateModel(IRepository<PurchaseRequisitionEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public PurchaseRequisitionEntity Requisition { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Requisition); return RedirectToPage("/MM/Index"); }
}
