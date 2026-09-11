using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.QM.Notification;

public class EditModel : PageModel
{
    private readonly IRepository<QualityNotificationEntity, Guid> _repo;
    [BindProperty] public QualityNotificationEntity Notification { get; set; } = new();
    public EditModel(IRepository<QualityNotificationEntity, Guid> repo) => _repo = repo;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        Notification = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _repo.UpdateAsync(Notification);
        return RedirectToPage("/QM/Index");
    }
}
