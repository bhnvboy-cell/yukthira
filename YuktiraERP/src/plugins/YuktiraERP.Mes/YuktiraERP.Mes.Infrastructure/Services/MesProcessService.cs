using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesProcessService
{
    private readonly MesDbContext _db;

    public MesProcessService(MesDbContext db) => _db = db;

    public async Task<MesRecipeDownload> DownloadRecipeToResourceAsync(
        Guid tenantId, Guid recipeId, Guid resourceId, string sentBy, CancellationToken ct = default)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId && r.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Recipe {recipeId} not found.");

        if (recipe.Status != RecipeStatus.APPROVED && recipe.Status != RecipeStatus.EFFECTIVE)
            throw new InvalidOperationException($"Recipe {recipe.Version} is {recipe.Status}; only APPROVED/EFFECTIVE recipes may be downloaded.");

        if (recipe.DocRevId.HasValue)
        {
            var rev = await _db.DocumentRevisions.FirstOrDefaultAsync(r => r.Id == recipe.DocRevId.Value && r.TenantId == tenantId, ct);
            if (rev == null || rev.Status != DocRevisionStatus.EFFECTIVE)
                throw new InvalidOperationException("Recipe download blocked: linked document revision is not EFFECTIVE.");
        }

        var now = DateTime.UtcNow;
        var checksum = RecordHashHelper.ComputeSha256(recipe.Id, recipe.Version, recipe.MaterialId);
        var download = new MesRecipeDownload
        {
            TenantId = tenantId,
            PlantCode = recipe.PlantCode,
            RecipeId = recipe.Id,
            Version = recipe.Version,
            ResourceId = resourceId,
            SentAt = now,
            Checksum = checksum,
            AckStatus = "ACKED",
            SentBy = sentBy
        };
        download.RecordHash = RecordHashHelper.ComputeSha256(download.Id, recipe.Id, resourceId, checksum, sentBy, now);
        _db.RecipeDownloads.Add(download);
        await _db.SaveChangesAsync(ct);
        return download;
    }

    public async Task<MesAlarm> RaiseAlarmAsync(
        Guid tenantId, string plantCode, Guid tagId, Guid resourceId, int severity, string? comment = null, CancellationToken ct = default)
    {
        var alarm = new MesAlarm
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            TagId = tagId,
            ResourceId = resourceId,
            Severity = Math.Clamp(severity, 1, 4),
            State = AlarmState.ACTIVE,
            RaisedAt = DateTime.UtcNow,
            Comment = comment
        };
        _db.Alarms.Add(alarm);
        await _db.SaveChangesAsync(ct);
        return alarm;
    }

    public async Task<MesAlarm> AcknowledgeAlarmAsync(Guid tenantId, Guid alarmId, string ackedBy, string? comment = null, CancellationToken ct = default)
    {
        var alarm = await _db.Alarms.FirstOrDefaultAsync(a => a.Id == alarmId && a.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Alarm {alarmId} not found.");
        alarm.State = AlarmState.ACKED;
        alarm.AckedAt = DateTime.UtcNow;
        alarm.AckedBy = ackedBy;
        if (!string.IsNullOrWhiteSpace(comment))
            alarm.Comment = comment;
        await _db.SaveChangesAsync(ct);
        return alarm;
    }

    public async Task<MesInterlock> BypassInterlockAsync(
        Guid tenantId, Guid interlockId, string supervisorUser, TimeSpan duration, Guid esignId, CancellationToken ct = default)
    {
        var sigExists = await _db.ESignatures.AnyAsync(s => s.Id == esignId && s.TenantId == tenantId, ct);
        if (!sigExists)
            throw new InvalidOperationException("Interlock bypass requires a valid supervisor e-signature.");

        var interlock = await _db.Interlocks.FirstOrDefaultAsync(i => i.Id == interlockId && i.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Interlock {interlockId} not found.");

        interlock.Status = "BYPASSED";
        interlock.BypassBy = supervisorUser;
        interlock.BypassUntil = DateTime.UtcNow.Add(duration);
        await _db.SaveChangesAsync(ct);
        return interlock;
    }
}
