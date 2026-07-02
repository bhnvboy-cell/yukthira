using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Data;
public class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly YuktiraDbContext _context;
    protected readonly DbSet<TEntity> _set;
    public EfRepository(YuktiraDbContext context) { _context = context; _set = context.Set<TEntity>(); }
    public Task<List<TEntity>> GetAllAsync() => _set.AsNoTracking().ToListAsync();
    public Task<TEntity?> GetByIdAsync(TKey id) => _set.FindAsync(id).AsTask();
    public Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) => _set.AsNoTracking().Where(predicate).ToListAsync();
    public async Task AddAsync(TEntity entity) { await _set.AddAsync(entity); }
    public async Task UpdateAsync(TEntity entity) { _set.Update(entity); }
    public async Task DeleteAsync(TKey id) { var e = await _set.FindAsync(id); if (e != null) _set.Remove(e); }
    public Task<int> CountAsync() => _set.CountAsync();
}
