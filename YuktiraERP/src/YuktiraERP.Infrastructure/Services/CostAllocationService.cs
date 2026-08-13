using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class CostAllocationService : ICostAllocationService
{
    private readonly YuktiraDbContext _db;

    public CostAllocationService(YuktiraDbContext db) { _db = db; }

    public async Task<List<CostAllocationRuleDto>> GetRulesAsync(Guid tenantId)
    {
        return await _db.CostAllocationRules
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .Select(r => new CostAllocationRuleDto
            {
                Id = r.Id,
                Name = r.Name,
                CostElementCode = r.CostElementCode,
                AllocationType = r.AllocationType,
                Basis = r.Basis,
                IsActive = r.IsActive
            })
            .ToListAsync();
    }

    public async Task<CostAllocationRuleDto> CreateRuleAsync(Guid tenantId, CostAllocationRuleDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Rule name is required");
        if (string.IsNullOrWhiteSpace(request.CostElementCode))
            throw new InvalidOperationException("Cost element is required");

        var exists = await _db.CostAllocationRules.AnyAsync(r => r.TenantId == tenantId && r.Name == request.Name);
        if (exists)
            throw new InvalidOperationException($"Rule '{request.Name}' already exists");

        var entity = new CostAllocationRuleEntity
        {
            TenantId = tenantId,
            Name = request.Name,
            CostElementCode = request.CostElementCode,
            AllocationType = request.AllocationType,
            Basis = request.Basis,
            IsActive = request.IsActive
        };
        _db.CostAllocationRules.Add(entity);
        await _db.SaveChangesAsync();

        return new CostAllocationRuleDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CostElementCode = entity.CostElementCode,
            AllocationType = entity.AllocationType,
            Basis = entity.Basis,
            IsActive = entity.IsActive
        };
    }

    public async Task<CostAllocationRuleDto?> UpdateRuleAsync(Guid tenantId, Guid id, CostAllocationRuleDto request)
    {
        var entity = await _db.CostAllocationRules.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == id);
        if (entity == null) return null;

        entity.Name = request.Name;
        entity.CostElementCode = request.CostElementCode;
        entity.AllocationType = request.AllocationType;
        entity.Basis = request.Basis;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new CostAllocationRuleDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CostElementCode = entity.CostElementCode,
            AllocationType = entity.AllocationType,
            Basis = entity.Basis,
            IsActive = entity.IsActive
        };
    }

    public async Task DeleteRuleAsync(Guid tenantId, Guid id)
    {
        var entity = await _db.CostAllocationRules.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == id);
        if (entity == null) return;
        _db.CostAllocationRules.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<CostAllocationRunDto> RunAllocationAsync(Guid tenantId, CostAllocationRunRequest request, string createdBy)
    {
        var period = string.IsNullOrEmpty(request.Period)
            ? $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month:D2}"
            : request.Period;
        if (request.TotalAmount < 0)
            throw new InvalidOperationException("Total amount cannot be negative");
        if (request.BasisValues.Count == 0)
            throw new InvalidOperationException("At least one cost center basis value is required");

        var totalBasis = request.BasisValues.Sum(b => b.BasisValue);
        if (totalBasis <= 0)
            throw new InvalidOperationException("Total basis value must be greater than zero");

        var run = new CostAllocationRunEntity
        {
            TenantId = tenantId,
            Period = period,
            TotalAllocated = request.TotalAmount,
            Status = "Completed",
            RunAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        _db.CostAllocationRuns.Add(run);
        await _db.SaveChangesAsync();

        foreach (var basis in request.BasisValues.OrderByDescending(b => b.BasisValue))
        {
            var sharePercent = basis.BasisValue / totalBasis * 100m;
            var amount = request.TotalAmount * basis.BasisValue / totalBasis;
            _db.CostAllocationDetails.Add(new CostAllocationDetailEntity
            {
                TenantId = tenantId,
                RunId = run.Id,
                CostCenterCode = basis.CostCenterCode,
                CostCenterName = basis.CostCenterName,
                CostElementCode = request.CostElementCode,
                Amount = amount,
                SharePercent = sharePercent,
                Basis = request.Basis
            });
        }
        await _db.SaveChangesAsync();

        return new CostAllocationRunDto
        {
            Id = run.Id,
            Period = run.Period,
            TotalAllocated = run.TotalAllocated,
            Status = run.Status,
            RunAt = run.RunAt,
            CreatedBy = run.CreatedBy
        };
    }

    public async Task<List<CostAllocationRunDto>> GetRunsAsync(Guid tenantId, int limit = 50)
    {
        return await _db.CostAllocationRuns
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.RunAt)
            .Take(limit)
            .Select(r => new CostAllocationRunDto
            {
                Id = r.Id,
                Period = r.Period,
                TotalAllocated = r.TotalAllocated,
                Status = r.Status,
                RunAt = r.RunAt,
                CreatedBy = r.CreatedBy
            })
            .ToListAsync();
    }

    public async Task<List<CostAllocationDetailDto>> GetRunDetailsAsync(Guid tenantId, Guid runId)
    {
        return await _db.CostAllocationDetails
            .Where(d => d.TenantId == tenantId && d.RunId == runId)
            .OrderByDescending(d => d.Amount)
            .Select(d => new CostAllocationDetailDto
            {
                Id = d.Id,
                RunId = d.RunId,
                CostCenterCode = d.CostCenterCode,
                CostCenterName = d.CostCenterName,
                CostElementCode = d.CostElementCode,
                Amount = d.Amount,
                SharePercent = d.SharePercent,
                Basis = d.Basis
            })
            .ToListAsync();
    }

    public async Task<List<CostCenterUtilizationDto>> GetUtilizationAsync(Guid tenantId, Guid runId)
    {
        var details = await _db.CostAllocationDetails
            .Where(d => d.TenantId == tenantId && d.RunId == runId)
            .ToListAsync();
        var centers = await _db.CostCenters.ToListAsync();

        var result = new List<CostCenterUtilizationDto>();
        foreach (var center in centers)
        {
            var allocated = details.Where(d => d.CostCenterCode == center.Code).Sum(d => d.Amount);
            var budget = center.PlannedBudget;
            result.Add(new CostCenterUtilizationDto
            {
                CostCenterCode = center.Code,
                CostCenterName = center.Name,
                PlannedBudget = budget,
                Allocated = allocated,
                UtilizationPercent = budget > 0 ? allocated / budget * 100m : (allocated > 0 ? 100m : 0)
            });
        }

        // Add any allocated centers not yet registered
        var knownCodes = centers.Select(c => c.Code).ToHashSet();
        foreach (var group in details.GroupBy(d => d.CostCenterCode))
        {
            if (knownCodes.Contains(group.Key)) continue;
            result.Add(new CostCenterUtilizationDto
            {
                CostCenterCode = group.Key,
                CostCenterName = group.First().CostCenterName,
                PlannedBudget = 0,
                Allocated = group.Sum(d => d.Amount),
                UtilizationPercent = 100
            });
        }

        return result.OrderByDescending(u => u.Allocated).ToList();
    }
}