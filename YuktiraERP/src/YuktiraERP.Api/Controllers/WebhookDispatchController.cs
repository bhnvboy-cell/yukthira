using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/webhook/dispatch")]
[Authorize(Roles = "SUPER_USER,ADMIN")]
public class WebhookDispatchController : ControllerBase
{
    private readonly IWebhookService _webhook;

    public WebhookDispatchController(IWebhookService webhook) => _webhook = webhook;

    [HttpPost]
    public async Task<IActionResult> Dispatch([FromBody] DispatchRequest req)
    {
        var tenantId = Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
        await _webhook.DispatchAsync(tenantId, req.EventType, req.EntityType, req.EntityId, req.Payload);
        return Ok(new { message = "Webhook dispatched" });
    }
}

public record DispatchRequest(string EventType, string EntityType, string EntityId, object? Payload = null);
