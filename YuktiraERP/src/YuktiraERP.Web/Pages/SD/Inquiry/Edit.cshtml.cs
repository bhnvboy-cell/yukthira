using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.SD.Inquiry;

public class EditModel : PageModel
{
    private readonly IRepository<InquiryEntity, Guid> _repo;
    public EditModel(IRepository<InquiryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InquiryEntity Inquiry { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/SD/Inquiry/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Inquiry = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Inquiry);
        return RedirectToPage("/SD/Inquiry/List");
    }
}
