using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IReleaseStrategyService
{
    Task<ReleaseStrategyEntity?> FindMatchingStrategyAsync(string documentType, decimal amount, string plant, string departmentKey);
    Task<List<ReleaseCodeEntity>> GetReleaseCodesAsync(Guid strategyId);
    Task<bool> ExecuteReleaseStrategyAsync(Guid documentId, string documentType, string userId);
}

public class ReleaseStrategyService : IReleaseStrategyService
{
    private readonly YuktiraDbContext _db;
    public ReleaseStrategyService(YuktiraDbContext db) { _db = db; }

    public async Task<ReleaseStrategyEntity?> FindMatchingStrategyAsync(string documentType, decimal amount, string plant, string departmentKey)
    {
        return await _db.ReleaseStrategies.FirstOrDefaultAsync(s =>
            s.IsActive &&
            s.DocumentType == documentType &&
            amount >= s.MinAmount &&
            amount <= s.MaxAmount &&
            (string.IsNullOrEmpty(s.Plant) || s.Plant == plant) &&
            (string.IsNullOrEmpty(s.DepartmentKey) || s.DepartmentKey == departmentKey));
    }

    public async Task<List<ReleaseCodeEntity>> GetReleaseCodesAsync(Guid strategyId)
    {
        return await _db.ReleaseCodes
            .Where(rc => rc.ReleaseStrategyId == strategyId)
            .OrderBy(rc => rc.Level)
            .ToListAsync();
    }

    public async Task<bool> ExecuteReleaseStrategyAsync(Guid documentId, string documentType, string userId)
    {
        var releaseCodes = await _db.ReleaseCodes.Where(rc => rc.IsRequired).ToListAsync();
        if (releaseCodes.Count == 0) return true;

        var userHasAuthority = releaseCodes.Any(rc =>
            rc.ApproverUserId == userId ||
            rc.ApproverRole == "ADMIN" ||
            rc.ApproverRole.Equals(userId, StringComparison.OrdinalIgnoreCase));
        return userHasAuthority;
    }
}
