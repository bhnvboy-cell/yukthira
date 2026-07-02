using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly YuktiraDbContext _db;
    private readonly IConfiguration _configuration;

    public NotificationService(YuktiraDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task SendAsync(SendNotificationRequest request)
    {
        var notification = new NotificationEntity
        {
            UserId = request.UserId,
            Channel = request.Channel.ToString(),
            Title = request.Title,
            Message = request.Message,
            LinkUrl = request.LinkUrl ?? "",
            IsRead = false,
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        if (request.Channel is NotificationChannelType.Email or NotificationChannelType.All)
            await SendEmailAsync(request);
        if (request.Channel is NotificationChannelType.SMS or NotificationChannelType.All)
            SendSms(request);
    }

    public async Task SendToRoleAsync(Guid tenantId, string roleCode, SendNotificationRequest request)
    {
        var users = await _db.AdminUsers.Where(u => u.Role == roleCode && u.IsActive).ToListAsync();
        foreach (var user in users)
        {
            _db.Notifications.Add(new NotificationEntity
            {
                UserId = user.Id,
                Channel = request.Channel.ToString(),
                Title = request.Title,
                Message = request.Message,
                LinkUrl = request.LinkUrl ?? "",
                IsRead = false,
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Channel = n.Channel,
                Title = n.Title,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var n = await _db.Notifications.FindAsync(notificationId);
        if (n != null)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsRead, true));
    }

    private async Task SendEmailAsync(SendNotificationRequest request)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var useSsl = bool.Parse(_configuration["Email:UseSsl"] ?? "true");

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(username))
            {
                Console.WriteLine($"[EMAIL-FALLBACK] To: {request.UserId} | Subject: {request.Title} | Body: {request.Message}");
                return;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new System.Net.NetworkCredential(username, password),
                EnableSsl = useSsl
            };

            var user = await _db.AdminUsers.FindAsync(request.UserId);
            var toEmail = user?.Email ?? $"{request.UserId}@yuktira.com";

            await client.SendMailAsync(
                new MailMessage(username, toEmail, request.Title, request.Message)
                {
                    IsBodyHtml = request.Message.Contains('<') && request.Message.Contains('>')
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL-FAILED] {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void SendSms(SendNotificationRequest request)
    {
        Console.WriteLine($"[SMS] To: {request.UserId} | Message: {request.Message}");
    }
}
