using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PM.Equipment;

public class CreateModel : PageModel
{
    private readonly IRepository<EquipmentEntity, Guid> _repo;
    public CreateModel(IRepository<EquipmentEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public EquipmentEntity Equipment { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Equipment); return RedirectToPage("/PM/Index"); }
}
