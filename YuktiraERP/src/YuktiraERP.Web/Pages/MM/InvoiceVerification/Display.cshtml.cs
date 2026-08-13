using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.InvoiceVerification;

public class DisplayModel : PageModel
{
    private readonly IRepository<InvoiceVerificationEntity, Guid> _repo;
    public DisplayModel(IRepository<InvoiceVerificationEntity, Guid> repo) { _repo = repo; }
    public InvoiceVerificationEntity Invoice { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/InvoiceVerification/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Invoice = entity;
        return Page();
    }
}
