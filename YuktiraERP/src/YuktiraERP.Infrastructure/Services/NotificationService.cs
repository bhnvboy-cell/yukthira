using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly YuktiraDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;

    public NotificationService(YuktiraDbContext db, IEmailSender emailSender, ISmsSender smsSender)
    {
        _db = db;
        _emailSender = emailSender;
        _smsSender = smsSender;
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

        var tenantId = request.TenantId ?? Guid.Empty;
        if (request.Channel is NotificationChannelType.Email or NotificationChannelType.All)
            await SendEmailAsync(request, tenantId);
        if (request.Channel is NotificationChannelType.SMS or NotificationChannelType.All)
            await SendSmsAsync(request, tenantId);
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

    private async Task SendEmailAsync(SendNotificationRequest request, Guid tenantId)
    {
        var user = await _db.AdminUsers.FindAsync(request.UserId);
        var to = user?.Email ?? $"{request.UserId}@yuktira.com";
        await _emailSender.SendAsync(new EmailMessage
        {
            To = to,
            Subject = request.Title,
            Body = request.Message,
            IsHtml = request.Message.Contains('<') && request.Message.Contains('>'),
            TemplateCode = request.TemplateCode,
            TemplateData = request.TemplateData
        }, tenantId);
    }

    private async Task SendSmsAsync(SendNotificationRequest request, Guid tenantId)
    {
        var user = await _db.AdminUsers.FindAsync(request.UserId);
        var to = user?.UserId ?? request.UserId.ToString();
        await _smsSender.SendAsync(new SmsMessage
        {
            To = to,
            Body = request.Message,
            TemplateCode = request.TemplateCode,
            TemplateData = request.TemplateData
        }, tenantId);
    }
}
