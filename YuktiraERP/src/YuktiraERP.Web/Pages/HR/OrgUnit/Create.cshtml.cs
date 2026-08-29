using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.OrgUnit;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<OrgUnitEntity, Guid> _repo;
    private readonly ITenantContext _tenant;

    public CreateModel(IRepository<OrgUnitEntity, Guid> repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    [BindProperty]
    public OrgUnitEntity OrgUnit { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        OrgUnit.UnitCode = "ORG-" + DateTime.Now.Ticks;
        OrgUnit.TenantId = _tenant.TenantId;
        await _repo.AddAsync(OrgUnit);
        return RedirectToPage("/HR/Index");
    }
}
