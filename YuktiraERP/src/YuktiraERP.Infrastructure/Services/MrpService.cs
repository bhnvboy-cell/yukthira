using System.Diagnostics;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace YuktiraERP.Infrastructure.Services;

public class MrpService : IMrpService
{
    private readonly YuktiraDbContext _db;
    private readonly ICapacityPlanningService _capacityService;

    public MrpService(YuktiraDbContext db, ICapacityPlanningService capacityService)
    {
        _db = db;
        _capacityService = capacityService;
    }

    public async Task<List<MrpSuggestionDto>> RunMrpAsync(Guid tenantId, Guid? materialId = null)
    {
        var sw = Stopwatch.StartNew();
        var materials = await _db.Set<MaterialMasterEntity>().ToListAsync();
        var stockItems = await _db.Set<StockItemEntity>().ToListAsync();
        var salesOrders = await _db.Set<SalesOrderEntity>()
            .Where(s => s.Status == "Confirmed" || s.Status == "Completed").ToListAsync();
        var demandData = await BuildDemandDictionaryAsync(materials, salesOrders);
        var vendors = await _db.Set<VendorEntity>().Where(v => v.Status == "Active").ToListAsync();
        var vendorLeadTimes = await _db.Set<VendorLeadTimeEntity>().ToListAsync();
        var result = new List<MrpSuggestionDto>();
        var exceptions = new List<MrpExceptionMessageEntity>();

        foreach (var mat in materials.Where(m => materialId == null || m.Id == materialId.Value))
        {
            var stock = stockItems.Where(s => s.MaterialName == mat.Name).Sum(s => s.Quantity);
            var demand = await CalculateTotalDemandAsync(mat.Name);
            var openPo = await GetOpenPurchaseOrdersAsync(mat.Name);
            var demandHistory = demandData.TryGetValue(mat.Name, out var dh) ? dh : new List<decimal> { 100 };
            var safetyStock = CalculateStatisticalSafetyStock(demandHistory, 0.95);
            var shortage = Math.Max(0, demand + safetyStock - stock - openPo);

            if (shortage > 0 || demand > 0)
            {
                var suggestionType = mat.Type is "RAW" or "PACKAGING" or "CONSUMABLE" ? "PURCHASE" : "PRODUCE";
                result.Add(new MrpSuggestionDto
                {
                    MaterialId = Guid.NewGuid(),
                    MaterialCode = mat.Code,
                    MaterialName = mat.Name,
                    CurrentStock = stock,
                    OpenPoQty = openPo,
                    TotalDemand = demand,
                    ShortageQty = shortage,
                    SuggestionType = suggestionType,
                    SuggestedQty = shortage > 0 ? shortage : 0
                });

                if (shortage > 0)
                {
                    exceptions.Add(new MrpExceptionMessageEntity
                    {
                        TenantId = tenantId,
                        MaterialCode = mat.Code,
                        MaterialName = mat.Name,
                        ExceptionType = "STOCK_SHORTAGE",
                        Message = $"Stock shortage of {shortage} for {mat.Name}",
                        Severity = "Error",
                        SuggestedAction = $"Create purchase/production order for {shortage} units"
                    });
                }

                if (suggestionType == "PURCHASE")
                {
                    var hasVendor = vendors.Any(v =>
                        vendorLeadTimes.Any(vlt => vlt.VendorId == v.Id && vlt.MaterialCode == mat.Code));
                    if (!hasVendor)
                    {
                        exceptions.Add(new MrpExceptionMessageEntity
                        {
                            TenantId = tenantId,
                            MaterialCode = mat.Code,
                            MaterialName = mat.Name,
                            ExceptionType = "NO_VENDOR",
                            Message = $"No vendor assigned for material {mat.Name}",
                            Severity = "Warning",
                            SuggestedAction = "Assign a vendor to this material in vendor lead time configuration"
                        });
                    }

                    var longLead = vendorLeadTimes
                        .Where(vlt => vlt.MaterialCode == mat.Code && vlt.LeadTimeDays > 30)
                        .ToList();
                    foreach (var lt in longLead)
                    {
                        exceptions.Add(new MrpExceptionMessageEntity
                        {
                            TenantId = tenantId,
                            MaterialCode = mat.Code,
                            MaterialName = mat.Name,
                            ExceptionType = "LONG_LEAD_TIME",
                            Message = $"Vendor lead time of {lt.LeadTimeDays} days exceeds 30-day threshold for {mat.Name}",
                            Severity = "Warning",
                            SuggestedAction = "Consider alternative vendor with shorter lead time or increase safety stock"
                        });
                    }
                }
            }
        }

        sw.Stop();

        var history = new MrpRunHistoryEntity
        {
            TenantId = tenantId,
            RunType = "Standard",
            RunAt = DateTime.UtcNow,
            Status = "Completed",
            MaterialsProcessed = materials.Count,
            SuggestionsGenerated = result.Count,
            ExceptionMessages = exceptions.Count,
            DurationMs = sw.ElapsedMilliseconds
        };
        _db.Set<MrpRunHistoryEntity>().Add(history);

        foreach (var ex in exceptions)
        {
            ex.RunHistoryId = history.Id;
        }
        _db.Set<MrpExceptionMessageEntity>().AddRange(exceptions);
        await _db.SaveChangesAsync();

        return result;
    }

