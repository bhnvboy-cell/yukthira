using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CO.InternalOrder;

public class CreateModel : PageModel
{
    private readonly IRepository<InternalOrderEntity, Guid> _repo;
    public CreateModel(IRepository<InternalOrderEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InternalOrderEntity Order { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Order); return RedirectToPage("/CO/Index"); }
}
