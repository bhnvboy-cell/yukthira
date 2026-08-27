using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.SD.Inquiry;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<InquiryEntity, Guid> _repo;
    public DisplayModel(IRepository<InquiryEntity, Guid> repo) { _repo = repo; }
    public InquiryEntity Inquiry { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/SD/Inquiry/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Inquiry = entity;
        return Page();
    }
}
