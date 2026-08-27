using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Transactions;

public class TransactionEngineModel : PageModel
{
    private readonly ITCodeLayoutRegistry _registry;
    public string Code { get; set; } = "";
    public Core.Domain.Transaction.TCodeLayoutConfig? Config { get; set; }

    public TransactionEngineModel(ITCodeLayoutRegistry registry) => _registry = registry;

    public IActionResult OnGet(string code)
    {
        Code = code.ToUpperInvariant();
        Config = _registry.Get(Code);
        if (Config is null) return Page();
        return Page();
    }
}
