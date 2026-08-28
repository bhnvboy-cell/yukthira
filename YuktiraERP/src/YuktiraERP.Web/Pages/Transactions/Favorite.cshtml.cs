using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Transactions;

[Authorize]
[IgnoreAntiforgeryToken]
public class FavoriteModel : PageModel
{
    private readonly ITransactionCodeService _service;
    public FavoriteModel(ITransactionCodeService service) => _service = service;

    public async Task<IActionResult> OnPostAsync([FromQuery] Guid transactionCodeId)
    {
        var userId = GetUserId();
        await _service.ToggleFavoriteAsync(userId, transactionCodeId);
        return new JsonResult(new { success = true });
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
}
