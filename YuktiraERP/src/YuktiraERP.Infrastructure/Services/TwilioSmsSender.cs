using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class TwilioSmsSender : ISmsSender
{
    private readonly IConfiguration _configuration;
    private readonly YuktiraDbContext _db;
    private readonly IHttpClientFactory _httpFactory;

    public TwilioSmsSender(IConfiguration configuration, YuktiraDbContext db, IHttpClientFactory httpFactory)
    {
        _configuration = configuration;
        _db = db;
        _httpFactory = httpFactory;
    }

    public async Task<bool> SendAsync(SmsMessage message, Guid tenantId)
    {
        var accountSid = _configuration["SMS:AccountSid"];
        var authToken = _configuration["SMS:AuthToken"];
        var fromNumber = _configuration["SMS:FromNumber"];
        var provider = _configuration["SMS:Provider"] ?? "Twilio";

        var delivery = new MessageDeliveryEntity
        {
            TenantId = tenantId,
            Channel = "SMS",
            ToAddress = message.To,
            Subject = "",
            Body = message.Body,
            Provider = provider
        };

        try
        {
            if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(fromNumber))
            {
                // No provider configured — log the attempt clearly instead of silently dropping.
                delivery.Status = "Unconfigured";
                delivery.ErrorMessage = "SMS provider credentials not configured";
                delivery.SentAt = DateTime.UtcNow;
                _db.MessageDeliveries.Add(delivery);
                await _db.SaveChangesAsync();
                return false;
            }

            var client = _httpFactory.CreateClient();
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}")));

            var form = new Dictionary<string, string>
            {
                ["To"] = message.To,
                ["From"] = fromNumber,
                ["Body"] = message.Body
            };
            request.Content = new FormUrlEncodedContent(form);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            delivery.Status = response.IsSuccessStatusCode ? "Sent" : "Failed";
            delivery.ErrorMessage = response.IsSuccessStatusCode ? "" : $"HTTP {(int)response.StatusCode}: {body}";
            delivery.SentAt = DateTime.UtcNow;
            _db.MessageDeliveries.Add(delivery);
            await _db.SaveChangesAsync();
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            delivery.Status = "Failed";
            delivery.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            delivery.SentAt = DateTime.UtcNow;
            _db.MessageDeliveries.Add(delivery);
            await _db.SaveChangesAsync();
            return false;
        }
    }
}