using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly INotificationService _notificationService;
    public IndexModel(INotificationService notificationService) => _notificationService = notificationService;
    public List<NotificationDto> Notifications { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        Notifications = await _notificationService.GetUserNotificationsAsync(userId);
    }
}
