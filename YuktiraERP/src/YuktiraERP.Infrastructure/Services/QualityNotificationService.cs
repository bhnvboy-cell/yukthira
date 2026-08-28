using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IQualityNotificationService
{
    Task<QualityNotificationEntity> CreateAsync(QualityNotificationEntity notification, string userId);
    Task<QualityNotificationEntity?> GetByIdAsync(Guid id);
    Task<QualityNotificationEntity?> GetByNumberAsync(string number);
    Task<List<QualityNotificationEntity>> GetAllAsync(string? status = null, int take = 50);
    Task<QualityNotificationEntity> UpdateAsync(QualityNotificationEntity notification);
    Task<QualityNotificationEntity> AssignDefectAsync(Guid id, string defectLocation, string defectCode, string defectType, string causeCode, string userId);
    Task<QualityNotificationTaskEntity> AddTaskAsync(Guid notificationId, QualityNotificationTaskEntity task);
    Task<QualityNotificationTaskEntity> CompleteTaskAsync(Guid taskId, string completionText, string userId);
    Task<List<QualityNotificationTaskEntity>> GetTasksAsync(Guid notificationId);
    Task<QualityNotificationEntity> SetStatusAsync(Guid id, string status, string userId);
}

public class QualityNotificationService : IQualityNotificationService
{
    private readonly YuktiraDbContext _db;

    public QualityNotificationService(YuktiraDbContext db) => _db = db;

    public async Task<QualityNotificationEntity> CreateAsync(QualityNotificationEntity notification, string userId)
    {
        notification.NotificationNumber = await GenerateNumberAsync();
        notification.CreatedBy = userId;
        notification.Status = "NEW";
        notification.CreatedAt = DateTime.UtcNow;
        _db.QualityNotifications.Add(notification);
        await _db.SaveChangesAsync();
        return notification;
    }

    public async Task<QualityNotificationEntity?> GetByIdAsync(Guid id) =>
        await _db.QualityNotifications.FindAsync(id);

    public async Task<QualityNotificationEntity?> GetByNumberAsync(string number) =>
        await _db.QualityNotifications.FirstOrDefaultAsync(n => n.NotificationNumber == number);

    public async Task<List<QualityNotificationEntity>> GetAllAsync(string? status = null, int take = 50)
    {
        var q = _db.QualityNotifications.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(n => n.Status == status);
        return await q.OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync();
    }

    public async Task<QualityNotificationEntity> UpdateAsync(QualityNotificationEntity notification)
    {
        notification.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return notification;
    }

    public async Task<QualityNotificationEntity> AssignDefectAsync(Guid id, string defectLocation, string defectCode, string defectType, string causeCode, string userId)
    {
        var n = await _db.QualityNotifications.FindAsync(id)
            ?? throw new InvalidOperationException("Quality notification not found");
        n.DefectLocation = defectLocation;
        n.DefectCode = defectCode;
        n.DefectType = defectType;
        n.CauseCode = causeCode;
        n.Status = "IN_PROCESS";
        n.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return n;
    }

    public async Task<QualityNotificationTaskEntity> AddTaskAsync(Guid notificationId, QualityNotificationTaskEntity task)
    {
        var n = await _db.QualityNotifications.FindAsync(notificationId)
            ?? throw new InvalidOperationException("Quality notification not found");
        task.NotificationId = notificationId;
        task.TaskNumber = await GenerateTaskNumberAsync(notificationId);
        task.Status = "OPEN";
        task.CreatedAt = DateTime.UtcNow;
        _db.QualityNotificationTasks.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<QualityNotificationTaskEntity> CompleteTaskAsync(Guid taskId, string completionText, string userId)
    {
        var task = await _db.QualityNotificationTasks.FindAsync(taskId)
            ?? throw new InvalidOperationException("Task not found");
        task.CompletionText = completionText;
        task.Status = "COMPLETED";
        task.CompletedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Check if all tasks for this notification are completed
        var remaining = await _db.QualityNotificationTasks
            .CountAsync(t => t.NotificationId == task.NotificationId && t.Status != "COMPLETED");
        if (remaining == 0)
        {
            var n = await _db.QualityNotifications.FindAsync(task.NotificationId);
            if (n != null)
            {
                n.Status = "COMPLETED";
                n.CompletedAt = DateTime.UtcNow;
                n.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
        return task;
    }

    public async Task<List<QualityNotificationTaskEntity>> GetTasksAsync(Guid notificationId) =>
        await _db.QualityNotificationTasks
            .Where(t => t.NotificationId == notificationId)
            .OrderBy(t => t.TaskNumber)
            .ToListAsync();

    public async Task<QualityNotificationEntity> SetStatusAsync(Guid id, string status, string userId)
    {
        var n = await _db.QualityNotifications.FindAsync(id)
            ?? throw new InvalidOperationException("Quality notification not found");
        n.Status = status;
        n.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return n;
    }

    private async Task<string> GenerateNumberAsync()
    {
        var count = await _db.QualityNotifications.CountAsync();
        return $"Q{(count + 1):D6}";
    }

    private async Task<string> GenerateTaskNumberAsync(Guid notificationId)
    {
        var count = await _db.QualityNotificationTasks.CountAsync(t => t.NotificationId == notificationId);
        return $"T{(count + 1):D3}";
    }
}
