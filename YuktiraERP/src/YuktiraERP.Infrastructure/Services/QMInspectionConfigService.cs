using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IQMInspectionConfigService
{
    Task<QMInspectionConfigEntity> CreateAsync(QMInspectionConfigEntity entity, string userId);
    Task<QMInspectionConfigEntity> UpdateAsync(QMInspectionConfigEntity entity, string userId);
    Task<QMInspectionConfigEntity?> GetByIdAsync(Guid id);
    Task<List<QMInspectionConfigEntity>> GetAllAsync(string? plant = null, string? inspectionType = null, int take = 200);
    Task<bool> DeleteAsync(Guid id);
}

public class QMInspectionConfigService : IQMInspectionConfigService
{
    private readonly YuktiraDbContext _db;
    public QMInspectionConfigService(YuktiraDbContext db) => _db = db;

    public async Task<QMInspectionConfigEntity> CreateAsync(QMInspectionConfigEntity entity, string userId)
    {
        entity.CreatedBy = userId;
        entity.CreatedAt = DateTime.UtcNow;
        _db.QMInspectionConfigs.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<QMInspectionConfigEntity> UpdateAsync(QMInspectionConfigEntity entity, string userId)
    {
        var existing = await _db.QMInspectionConfigs.FindAsync(entity.Id);
        if (existing == null) throw new InvalidOperationException("QM Config not found");
        existing.ConfigName = entity.ConfigName;
        existing.Plant = entity.Plant;
        existing.InspectionType = entity.InspectionType;
        existing.MaterialGroup = entity.MaterialGroup;
        existing.VendorCode = entity.VendorCode;
        existing.VendorName = entity.VendorName;
        existing.CustomerCode = entity.CustomerCode;
        existing.CustomerName = entity.CustomerName;
        existing.BatchNumber = entity.BatchNumber;
        existing.POReference = entity.POReference;
        existing.DeliveryReference = entity.DeliveryReference;
        existing.ProductionOrderReference = entity.ProductionOrderReference;
        existing.SampleSize = entity.SampleSize;
        existing.InspectionLevel = entity.InspectionLevel;
        existing.Status = entity.Status;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<QMInspectionConfigEntity?> GetByIdAsync(Guid id) =>
        await _db.QMInspectionConfigs.FindAsync(id);

    public async Task<List<QMInspectionConfigEntity>> GetAllAsync(string? plant = null, string? inspectionType = null, int take = 200)
    {
        var q = _db.QMInspectionConfigs.AsQueryable();
        if (!string.IsNullOrEmpty(plant)) q = q.Where(x => x.Plant == plant);
        if (!string.IsNullOrEmpty(inspectionType)) q = q.Where(x => x.InspectionType == inspectionType);
        return await q.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var e = await _db.QMInspectionConfigs.FindAsync(id);
        if (e == null) return false;
        _db.QMInspectionConfigs.Remove(e);
        await _db.SaveChangesAsync();
        return true;
    }
}
