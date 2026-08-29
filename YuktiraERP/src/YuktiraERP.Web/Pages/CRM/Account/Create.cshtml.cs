using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CRM.Account;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<CrmAccountEntity, Guid> _repo;
    private readonly ITenantContext _tenant;

    public CreateModel(IRepository<CrmAccountEntity, Guid> repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    [BindProperty]
    public CrmAccountEntity Account { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        Account.AccountId = "ACC-" + DateTime.Now.Ticks;
        Account.TenantId = _tenant.TenantId;
        await _repo.AddAsync(Account);
        return RedirectToPage("/CRM/Index");
    }
}