    public Task RefreshStockViewAsync() => Task.CompletedTask;

    public async Task<List<MrpSuggestionDto>> GetShortageAlertsAsync(Guid tenantId)
    {
        var materials = await _db.Set<MaterialMasterEntity>().ToListAsync();
        var stockItems = await _db.Set<StockItemEntity>().ToListAsync();
        var alerts = new List<MrpSuggestionDto>();

        foreach (var mat in materials)
        {
            var stock = stockItems.Where(s => s.MaterialName == mat.Name).Sum(s => s.Quantity);
            var minStock = stockItems.Where(s => s.MaterialName == mat.Name).Sum(s => s.MinStock);
            if (stock < minStock && minStock > 0)
            {
                alerts.Add(new MrpSuggestionDto
                {
                    MaterialId = Guid.NewGuid(),
                    MaterialCode = mat.Code,
                    MaterialName = mat.Name,
                    CurrentStock = stock,
                    TotalDemand = 0,
                    ShortageQty = minStock - stock,
                    SuggestionType = "REORDER",
                    SuggestedQty = minStock - stock
                });
            }
        }
        return alerts;
    }

    public async Task<List<MrpExplosionResult>> ExplodeBomAsync(MrpRunRequest request)
    {
        var results = new List<MrpExplosionResult>();
        var boms = await _db.Set<BillOfMaterialEntity>().Where(b => b.Status == "Active").ToListAsync();
        var materials = await _db.Set<MaterialMasterEntity>().ToListAsync();
        var productName = request.ProductName ?? "";
        var quantity = request.Quantity;

        var exploded = await MultiLevelExplosionAsync(Guid.Empty, productName, quantity);
        var grouped = exploded.GroupBy(r => r.ParentProduct);

        foreach (var group in grouped)
        {
            results.Add(new MrpExplosionResult
            {
                ProductName = group.Key,
                PlanQuantity = quantity,
                StartDate = request.StartDate ?? DateTime.Today,
                EndDate = request.EndDate ?? DateTime.Today.AddDays(14),
                Requirements = group.ToList(),
                BomLevel = group.Min(r => r.Level)
            });
        }
        return results;
    }

    public async Task<List<MrpRequirementDto>> CalculateNetRequirementsAsync(Guid tenantId, string productName, decimal quantity, DateTime needByDate)
    {
        return await MultiLevelExplosionAsync(tenantId, productName, quantity);
    }

