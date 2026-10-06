using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Dashboard;

[Authorize]
public class DynamicCanvasModel : PageModel
{
    private readonly IApprovalService _approvals;

    public DynamicCanvasModel(IApprovalService approvals) => _approvals = approvals;

    public DynamicCanvasContext Context { get; set; } = new();

    public async Task OnGetAsync()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var userName = User.Identity?.Name ?? "User";
        var tenantId = Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
        var userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var hour = DateTime.Now.Hour;

        var pending = 0;
        try
        {
            pending = (await _approvals.GetPendingApprovalsAsync(tenantId, userId)).Count;
        }
        catch
        {
            pending = 0;
        }

        Context = new DynamicCanvasContext
        {
            Role = role,
            UserName = userName,
            Hour = hour,
            PendingApprovals = pending,
            TenantId = tenantId == Guid.Empty ? string.Empty : tenantId.ToString(),
            IsAdmin = role is "ADMIN" or "SUPER_USER",
            IsSuperUser = role == "SUPER_USER",
            Greeting = BuildGreeting(hour, userName, pending)
        };
    }

    private static string BuildGreeting(int hour, string userName, int pending)
    {
        var part = hour switch
        {
            < 5 => "Burning the midnight oil",
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            < 22 => "Good evening",
            _ => "Working late"
        };
        var suffix = pending > 0
            ? $"{pending} approval{(pending == 1 ? "" : "s")} waiting on you"
            : "no approvals waiting";
        return $"{part}, {userName} - {suffix}";
    }
}

public class DynamicCanvasContext
{
    public string Role { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Hour { get; set; }
    public int PendingApprovals { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsSuperUser { get; set; }
    public string Greeting { get; set; } = string.Empty;
}
