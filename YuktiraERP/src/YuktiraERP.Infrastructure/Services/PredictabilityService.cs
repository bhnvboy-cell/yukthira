using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class PredictabilityService : IPredictabilityService
{
    private readonly YuktiraDbContext _db;
    private readonly IAIEngine _aiEngine;

    public PredictabilityService(YuktiraDbContext db, IAIEngine aiEngine)
    {
        _db = db;
        _aiEngine = aiEngine;
    }

    public async Task<DemandForecastDto> ForecastDemandAsync(Guid tenantId, Guid materialId, int forecastPeriods, ForecastModel model = ForecastModel.ExponentialSmoothing)
    {
        var material = await _db.Set<MaterialMasterEntity>().FindAsync(materialId);
        var historicalDemand = await GetDemandHistoryFromDbAsync(material?.Name ?? "");

        var result = await _aiEngine.ForecastAsync(historicalDemand, forecastPeriods, model);

        var labels = new List<string>();
        for (int i = 0; i < historicalDemand.Count; i++)
            labels.Add($"T-{historicalDemand.Count - i}");
        for (int i = 1; i <= forecastPeriods; i++)
            labels.Add($"T+{i}");

        return new DemandForecastDto
        {
            MaterialName = material?.Name ?? "Unknown",
            ModelName = result.ModelName,
            HistoricalDemand = historicalDemand,
            ForecastedDemand = result.ForecastedValues,
            PeriodLabels = labels,
            NextPeriodForecast = result.NextValue,
            RSquare = result.RSquare ?? 0,
            Mape = result.Mape ?? CalculateMape(historicalDemand, result.ForecastedValues)
        };
    }

    public async Task<SafetyStockResult> CalculateSafetyStockAsync(Guid tenantId, Guid materialId, double serviceLevel = 0.95, decimal leadTimeDays = 7)
    {
        var material = await _db.Set<MaterialMasterEntity>().FindAsync(materialId);
        if (material == null) return new SafetyStockResult();

        var demandHistory = await GetDemandHistoryFromDbAsync(material.Name);
        var avgDemand = demandHistory.Count > 0 ? demandHistory.Average() : 0;
        var stdDev = demandHistory.Count > 1
            ? Math.Sqrt(demandHistory.Select(d => Math.Pow((double)(d - avgDemand), 2)).Average())
            : 0;
        var zScore = (double)GetZScore(serviceLevel);
        var safetyStock = (decimal)(zScore * stdDev * Math.Sqrt((double)leadTimeDays));
        var reorderPoint = avgDemand * leadTimeDays / 30 + safetyStock;

        var stock = await _db.Set<StockItemEntity>().Where(s => s.MaterialName == material.Name).SumAsync(s => s.Quantity);
        var daysOfStock = avgDemand > 0 ? stock / (avgDemand / 30) : 999;

        var recommendation = stock <= safetyStock ? "CRITICAL - Stock below safety level. Place urgent order."
            : stock <= reorderPoint ? "ALERT - Stock below reorder point. Place order soon."
            : stock <= reorderPoint * 1.5m ? "WARNING - Stock approaching reorder point. Monitor."
            : "OK - Stock level is healthy.";

        return new SafetyStockResult
        {
            MaterialId = material.Id,
            MaterialCode = material.Code,
            MaterialName = material.Name,
            AverageDemand = Math.Round(avgDemand, 1),
            DemandStdDev = Math.Round(stdDev, 1),
            LeadTimeDays = leadTimeDays,
            SafetyStock = Math.Round(safetyStock, 0),
            ReorderPoint = Math.Round(reorderPoint, 0),
            CurrentStock = stock,
            DaysOfStock = Math.Round(daysOfStock, 1),
            Recommendation = recommendation
        };
    }

    public async Task<List<SafetyStockResult>> GetStockAlertsAsync(Guid tenantId, double serviceLevel = 0.95)
    {
        var materials = await _db.Set<MaterialMasterEntity>().ToListAsync();
        var alerts = new List<SafetyStockResult>();
        foreach (var mat in materials)
        {
            var result = await CalculateSafetyStockAsync(tenantId, mat.Id, serviceLevel);
            if (result.CurrentStock <= result.ReorderPoint)
                alerts.Add(result);
        }
        return alerts;
    }

    public async Task<List<SafetyStockResult>> CalculateAllSafetyStockAsync(Guid tenantId, double serviceLevel = 0.95, decimal defaultLeadTimeDays = 7)
    {
        var materials = await _db.Set<MaterialMasterEntity>().ToListAsync();
        var results = new List<SafetyStockResult>();
        foreach (var mat in materials)
            results.Add(await CalculateSafetyStockAsync(tenantId, mat.Id, serviceLevel, defaultLeadTimeDays));
        return results;
    }

    private async Task<List<decimal>> GetDemandHistoryFromDbAsync(string materialName)
    {
        var confirmedSOs = await _db.Set<SalesOrderEntity>()
            .Where(s => s.Status == "Confirmed" || s.Status == "Completed").ToListAsync();
        var soIds = confirmedSOs.Select(s => s.Id).ToHashSet();
        var lines = await _db.Set<SalesOrderLineEntity>()
            .Where(l => l.MaterialName == materialName && soIds.Contains(l.SalesOrderId)).ToListAsync();
        var prodOrders = await _db.Set<ProductionOrderEntity>()
            .Where(p => p.ProductName == materialName && p.Status != "Cancelled").ToListAsync();

        if (lines.Count == 0 && prodOrders.Count == 0)
        {
            var allLines = await _db.Set<SalesOrderLineEntity>().Where(l => soIds.Contains(l.SalesOrderId)).ToListAsync();
            if (allLines.Count > 0)
            {
                var avgPerMat = allLines.Average(l => l.Quantity);
                return Enumerable.Repeat(avgPerMat, 12).ToList();
            }
            return new() { 100, 105, 98, 110, 102, 108, 115, 95, 112, 106, 118, 103 };
        }

        var monthly = new List<decimal>();
        for (int m = 0; m < 12; m++)
        {
            var month = DateTime.UtcNow.AddMonths(-11 + m);
            var lineQty = lines.Where(l =>
            {
                var so = confirmedSOs.FirstOrDefault(s => s.Id == l.SalesOrderId);
                return so != null && so.OrderDate.Year == month.Year && so.OrderDate.Month == month.Month;
            }).Sum(l => l.Quantity);
            var prodQty = prodOrders
                .Where(p => p.StartDate.Year == month.Year && p.StartDate.Month == month.Month)
                .Sum(p => p.Quantity);
            monthly.Add(lineQty + prodQty);
        }

        if (monthly.All(q => q == 0))
            return new() { 100, 105, 98, 110, 102, 108, 115, 95, 112, 106, 118, 103 };

        return monthly;
    }

    private static double CalculateMape(List<decimal> actual, List<decimal> forecast)
    {
        if (actual.Count == 0 || forecast.Count == 0) return 0;
        var n = Math.Min(actual.Count, forecast.Count);
        double sum = 0;
        for (int i = 0; i < n; i++)
            if (actual[i] != 0) sum += Math.Abs((double)((actual[i] - forecast[i]) / actual[i]));
        return sum / n * 100;
    }

    private static decimal GetZScore(double serviceLevel) => serviceLevel switch
    {
        >= 0.999 => 3.09m, >= 0.995 => 2.58m, >= 0.99 => 2.33m,
        >= 0.975 => 1.96m, >= 0.95 => 1.65m, >= 0.90 => 1.28m,
        >= 0.85 => 1.04m, >= 0.80 => 0.84m, _ => 0.00m
    };
}
