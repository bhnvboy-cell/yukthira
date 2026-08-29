using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.QM.Notification;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<QualityNotificationEntity, Guid> _repo;
    public CreateModel(IRepository<QualityNotificationEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public QualityNotificationEntity Notification { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Notification); return RedirectToPage("/QM/Index"); }
}