    public async Task<List<MrpRequirementDto>> MultiLevelExplosionAsync(Guid tenantId, string productName, decimal quantity, int maxLevel = 10)
    {
        var requirements = new List<MrpRequirementDto>();
        var boms = await _db.Set<BillOfMaterialEntity>().Where(b => b.Status == "Active").ToListAsync();
        var materials = await _db.Set<MaterialMasterEntity>().ToListAsync();
        var stockItems = await _db.Set<StockItemEntity>().ToListAsync();
        var salesOrders = await _db.Set<SalesOrderEntity>()
            .Where(s => s.Status == "Confirmed" || s.Status == "Completed").ToListAsync();
        var demandData = await BuildDemandDictionaryAsync(materials, salesOrders);

        await ExplodeLevelAsync(productName, quantity, 0, boms, materials, stockItems, demandData, requirements, maxLevel);
        return requirements;
    }

    public async Task<List<MrpRunHistoryDto>> GetRunHistoryAsync(Guid tenantId, int limit = 20)
    {
        return await _db.Set<MrpRunHistoryEntity>()
            .Where(h => h.TenantId == tenantId)
            .OrderByDescending(h => h.RunAt)
            .Take(limit)
            .Select(h => new MrpRunHistoryDto
            {
                Id = h.Id,
                RunAt = h.RunAt,
                Status = h.Status,
                MaterialsProcessed = h.MaterialsProcessed,
                SuggestionsGenerated = h.SuggestionsGenerated,
                DurationMs = h.DurationMs,
                Exceptions = _db.Set<MrpExceptionMessageEntity>()
                    .Where(e => e.RunHistoryId == h.Id)
                    .Select(e => new MrpExceptionMessageDto
                    {
                        Id = e.Id,
                        MaterialCode = e.MaterialCode,
                        MaterialName = e.MaterialName,
                        ExceptionType = e.ExceptionType,
                        Message = e.Message,
                        Severity = e.Severity,
                        SuggestedAction = e.SuggestedAction,
                        CreatedAt = e.CreatedAt
                    }).ToList()
            })
            .ToListAsync();
    }

