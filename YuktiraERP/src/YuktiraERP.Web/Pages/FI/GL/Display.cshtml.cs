using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI.GL;
public class DisplayModel : PageModel
{
    private readonly IRepository<AccountEntity, Guid> _repo;
    public DisplayModel(IRepository<AccountEntity, Guid> repo) { _repo = repo; }
    [BindProperty(SupportsGet = true)] public string AccountCode { get; set; } = "";
    public AccountEntity? Account { get; set; }
    public List<AccountEntity> Accounts { get; set; } = new();
    public async Task OnGetAsync()
    {
        Accounts = await _repo.GetAllAsync();
        if (!string.IsNullOrEmpty(AccountCode))
            Account = Accounts.FirstOrDefault(a => a.AccountCode == AccountCode);
    }
}
