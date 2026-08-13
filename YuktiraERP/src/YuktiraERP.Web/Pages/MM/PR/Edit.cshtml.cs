using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.PR;

public class EditModel : PageModel
{
    private readonly IRepository<PurchaseRequisitionEntity, Guid> _repo;
    public EditModel(IRepository<PurchaseRequisitionEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public PurchaseRequisitionEntity Requisition { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/PR/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Requisition = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Requisition);
        return RedirectToPage("/MM/PR/List");
    }
}
