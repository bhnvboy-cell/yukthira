using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.Recruitment;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<RecruitmentEntity, Guid> _repo;
    private readonly ITenantContext _tenant;

    public CreateModel(IRepository<RecruitmentEntity, Guid> repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    [BindProperty]
    public RecruitmentEntity Recruitment { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        Recruitment.RequisitionId = "REQ-" + DateTime.Now.Ticks;
        Recruitment.TenantId = _tenant.TenantId;
        Recruitment.RequestedDate = DateTime.UtcNow;
        await _repo.AddAsync(Recruitment);
        return RedirectToPage("/HR/Index");
    }
}
