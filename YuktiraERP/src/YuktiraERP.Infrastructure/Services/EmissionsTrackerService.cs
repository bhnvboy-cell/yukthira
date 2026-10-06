using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class EmissionsTrackerService : IEmissionsTrackerService
{
    private readonly YuktiraDbContext _db;
    private readonly IAuditService _audit;
    private readonly EmissionsOptions _options;

    public EmissionsTrackerService(YuktiraDbContext db, IAuditService audit, IOptions<EmissionsOptions> options)
    {
        _db = db;
        _audit = audit;
        _options = options?.Value ?? new EmissionsOptions();
    }

    public async Task<EmissionsRecomputeResult> RecomputeAsync(Guid tenantId, string userId, CancellationToken ct)
    {
        var result = new EmissionsRecomputeResult
        {
            Enabled = _options.Enabled,
            ComputedAt = DateTime.UtcNow
        };
        if (!_options.Enabled) return result;

        var factors = await _db.EmissionFactors.AsNoTracking()
            .Where(f => (f.TenantId == tenantId || f.TenantId == Guid.Empty) && f.IsActive)
            .ToListAsync(ct);

        var logs = new List<EmissionLogEntity>();
        var periods = new HashSet<string>(StringComparer.Ordinal);
        var tenantKey = tenantId.ToString();

        var processFactor = ResolveFactor(factors, 1, "Process", null);
        if (processFactor == null)
        {
            result.Skipped.Add("Scope 1 Process (no active emission factor)");
        }
        else
        {
            var orders = await _db.ProductionOrders.AsNoTracking()
                .Where(o => o.TenantId == tenantId && o.YieldQty > 0 && (o.Status == "COMPLETED" || o.Status == "TECO"))
                .ToListAsync(ct);
            foreach (var order in orders)
            {
                var period = (order.ConfirmedAt ?? order.CreatedAt).ToString("yyyy-MM");
                logs.Add(NewLog(tenantId, 1, "Process", "ProductionOrder", order.Id.ToString(),
                    order.MaterialCode, order.YieldQty, processFactor.Unit,
                    Math.Round(order.YieldQty * processFactor.KgCo2ePerUnit, 6),
                    processFactor.KgCo2ePerUnit, period));
                periods.Add(period);
                result.Scope1Count++;
            }
        }

        var energyFactor = ResolveFactor(factors, 2, "Energy", null);
        if (_options.MonthlyEnergyKwh <= 0)
        {
            result.Skipped.Add("Scope 2 Energy (MonthlyEnergyKwh is 0 in configuration)");
        }
        else if (energyFactor == null)
        {
            result.Skipped.Add("Scope 2 Energy (no active emission factor)");
        }
        else
        {
            var period = DateTime.UtcNow.ToString("yyyy-MM");
            logs.Add(NewLog(tenantId, 2, "Energy", "EnergyEstimate", period, "",
                _options.MonthlyEnergyKwh, energyFactor.Unit,
                Math.Round(_options.MonthlyEnergyKwh * energyFactor.KgCo2ePerUnit, 6),
                energyFactor.KgCo2ePerUnit, period));
            periods.Add(period);
            result.Scope2Count++;
        }

        var materialFactor = ResolveFactor(factors, 3, "Material", null);
        if (materialFactor == null)
        {
            result.Skipped.Add("Scope 3 Material (no active emission factor)");
        }
        else
        {
            var headers = await _db.MaterialDocumentHeaders.AsNoTracking()
                .Where(h => h.TenantId == tenantKey)
                .ToListAsync(ct);
            var headerMap = new Dictionary<string, MaterialDocumentHeaderEntity>(StringComparer.Ordinal);
            foreach (var header in headers)
            {
                headerMap[header.Id.ToString()] = header;
            }

            var items = await _db.MaterialDocumentItems.AsNoTracking()
                .Where(i => i.TenantId == tenantKey && i.Quantity > 0 && !i.IsReversal &&
                            (i.MovementType == 101 || i.MovementType == 103 || i.MovementType == 561))
                .ToListAsync(ct);

            foreach (var item in items)
            {
                if (!headerMap.TryGetValue(item.MaterialDocumentHeaderId, out var docHeader)) continue;
                if (docHeader.IsReversal) continue;

                var period = docHeader.PostingDate.ToString("yyyy-MM");
                var referenceId = string.IsNullOrWhiteSpace(docHeader.DocumentNumber)
                    ? docHeader.Id.ToString()
                    : docHeader.DocumentNumber;
                logs.Add(NewLog(tenantId, 3, "Material", "MaterialDocument", referenceId,
                    item.MaterialCode, item.Quantity, materialFactor.Unit,
                    Math.Round(item.Quantity * materialFactor.KgCo2ePerUnit, 6),
                    materialFactor.KgCo2ePerUnit, period));
                periods.Add(period);
                result.Scope3Count++;
            }
        }

        var freightFactor = ResolveFactor(factors, 3, "Freight", null);
        if (freightFactor == null)
        {
            result.Skipped.Add("Scope 3 Freight (no active emission factor)");
        }
        else
        {
            var handlingUnits = await _db.HandlingUnits.AsNoTracking()
                .Where(h => h.TenantId == tenantId)
                .ToListAsync(ct);
            foreach (var handlingUnit in handlingUnits)
            {
                var weightKg = handlingUnit.NetWeight > 0 ? handlingUnit.NetWeight : handlingUnit.GrossWeight;
                if (weightKg <= 0)
                {
                    result.Skipped.Add($"Scope 3 Freight handling unit {handlingUnit.HUNumber} (weight is missing)");
                    continue;
                }

                var tonKm = Math.Round(weightKg / 1000m * _options.DefaultFreightDistanceKm, 4);
                var period = (handlingUnit.PackedAt ?? handlingUnit.CreatedAt).ToString("yyyy-MM");
                logs.Add(NewLog(tenantId, 3, "Freight", "HandlingUnit", handlingUnit.Id.ToString(),
                    handlingUnit.MaterialCode, tonKm, freightFactor.Unit,
                    Math.Round(tonKm * freightFactor.KgCo2ePerUnit, 6),
                    freightFactor.KgCo2ePerUnit, period));
                periods.Add(period);
                result.Scope3Count++;
            }
        }

        var existing = await _db.EmissionLogs
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);
        if (existing.Count > 0) _db.EmissionLogs.RemoveRange(existing);
        if (logs.Count > 0) _db.EmissionLogs.AddRange(logs);
        await _db.SaveChangesAsync(ct);

        result.Inserted = logs.Count;
        result.SkippedSources = result.Skipped.Count;
        result.Periods = periods.OrderBy(p => p, StringComparer.Ordinal).ToList();
        result.ComputedAt = DateTime.UtcNow;
        return result;
    }

    public async Task<EmissionsSummaryDto> GetSummaryAsync(Guid tenantId, string? period = null, CancellationToken ct = default)
    {
        var resolvedPeriod = string.IsNullOrWhiteSpace(period)
            ? DateTime.UtcNow.ToString("yyyy-MM")
            : period!.Trim();

        var logs = await _db.EmissionLogs.AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.Period == resolvedPeriod)
            .ToListAsync(ct);

        var summary = new EmissionsSummaryDto
        {
            Period = resolvedPeriod,
            ComputedAt = DateTime.UtcNow
        };

        foreach (var log in logs)
        {
            if (log.Scope == 1) summary.Scope1Kg += log.KgCo2e;
            else if (log.Scope == 2) summary.Scope2Kg += log.KgCo2e;
            else if (log.Scope == 3) summary.Scope3Kg += log.KgCo2e;
        }

        summary.TotalKg = summary.Scope1Kg + summary.Scope2Kg + summary.Scope3Kg;
        summary.OutputQuantity = logs.Where(l => l.Scope == 1).Sum(l => l.Quantity);
        summary.Unit = logs
            .Where(l => l.Scope == 1 && !string.IsNullOrWhiteSpace(l.Unit))
            .Select(l => l.Unit)
            .FirstOrDefault() ?? "kg";
        summary.PerUnitKg = summary.OutputQuantity == 0
            ? 0m
            : Math.Round(summary.TotalKg / summary.OutputQuantity, 6);
        summary.Scope1Kg = Math.Round(summary.Scope1Kg, 6);
        summary.Scope2Kg = Math.Round(summary.Scope2Kg, 6);
        summary.Scope3Kg = Math.Round(summary.Scope3Kg, 6);
        summary.TotalKg = Math.Round(summary.TotalKg, 6);
        summary.BySourceType = logs
            .GroupBy(l => new { l.Scope, l.SourceType })
            .OrderBy(g => g.Key.Scope)
            .ThenBy(g => g.Key.SourceType, StringComparer.Ordinal)
            .Select(g => new EmissionsSourceTypeDto
            {
                Scope = g.Key.Scope,
                SourceType = g.Key.SourceType,
                Kg = Math.Round(g.Sum(x => x.KgCo2e), 6)
            })
            .ToList();

        return summary;
    }

    public async Task<List<EmissionFactorDto>> GetFactorsAsync(Guid tenantId, int? scope, string? sourceType, CancellationToken ct = default)
    {
        var query = _db.EmissionFactors.AsNoTracking()
            .Where(f => f.TenantId == tenantId || f.TenantId == Guid.Empty);

        if (scope.HasValue)
        {
            var requestedScope = scope.Value;
            query = query.Where(f => f.Scope == requestedScope);
        }
        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            var requestedSource = sourceType.Trim();
            query = query.Where(f => f.SourceType == requestedSource);
        }

        var factors = await query
            .OrderBy(f => f.Scope)
            .ThenBy(f => f.SourceType)
            .ThenByDescending(f => f.EffectiveFrom)
            .ToListAsync(ct);

        return factors.Select(ToDto).ToList();
    }

    public async Task<EmissionFactorDto> UpsertFactorAsync(EmissionFactorDto factor, Guid tenantId, string userId, CancellationToken ct)
    {
        if (factor == null) throw new InvalidOperationException("Emission factor is required");
        if (factor.Scope < 1 || factor.Scope > 3)
            throw new InvalidOperationException("Emission factor scope must be 1, 2 or 3");
        if (string.IsNullOrWhiteSpace(factor.SourceType))
            throw new InvalidOperationException("Emission factor source type is required");

        EmissionFactorEntity entity;
        var isNew = true;

        if (factor.Id != Guid.Empty)
        {
            var existing = await _db.EmissionFactors
                .FirstOrDefaultAsync(f => f.Id == factor.Id, ct);
            if (existing != null && existing.TenantId == tenantId)
            {
                entity = existing;
                isNew = false;
            }
            else if (existing == null)
            {
                entity = new EmissionFactorEntity { Id = factor.Id };
            }
            else
            {
                entity = new EmissionFactorEntity();
            }
        }
        else
        {
            entity = new EmissionFactorEntity();
        }

        entity.TenantId = tenantId;
        entity.Scope = factor.Scope;
        entity.SourceType = factor.SourceType ?? "";
        entity.MaterialCode = factor.MaterialCode ?? "";
        entity.Unit = string.IsNullOrWhiteSpace(factor.Unit) ? "kg" : factor.Unit;
        entity.KgCo2ePerUnit = factor.KgCo2ePerUnit;
        entity.Region = factor.Region ?? "";
        entity.IsActive = factor.IsActive;
        entity.EffectiveFrom = factor.EffectiveFrom;
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew) _db.EmissionFactors.Add(entity);
        await _db.SaveChangesAsync(ct);

        var saved = ToDto(entity);
        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = Guid.TryParse(userId, out var parsedUserId) ? (Guid?)parsedUserId : null,
            TenantId = tenantId,
            ModuleName = "ESG",
            ActionType = isNew ? ActionType.Create : ActionType.Update,
            EntityName = "EmissionFactor",
            EntityId = entity.Id.ToString(),
            NewValue = JsonSerializer.Serialize(saved),
            Details = isNew ? "Emission factor created" : "Emission factor updated"
        });

        return saved;
    }

    public async Task<EmissionLedgerResult> GetLedgerAsync(Guid tenantId, string? period, int? scope, int page, int pageSize, CancellationToken ct = default)
    {
        var resolvedPage = page < 1 ? 1 : page;
        var resolvedSize = pageSize < 1 ? 50 : (pageSize > 500 ? 500 : pageSize);

        var query = _db.EmissionLogs.AsNoTracking()
            .Where(l => l.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(period))
        {
            var requestedPeriod = period.Trim();
            query = query.Where(l => l.Period == requestedPeriod);
        }
        if (scope.HasValue)
        {
            var requestedScope = scope.Value;
            query = query.Where(l => l.Scope == requestedScope);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.ComputedAt)
            .ThenByDescending(l => l.Id)
            .Skip((resolvedPage - 1) * resolvedSize)
            .Take(resolvedSize)
            .ToListAsync(ct);

        return new EmissionLedgerResult
        {
            Items = items.Select(ToLogDto).ToList(),
            TotalCount = totalCount,
            Page = resolvedPage,
            PageSize = resolvedSize
        };
    }

    private static EmissionFactorEntity? ResolveFactor(
        IReadOnlyList<EmissionFactorEntity> factors,
        int scope,
        string sourceType,
        string? materialCode)
    {
        var scopeMatches = factors
            .Where(f => f.Scope == scope &&
                        string.Equals(f.SourceType, sourceType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!string.IsNullOrWhiteSpace(materialCode))
        {
            var specific = scopeMatches
                .Where(f => !string.IsNullOrWhiteSpace(f.MaterialCode) &&
                            string.Equals(f.MaterialCode, materialCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.EffectiveFrom)
                .ThenByDescending(f => f.TenantId != Guid.Empty)
                .FirstOrDefault();
            if (specific != null) return specific;
        }

        return scopeMatches
            .Where(f => string.IsNullOrWhiteSpace(f.MaterialCode))
            .OrderByDescending(f => f.EffectiveFrom)
            .ThenByDescending(f => f.TenantId != Guid.Empty)
            .FirstOrDefault();
    }

    private static EmissionLogEntity NewLog(
        Guid tenantId,
        int scope,
        string sourceType,
        string referenceType,
        string referenceId,
        string materialCode,
        decimal quantity,
        string unit,
        decimal kgCo2e,
        decimal kgCo2ePerUnit,
        string period)
    {
        return new EmissionLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = scope,
            SourceType = sourceType,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            MaterialCode = materialCode,
            Quantity = quantity,
            Unit = unit,
            KgCo2e = kgCo2e,
            KgCo2ePerUnit = kgCo2ePerUnit,
            Period = period,
            ComputedAt = DateTime.UtcNow
        };
    }

    private static EmissionFactorDto ToDto(EmissionFactorEntity entity)
    {
        return new EmissionFactorDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Scope = entity.Scope,
            SourceType = entity.SourceType,
            MaterialCode = entity.MaterialCode,
            Unit = entity.Unit,
            KgCo2ePerUnit = entity.KgCo2ePerUnit,
            Region = entity.Region,
            IsActive = entity.IsActive,
            EffectiveFrom = entity.EffectiveFrom
        };
    }

    private static EmissionLogDto ToLogDto(EmissionLogEntity entity)
    {
        return new EmissionLogDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Scope = entity.Scope,
            SourceType = entity.SourceType,
            ReferenceType = entity.ReferenceType,
            ReferenceId = entity.ReferenceId,
            MaterialCode = entity.MaterialCode,
            Quantity = entity.Quantity,
            Unit = entity.Unit,
            KgCo2e = entity.KgCo2e,
            KgCo2ePerUnit = entity.KgCo2ePerUnit,
            Period = entity.Period,
            ComputedAt = entity.ComputedAt
        };
    }
}
