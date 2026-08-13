using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/comm/[controller]")]
[Authorize]
public class NotificationDeliveryController : ControllerBase
{
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;
    private readonly ITenantContext _tenant;
    private readonly YuktiraDbContext _db;

    public NotificationDeliveryController(IEmailSender email, ISmsSender sms, ITenantContext tenant, YuktiraDbContext db)
    {
        _email = email;
        _sms = sms;
        _tenant = tenant;
        _db = db;
    }

    [HttpPost("email/send")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> SendEmail([FromBody] EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.To))
            return BadRequest(new { error = "Recipient (To) is required" });
        var success = await _email.SendAsync(message, _tenant.TenantId);
        return Ok(new { success, tenantId = _tenant.TenantId });
    }

    [HttpPost("sms/send")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> SendSms([FromBody] SmsMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.To))
            return BadRequest(new { error = "Recipient (To) is required" });
        var success = await _sms.SendAsync(message, _tenant.TenantId);
        return Ok(new { success, tenantId = _tenant.TenantId });
    }

    [HttpGet("log")]
    public async Task<IActionResult> GetLog([FromQuery] int limit = 100)
    {
        var items = await _db.MessageDeliveries
            .Where(m => m.TenantId == _tenant.TenantId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .Select(m => new
            {
                m.Id, m.Channel, m.ToAddress, m.Subject, m.Status,
                m.ErrorMessage, m.Provider, m.SentAt
            })
            .ToListAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }
}