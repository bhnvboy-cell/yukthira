using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP.BOM;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<BillOfMaterialEntity, Guid> _repo;
    private readonly ITenantContext _tenant;

    public CreateModel(IRepository<BillOfMaterialEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    [BindProperty] public BillOfMaterialEntity Bom { get; set; } = new();

    public IActionResult OnGet()
    {
        Bom.ValidFrom = DateTime.UtcNow;
        Bom.ValidTo = DateTime.UtcNow.AddYears(5);
        Bom.BaseQuantity = 1;
        Bom.BOMUsage = "Production";
        Bom.ItemCategory = "L";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Bom.TenantId = _tenant.TenantId;
        Bom.BomId = $"BOM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        await _repo.AddAsync(Bom);
        return RedirectToPage("/PP/Index");
    }
}
