using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Transactions;

[Authorize]
public class TransactionEngineModel : PageModel
{
    private readonly ITCodeLayoutRegistry _registry;
    private readonly ITransactionCodeService _tx;
    private readonly IAuthorizationTraceService _trace;
    private readonly ITenantContext _tenant;

    public string Code { get; set; } = "";
    public Core.Domain.Transaction.TCodeLayoutConfig? Config { get; set; }

    public TransactionEngineModel(ITCodeLayoutRegistry registry, ITransactionCodeService tx, IAuthorizationTraceService trace, ITenantContext tenant)
    {
        _registry = registry;
        _tx = tx;
        _trace = trace;
        _tenant = tenant;
    }

    public async Task<IActionResult> OnGet(string code)
    {
        Code = (code ?? "").ToUpperInvariant();
        Guid? userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "";

        var check = await _tx.CheckAccessDetailedAsync(Code, userId, role);
        if (check.RuleSource != "UnknownTCode")
        {
            try
            {
                await _trace.TraceAsync(new AuthorizationTraceEntry
                {
                    TenantId = _tenant.TenantId,
                    UserId = userId,
                    UserName = userName,
                    Role = role ?? "",
                    SessionId = HttpContext.TraceIdentifier,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    ResourceType = "TCode",
                    Resource = Code,
                    Decision = check.Allowed ? "Allow" : "Deny",
                    RuleSource = check.RuleSource,
                    Reason = check.Reason,
                    HttpMethod = HttpContext.Request.Method,
                    HttpPath = HttpContext.Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers.UserAgent.ToString()
                });
            }
            catch
            {
            }
            if (!check.Allowed) return Redirect("/Auth/AccessDenied");
        }

        Config = _registry.Get(Code);
        return Page();
    }
}
