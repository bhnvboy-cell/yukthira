using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Transactions;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ITransactionCodeService _service;

    public IndexModel(ITransactionCodeService service) => _service = service;

    public List<TransactionGroupVm> Groups { get; set; } = new();
    public List<TransactionCodeDto> Recent { get; set; } = new();
    public List<TransactionCodeDto> Favorites { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = GetUserId();
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

        var permitted = await _service.GetPermittedCodesAsync(userId, role);
        Groups = permitted.GroupBy(t => t.Group)
            .Select(g => new TransactionGroupVm
            {
                Name = g.Key.ToString(),
                Icon = GetGroupIcon(g.Key),
                Codes = g.OrderBy(t => t.SortOrder).ToList()
            }).ToList();

        var permittedIds = permitted.Select(t => t.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Recent = (await _service.GetRecentAsync(userId, 10)).Where(t => permittedIds.Contains(t.Code)).ToList();
        Favorites = (await _service.GetFavoritesAsync(userId)).Where(t => permittedIds.Contains(t.Code)).ToList();
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    private static string GetGroupIcon(Core.Domain.Transaction.TransactionGroup group) => group switch
    {
        Core.Domain.Transaction.TransactionGroup.MasterData => "bi-folder",
        Core.Domain.Transaction.TransactionGroup.Transactions => "bi-arrow-left-right",
        Core.Domain.Transaction.TransactionGroup.Process => "bi-arrow-repeat",
        Core.Domain.Transaction.TransactionGroup.Reports => "bi-file-earmark-bar-graph",
        Core.Domain.Transaction.TransactionGroup.Configuration => "bi-sliders",
        Core.Domain.Transaction.TransactionGroup.Analytics => "bi-graph-up",
        Core.Domain.Transaction.TransactionGroup.Administration => "bi-gear-wide",
        Core.Domain.Transaction.TransactionGroup.Utilities => "bi-tools",
        _ => "bi-asterisk"
    };

    public class TransactionGroupVm
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "bi-asterisk";
        public List<TransactionCodeDto> Codes { get; set; } = new();
    }
}
