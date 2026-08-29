using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP.WorkCenter;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<WorkCenterEntity, Guid> _repo;
    private readonly ITenantContext _tenant;

    public CreateModel(IRepository<WorkCenterEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    [BindProperty] public WorkCenterEntity WorkCenter { get; set; } = new();

    public IActionResult OnGet()
    {
        WorkCenter.WorkCenterCategory = "Machine";
        WorkCenter.ShiftsPerDay = 1;
        WorkCenter.Status = "Active";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        WorkCenter.TenantId = _tenant.TenantId;
        WorkCenter.Code = $"WC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
        WorkCenter.CapacityPerDay = WorkCenter.CapacityPerShift * WorkCenter.ShiftsPerDay;
        await _repo.AddAsync(WorkCenter);
        return RedirectToPage("/PP/Index");
    }
}
