using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.QM.Notification;

public class DisplayModel : PageModel
{
    private readonly IRepository<QualityNotificationEntity, Guid> _repo;
    public QualityNotificationEntity Notification { get; set; } = new();
    public DisplayModel(IRepository<QualityNotificationEntity, Guid> repo) => _repo = repo;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        Notification = entity;
        return Page();
    }
}
