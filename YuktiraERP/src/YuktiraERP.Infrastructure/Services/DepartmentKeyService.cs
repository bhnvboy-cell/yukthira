using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IDepartmentKeyService
{
    Task<List<DepartmentKeyEntity>> GetAllAsync(Guid tenantId);
    Task<DepartmentKeyEntity?> GetByCodeAsync(string code, Guid tenantId);
    Task<DepartmentKeyEntity> CreateAsync(DepartmentKeyEntity entity);
}

public class DepartmentKeyService : IDepartmentKeyService
{
    private readonly YuktiraDbContext _db;
    public DepartmentKeyService(YuktiraDbContext db) { _db = db; }

    public async Task<List<DepartmentKeyEntity>> GetAllAsync(Guid tenantId)
        => await _db.DepartmentKeys.Where(d => d.TenantId == tenantId && d.IsActive).ToListAsync();

    public async Task<DepartmentKeyEntity?> GetByCodeAsync(string code, Guid tenantId)
        => await _db.DepartmentKeys.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Code == code);

    public async Task<DepartmentKeyEntity> CreateAsync(DepartmentKeyEntity entity)
    {
        _db.DepartmentKeys.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }
}
