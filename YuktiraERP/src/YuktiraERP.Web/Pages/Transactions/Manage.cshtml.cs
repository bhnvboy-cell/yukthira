using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Domain.Transaction;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Transactions;

[Authorize(Roles = "SUPER_USER,ADMIN")]
public class ManageModel : PageModel
{
    private readonly ITransactionCodeService _service;
    private readonly IModuleCatalog _catalog;

    public ManageModel(ITransactionCodeService service, IModuleCatalog catalog)
    {
        _service = service;
        _catalog = catalog;
    }

    public List<TransactionCodeDto> Codes { get; set; } = new();
    public List<TransactionLogDto> Logs { get; set; } = new();
    public IReadOnlyList<YuktiraERP.Core.Domain.Modules.ModuleDefinition> Modules { get; set; } = new List<YuktiraERP.Core.Domain.Modules.ModuleDefinition>();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        Codes = await _service.GetAllAsync();
        Logs = await _service.GetLogAsync(pageSize: 20);
        Modules = _catalog.Modules;
    }

    public async Task<IActionResult> OnPostCreateAsync(string code, string name, string description, string module, string route, string icon, string group, string requiredRole)
    {
        try
        {
            Enum.TryParse<TransactionGroup>(group, out var tg);
            await _service.CreateAsync(new TransactionCodeDto
            {
                Code = code.ToUpperInvariant(),
                Name = name,
                Description = description,
                Module = module.ToUpperInvariant(),
                Group = tg,
                Route = route,
                Icon = string.IsNullOrEmpty(icon) ? "bi-asterisk" : icon,
                RequiredRole = requiredRole
            });
            SuccessMessage = $"Transaction code {code} created successfully";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        Codes = await _service.GetAllAsync();
        Logs = await _service.GetLogAsync(pageSize: 20);
        Modules = _catalog.Modules;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) ErrorMessage = "Cannot delete system transactions";
        else SuccessMessage = "Transaction deleted";
        Codes = await _service.GetAllAsync();
        Logs = await _service.GetLogAsync(pageSize: 20);
        Modules = _catalog.Modules;
        return Page();
    }

    public async Task<IActionResult> OnPostToggleFavoriteAsync(Guid codeId)
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        await _service.ToggleFavoriteAsync(userId, codeId);
        Codes = await _service.GetAllAsync();
        Logs = await _service.GetLogAsync(pageSize: 20);
        return Page();
    }
}