    public async Task<List<MrpExceptionMessageDto>> GetExceptionMessagesAsync(Guid tenantId, Guid? runHistoryId = null)
    {
        var query = _db.Set<MrpExceptionMessageEntity>()
            .Where(e => e.TenantId == tenantId);

        if (runHistoryId.HasValue)
            query = query.Where(e => e.RunHistoryId == runHistoryId.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new MrpExceptionMessageDto
            {
                Id = e.Id,
                MaterialCode = e.MaterialCode,
                MaterialName = e.MaterialName,
                ExceptionType = e.ExceptionType,
                Message = e.Message,
                Severity = e.Severity,
                SuggestedAction = e.SuggestedAction,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<MrpSuggestionDto>> RunMrpMultiPlantAsync(Guid tenantId, Guid? plantId = null)
    {
        var sw = Stopwatch.StartNew();
        var materials = await _db.Set<MaterialMasterEntity>().ToListAsync();
        var stockItems = await _db.Set<StockItemEntity>().ToListAsync();
        var salesOrders = await _db.Set<SalesOrderEntity>()
            .Where(s => s.Status == "Confirmed" || s.Status == "Completed").ToListAsync();
        var demandData = await BuildDemandDictionaryAsync(materials, salesOrders);
        var plants = await _db.Set<PlantEntity>()
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .ToListAsync();

        if (plantId.HasValue)
            plants = plants.Where(p => p.Id == plantId.Value).ToList();

        var vendors = await _db.Set<VendorEntity>().Where(v => v.Status == "Active").ToListAsync();
        var vendorLeadTimes = await _db.Set<VendorLeadTimeEntity>().ToListAsync();
        var result = new List<MrpSuggestionDto>();
        var exceptions = new List<MrpExceptionMessageEntity>();

        foreach (var mat in materials)
        {
            var plantCodes = plants.Select(p => p.Code).ToHashSet();
            if (plantCodes.Count == 0) continue;

            var stock = stockItems.Where(s => s.MaterialName == mat.Name).Sum(s => s.Quantity);
            var demand = await CalculateTotalDemandAsync(mat.Name);
            var openPo = await GetOpenPurchaseOrdersAsync(mat.Name);
            var demandHistory = demandData.TryGetValue(mat.Name, out var dh) ? dh : new List<decimal> { 100 };
            var safetyStock = CalculateStatisticalSafetyStock(demandHistory, 0.95);
            var shortage = Math.Max(0, demand + safetyStock - stock - openPo);

            if (shortage > 0 || demand > 0)
            {
                var suggestionType = mat.Type is "RAW" or "PACKAGING" or "CONSUMABLE" ? "PURCHASE" : "PRODUCE";
                var plantInfo = string.Join(", ", plants.Select(p => $"{p.Code}:{p.Name}"));

                result.Add(new MrpSuggestionDto
                {
                    MaterialId = Guid.NewGuid(),
                    MaterialCode = mat.Code,
                    MaterialName = mat.Name + $" [{plantInfo}]",
                    CurrentStock = stock,
                    OpenPoQty = openPo,
                    TotalDemand = demand,
                    ShortageQty = shortage,
                    SuggestionType = suggestionType,
                    SuggestedQty = shortage > 0 ? shortage : 0
                });

                if (shortage > 0)
                {
                    exceptions.Add(new MrpExceptionMessageEntity
                    {
                        TenantId = tenantId,
                        MaterialCode = mat.Code,
                        MaterialName = mat.Name,
                        ExceptionType = "STOCK_SHORTAGE",
                        Message = $"Multi-plant stock shortage of {shortage} for {mat.Name}",
                        Severity = "Error",
                        SuggestedAction = "Review plant stock transfers or create procurement"
                    });
                }
            }
        }

        sw.Stop();

        var history = new MrpRunHistoryEntity
        {
            TenantId = tenantId,
            RunType = "MultiPlant",
            RunAt = DateTime.UtcNow,
            Status = "Completed",
            MaterialsProcessed = materials.Count,
            SuggestionsGenerated = result.Count,
            ExceptionMessages = exceptions.Count,
            DurationMs = sw.ElapsedMilliseconds
        };
        _db.Set<MrpRunHistoryEntity>().Add(history);

        foreach (var ex in exceptions)
        {
            ex.RunHistoryId = history.Id;
        }
        _db.Set<MrpExceptionMessageEntity>().AddRange(exceptions);
        await _db.SaveChangesAsync();

        return result;
    }

    public async Task<List<MrpCapacityPlanDto>> CalculateCapacityLevelingAsync(Guid tenantId, DateTime start, DateTime end)
    {
        var loads = await _capacityService.CalculateLoadAsync(tenantId, start, end);
        var workCenters = await _db.Set<WorkCenterEntity>().ToListAsync();
        var result = new List<MrpCapacityPlanDto>();

        foreach (var load in loads)
        {
            var wc = workCenters.FirstOrDefault(w => w.Code == load.Code);
            string suggestion;

            if (load.LoadPercent > 100)
            {
                var overtimeHours = load.RequiredHours - load.AvailableHours;
                var extraShifts = (int)Math.Ceiling(overtimeHours / 8);
                suggestion = $"OVERLOADED: Add {extraShifts} extra shift(s) or {overtimeHours:F1} hours overtime";
            }
            else if (load.LoadPercent > 85)
            {
                suggestion = "NEAR_CAPACITY: Monitor closely, consider partial overtime";
            }
            else if (load.LoadPercent < 50)
            {
                suggestion = "UNDERUTILIZED: Consider reallocating resources or reducing shifts";
            }
            else
            {
                suggestion = "BALANCED: Capacity within normal range";
            }

            result.Add(new MrpCapacityPlanDto
            {
                WorkCenterCode = load.Code,
                WorkCenterName = wc?.Name ?? load.Code,
                AvailableHours = load.AvailableHours,
                RequiredHours = load.RequiredHours,
                LoadPercent = load.LoadPercent,
                LevelingSuggestion = suggestion,
                Operations = load.Operations
            });
        }

        var levelEntities = result.Select(r => new MrpCapacityLevelEntity
        {
            TenantId = tenantId,
            WorkCenterCode = r.WorkCenterCode,
            AvailableHours = r.AvailableHours,
            RequiredHours = r.RequiredHours,
            LoadPercent = r.LoadPercent,
            LevelingSuggestion = r.LevelingSuggestion
        }).ToList();

        _db.Set<MrpCapacityLevelEntity>().AddRange(levelEntities);
        await _db.SaveChangesAsync();

        return result;
    }

    public async Task<List<MrpSuggestionDto>> RunMrpWithVendorLeadTimeAsync(Guid tenantId, Guid? plantId = null)
    {
        var sw = Stopwatch.StartNew();
        var materials = await _db.Set<MaterialMasterEntity>().ToListAsync();
        var stockItems = await _db.Set<StockItemEntity>().ToListAsync();
        var salesOrders = await _db.Set<SalesOrderEntity>()
            .Where(s => s.Status == "Confirmed" || s.Status == "Completed").ToListAsync();
        var demandData = await BuildDemandDictionaryAsync(materials, salesOrders);
        var vendors = await _db.Set<VendorEntity>().Where(v => v.Status == "Active").ToListAsync();
        var vendorLeadTimes = await _db.Set<VendorLeadTimeEntity>().ToListAsync();
        var result = new List<MrpSuggestionDto>();
        var exceptions = new List<MrpExceptionMessageEntity>();

        var filteredMaterials = materialIdFilter(materials, plantId);

        foreach (var mat in filteredMaterials)
        {
            var stock = stockItems.Where(s => s.MaterialName == mat.Name).Sum(s => s.Quantity);
            var demand = await CalculateTotalDemandAsync(mat.Name);
            var openPo = await GetOpenPurchaseOrdersAsync(mat.Name);
            var demandHistory = demandData.TryGetValue(mat.Name, out var dh) ? dh : new List<decimal> { 100 };
            var safetyStock = CalculateStatisticalSafetyStock(demandHistory, 0.95);
            var shortage = Math.Max(0, demand + safetyStock - stock - openPo);

            if (shortage > 0 || demand > 0)
            {
                var suggestionType = mat.Type is "RAW" or "PACKAGING" or "CONSUMABLE" ? "PURCHASE" : "PRODUCE";
                var adjustedQty = shortage > 0 ? shortage : 0;
                var maxLeadDays = 0;

                if (suggestionType == "PURCHASE" && adjustedQty > 0)
                {
                    var matLeadTimes = vendorLeadTimes.Where(vlt => vlt.MaterialCode == mat.Code).ToList();
                    if (matLeadTimes.Any())
                    {
                        maxLeadDays = matLeadTimes.Max(vlt => vlt.LeadTimeDays);
                        var needBy = DateTime.Today.AddDays(14);
                        var orderDate = needBy.AddDays(-maxLeadDays);
                    }
                }

                result.Add(new MrpSuggestionDto
                {
                    MaterialId = Guid.NewGuid(),
                    MaterialCode = mat.Code,
                    MaterialName = mat.Name,
                    CurrentStock = stock,
                    OpenPoQty = openPo,
                    TotalDemand = demand,
                    ShortageQty = shortage,
                    SuggestionType = suggestionType,
                    SuggestedQty = adjustedQty
                });

                if (shortage > 0)
                {
                    exceptions.Add(new MrpExceptionMessageEntity
                    {
                        TenantId = tenantId,
                        MaterialCode = mat.Code,
                        MaterialName = mat.Name,
                        ExceptionType = "STOCK_SHORTAGE",
                        Message = $"Stock shortage of {shortage} for {mat.Name}",
                        Severity = "Error",
                        SuggestedAction = $"Place order {maxLeadDays} days ahead of need date"
                    });
                }

                if (suggestionType == "PURCHASE")
                {
                    var hasVendor = vendors.Any(v =>
                        vendorLeadTimes.Any(vlt => vlt.VendorId == v.Id && vlt.MaterialCode == mat.Code));
                    if (!hasVendor)
                    {
                        exceptions.Add(new MrpExceptionMessageEntity
                        {
                            TenantId = tenantId,
                            MaterialCode = mat.Code,
                            MaterialName = mat.Name,
                            ExceptionType = "NO_VENDOR",
                            Message = $"No vendor assigned for material {mat.Name}",
                            Severity = "Warning",
                            SuggestedAction = "Assign a vendor in vendor lead time configuration"
                        });
                    }

                    var longLead = vendorLeadTimes
                        .Where(vlt => vlt.MaterialCode == mat.Code && vlt.LeadTimeDays > 30)
                        .ToList();
                    foreach (var lt in longLead)
                    {
                        exceptions.Add(new MrpExceptionMessageEntity
                        {
                            TenantId = tenantId,
                            MaterialCode = mat.Code,
                            MaterialName = mat.Name,
                            ExceptionType = "LONG_LEAD_TIME",
                            Message = $"Lead time {lt.LeadTimeDays}d > 30d for {mat.Name}",
                            Severity = "Warning",
                            SuggestedAction = "Source alternative vendor or increase safety stock"
                        });
                    }
                }
            }
        }

        sw.Stop();

        var history = new MrpRunHistoryEntity
        {
            TenantId = tenantId,
            RunType = "WithVendorLT",
            RunAt = DateTime.UtcNow,
            Status = "Completed",
            MaterialsProcessed = filteredMaterials.Count,
            SuggestionsGenerated = result.Count,
            ExceptionMessages = exceptions.Count,
            DurationMs = sw.ElapsedMilliseconds
        };
        _db.Set<MrpRunHistoryEntity>().Add(history);

        foreach (var ex in exceptions)
        {
            ex.RunHistoryId = history.Id;
        }
        _db.Set<MrpExceptionMessageEntity>().AddRange(exceptions);
        await _db.SaveChangesAsync();

        return result;
    }

    private List<MaterialMasterEntity> materialIdFilter(List<MaterialMasterEntity> materials, Guid? plantId)
    {
        if (!plantId.HasValue) return materials;
        var plant = _db.Set<PlantEntity>().FirstOrDefault(p => p.Id == plantId.Value);
        if (plant == null) return materials;
        return materials.Where(m => m.Code.StartsWith(plant.Code)).ToList();
    }

    private async Task ExplodeLevelAsync(string productName, decimal quantity, int level,
        List<BillOfMaterialEntity> boms, List<MaterialMasterEntity> materials,
        List<StockItemEntity> stockItems, Dictionary<string, List<decimal>> demandData,
        List<MrpRequirementDto> requirements, int maxLevel)
    {
        if (level > maxLevel) return;

        var components = boms.Where(b => b.ProductName == productName).ToList();
        foreach (var bom in components)
        {
            var grossReq = quantity * bom.Quantity;
            var material = materials.FirstOrDefault(m => m.Name == bom.ComponentName);
            var stock = material != null
                ? stockItems.Where(s => s.MaterialName == material.Name).Sum(s => s.Quantity)
                : 0;
            var demandHistory = material != null && demandData.TryGetValue(material.Name, out var dh) ? dh : new List<decimal> { 100 };
            var safetyStock = CalculateStatisticalSafetyStock(demandHistory, 0.95);
            var netReq = Math.Max(0, grossReq - stock + safetyStock);

            var isManufactured = boms.Any(b => b.ProductName == bom.ComponentName);

            requirements.Add(new MrpRequirementDto
            {
                Level = level,
                ParentProduct = productName,
                ComponentCode = material?.Code ?? bom.ComponentName,
                ComponentName = bom.ComponentName,
                QtyPerParent = bom.Quantity,
                GrossRequirement = grossReq,
                OnHandStock = stock,
                ScheduledReceipts = 0,
                SafetyStock = safetyStock,
                NetRequirement = netReq,
                OrderType = isManufactured ? "PRODUCE" : "PURCHASE",
                UOM = bom.UOM,
                NeedByDate = DateTime.Today.AddDays(level * 7)
            });

            if (isManufactured && netReq > 0)
            {
                await ExplodeLevelAsync(bom.ComponentName, netReq, level + 1, boms, materials, stockItems, demandData, requirements, maxLevel);
            }
        }
    }

    private async Task<decimal> CalculateTotalDemandAsync(string materialName)
    {
        var prodOrders = await _db.Set<ProductionOrderEntity>().Where(o => o.Status != "Completed").ToListAsync();
        var boms = await _db.Set<BillOfMaterialEntity>().Where(b => b.Status == "Active").ToListAsync();
        decimal demand = 0;

        foreach (var order in prodOrders)
        {
            var components = boms.Where(b => b.ProductName == order.ProductName).ToList();
            demand += components.Where(c => c.ComponentName == materialName).Sum(c => order.Quantity * c.Quantity);
        }
        return demand;
    }

    private async Task<decimal> GetOpenPurchaseOrdersAsync(string materialName)
    {
        var pos = await _db.Set<PurchaseOrderEntity>()
            .Where(p => p.ItemName == materialName && p.Status != "Completed" && p.Status != "Cancelled")
            .ToListAsync();
        decimal total = 0;
        foreach (var po in pos)
        {
            var parts = po.Quantity?.Split(' ');
            if (parts != null && parts.Length > 0 && decimal.TryParse(parts[0], out var qty))
                total += qty;
        }
        return total;
    }

    private static decimal CalculateStatisticalSafetyStock(List<decimal> demandHistory, double serviceLevel)
    {
        if (demandHistory.Count < 2) return demandHistory.Sum() * 0.2m;
        var avg = demandHistory.Average();
        var variance = demandHistory.Select(d => (double)(d - avg)).Select(d => d * d).Average();
        var stdDev = (decimal)Math.Sqrt(variance);
        var zScore = GetZScore(serviceLevel);
        return Math.Round(zScore * stdDev * (decimal)Math.Sqrt(7), 0);
    }

    private static decimal GetZScore(double serviceLevel) => serviceLevel switch
    {
        >= 0.999 => 3.09m, >= 0.995 => 2.58m, >= 0.99 => 2.33m,
        >= 0.975 => 1.96m, >= 0.95 => 1.65m, >= 0.90 => 1.28m,
        >= 0.85 => 1.04m, >= 0.80 => 0.84m, _ => 0.00m
    };

    private async Task<Dictionary<string, List<decimal>>> BuildDemandDictionaryAsync(
        List<MaterialMasterEntity> materials, List<SalesOrderEntity> salesOrders)
    {
        var dict = new Dictionary<string, List<decimal>>();
        var prodOrders = await _db.Set<ProductionOrderEntity>()
            .Where(p => p.Status != "Cancelled").ToListAsync();
        var allLines = await _db.Set<SalesOrderLineEntity>().ToListAsync();
        var soIds = salesOrders.Select(s => s.Id).ToHashSet();
        allLines = allLines.Where(l => soIds.Contains(l.SalesOrderId)).ToList();

        foreach (var mat in materials)
        {
            var monthlyQuantities = new List<decimal>();

            for (int m = 0; m < 12; m++)
            {
                var month = DateTime.UtcNow.AddMonths(-11 + m);
                var monthLineQty = allLines
                    .Where(l => l.MaterialName == mat.Name && salesOrders.Any(s =>
                        s.Id == l.SalesOrderId && s.OrderDate.Year == month.Year && s.OrderDate.Month == month.Month))
                    .Sum(l => l.Quantity);
                var monthProd = prodOrders
                    .Where(p => p.ProductName == mat.Name && p.StartDate.Year == month.Year && p.StartDate.Month == month.Month)
                    .Sum(p => p.Quantity);
                monthlyQuantities.Add(monthLineQty + monthProd);
            }

            if (monthlyQuantities.All(q => q == 0))
                monthlyQuantities = mat.Type switch
                {
                    "RAW" => new() { 1200, 1150, 1300, 1100, 1250, 1400, 1350, 1280, 1420, 1380, 1500, 1450 },
                    "FINISHED" => new() { 500, 480, 520, 490, 510, 530, 505, 515, 540, 525, 550, 535 },
                    "PACKAGING" => new() { 3000, 2800, 3200, 2900, 3100, 3300, 3050, 3150, 3400, 3250, 3500, 3350 },
                    _ => new() { 100, 95, 110, 90, 105, 115, 108, 102, 118, 112, 120, 110 }
                };

            dict[mat.Name] = monthlyQuantities;
        }
        return dict;
    }
}
