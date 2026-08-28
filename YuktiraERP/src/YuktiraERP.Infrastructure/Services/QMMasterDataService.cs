using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IQMMasterDataService
{
    Task<QMMasterDataEntity> CreateAsync(QMMasterDataEntity entity, string userId);
    Task<QMMasterDataEntity> UpdateAsync(QMMasterDataEntity entity, string userId);
    Task<QMMasterDataEntity?> GetByIdAsync(Guid id);
    Task<List<QMMasterDataEntity>> GetAllAsync(string? plant = null, string? material = null, int take = 200);
    Task<bool> DeleteAsync(Guid id);
}

public class QMMasterDataService : IQMMasterDataService
{
    private readonly YuktiraDbContext _db;
    public QMMasterDataService(YuktiraDbContext db) => _db = db;

    public async Task<QMMasterDataEntity> CreateAsync(QMMasterDataEntity entity, string userId)
    {
        entity.CreatedBy = userId;
        entity.CreatedAt = DateTime.UtcNow;
        _db.QMMasterDatas.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<QMMasterDataEntity> UpdateAsync(QMMasterDataEntity entity, string userId)
    {
        var existing = await _db.QMMasterDatas.FindAsync(entity.Id);
        if (existing == null) throw new InvalidOperationException("QM Master Data not found");
        existing.MaterialCode = entity.MaterialCode;
        existing.MaterialName = entity.MaterialName;
        existing.Plant = entity.Plant;
        existing.InspectionType = entity.InspectionType;
        existing.InspectionLotOrigin = entity.InspectionLotOrigin;
        existing.InspectionScope = entity.InspectionScope;
        existing.InspectionProcedure = entity.InspectionProcedure;
        existing.SampleProcedure = entity.SampleProcedure;
        existing.DynModificationKey = entity.DynModificationKey;
        existing.QMControlKey = entity.QMControlKey;
        existing.CatalogType = entity.CatalogType;
        existing.DefectCatalog = entity.DefectCatalog;
        existing.DefectCodeGroup = entity.DefectCodeGroup;
        existing.UDCatalog = entity.UDCatalog;
        existing.UDCodeGroup = entity.UDCodeGroup;
        existing.Frequency = entity.Frequency;
        existing.FrequencyUnit = entity.FrequencyUnit;
        existing.IsActive = entity.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<QMMasterDataEntity?> GetByIdAsync(Guid id) =>
        await _db.QMMasterDatas.FindAsync(id);

    public async Task<List<QMMasterDataEntity>> GetAllAsync(string? plant = null, string? material = null, int take = 200)
    {
        var q = _db.QMMasterDatas.AsQueryable();
        if (!string.IsNullOrEmpty(plant)) q = q.Where(x => x.Plant == plant);
        if (!string.IsNullOrEmpty(material)) q = q.Where(x => x.MaterialCode.Contains(material));
        return await q.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var e = await _db.QMMasterDatas.FindAsync(id);
        if (e == null) return false;
        _db.QMMasterDatas.Remove(e);
        await _db.SaveChangesAsync();
        return true;
    }
}
