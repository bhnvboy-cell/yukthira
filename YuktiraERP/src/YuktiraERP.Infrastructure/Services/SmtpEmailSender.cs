using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly YuktiraDbContext _db;

    public SmtpEmailSender(IConfiguration configuration, YuktiraDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    public async Task<bool> SendAsync(EmailMessage message, Guid tenantId)
    {
        var host = _configuration["Email:SmtpHost"];
        var port = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var useSsl = bool.Parse(_configuration["Email:UseSsl"] ?? "true");

        var delivery = new MessageDeliveryEntity
        {
            TenantId = tenantId,
            Channel = "Email",
            ToAddress = message.To,
            Subject = message.Subject,
            Body = message.Body,
            Provider = "SMTP"
        };

        try
        {
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("SMTP host or username is not configured");

            using var client = new SmtpClient(host, port)
            {
                Credentials = new System.Net.NetworkCredential(username, password),
                EnableSsl = useSsl
            };

            await client.SendMailAsync(new MailMessage(username, message.To, message.Subject, message.Body)
            {
                IsBodyHtml = message.IsHtml
            });

            delivery.Status = "Sent";
            delivery.SentAt = DateTime.UtcNow;
            _db.MessageDeliveries.Add(delivery);
            await _db.SaveChangesAsync();
            return true;
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