using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class KpiService : IKpiService
{
    private readonly YuktiraDbContext _db;

    public KpiService(YuktiraDbContext db) => _db = db;

    private static readonly List<KpiDefinitionDto> _definitions = new()
    {
        new() { Code = "InventoryHealth", Name = "Inventory Health", Formula = "StockBelowMin / TotalStock * 100", Category = "Inventory", Unit = "%" },
        new() { Code = "SalesToday", Name = "Sales Today", Formula = "SUM(SalesOrders.Amount WHERE Date=Today)", Category = "Sales", Unit = "USD" },
        new() { Code = "ProductionEfficiency", Name = "Production Efficiency", Formula = "(ActualOutput / PlannedOutput) * 100", Category = "Production", Unit = "%" },
        new() { Code = "QcPassRate", Name = "QC Pass Rate", Formula = "(Passed / Inspected) * 100", Category = "Quality", Unit = "%" },
        new() { Code = "LimsSampleTat", Name = "LIMS Sample TAT", Formula = "AVG(CompletionDate - CollectionDate)", Category = "LIMS", Unit = "Hours" },
        new() { Code = "MonthlyRevenue", Name = "Monthly Revenue", Formula = "SUM(SalesOrders.TotalAmount)", Category = "Sales", Unit = "USD" },
        new() { Code = "OpenPOs", Name = "Open Purchase Orders", Formula = "COUNT(PurchaseOrders WHERE Status='Pending')", Category = "Procurement", Unit = "Count" },
        new() { Code = "PendingApprovals", Name = "Pending Approvals", Formula = "COUNT(ApprovalRequests WHERE Status='Pending')", Category = "Operations", Unit = "Count" },
        new() { Code = "StockTurnover", Name = "Stock Turnover Ratio", Formula = "COGS / AvgStock", Category = "Inventory", Unit = "Ratio" },
        new() { Code = "EmployeeCount", Name = "Active Employees", Formula = "COUNT(Employees WHERE Status='Active')", Category = "HR", Unit = "Count" },
    };

    public async Task<Dictionary<string, decimal>> CalculateKpiAsync(Guid tenantId, string kpiCode)
    {
        var result = new Dictionary<string, decimal>();
        var value = kpiCode switch
        {
            "InventoryHealth" => await CalculateInventoryHealthAsync(tenantId),
            "SalesToday" => await CalculateSalesTodayAsync(tenantId),
            "ProductionEfficiency" => await CalculateProductionEfficiencyAsync(tenantId),
            "QcPassRate" => await CalculateQcPassRateAsync(tenantId),
            "LimsSampleTat" => await CalculateLimsSampleTatAsync(tenantId),
            "MonthlyRevenue" => await CalculateMonthlyRevenueAsync(tenantId),
            "OpenPOs" => await CountOpenPurchaseOrdersAsync(tenantId),
            "PendingApprovals" => await CountPendingApprovalsAsync(tenantId),
            "StockTurnover" => await CalculateStockTurnoverAsync(tenantId),
            "EmployeeCount" => await CountActiveEmployeesAsync(tenantId),
            _ => 0
        };
        result[kpiCode] = value;

        _db.KpiSnapshots.Add(new KpiSnapshotEntity
        {
            TenantId = tenantId,
            KpiCode = kpiCode,
            Value = value,
            SnapshotAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return result;
    }

    public Task<List<KpiDefinitionDto>> GetAvailableKpisAsync(Guid tenantId)
        => Task.FromResult(_definitions);

    public async Task<List<KpiCardDto>> GetDashboardKpisAsync(Guid tenantId)
    {
        var codes = new[] { "InventoryHealth", "SalesToday", "ProductionEfficiency", "QcPassRate", "LimsSampleTat" };
        var cards = new List<KpiCardDto>();

        foreach (var code in codes)
        {
            var current = (await CalculateKpiAsync(tenantId, code)).GetValueOrDefault(code, 0);
            var previous = await GetPreviousSnapshotAsync(tenantId, code);
            var trend = current > previous ? "up" : current < previous ? "down" : "flat";
            var trendValue = previous != 0 ? Math.Round((current - previous) / Math.Abs(previous) * 100, 1) : 0;

            var (display, unit, icon, category, color, alert) = FormatKpiCard(code, current);
            cards.Add(new KpiCardDto
            {
                Code = code,
                Label = _definitions.FirstOrDefault(d => d.Code == code)?.Name ?? code,
                Value = current,
                DisplayValue = display,
                Unit = unit,
                Icon = icon,
                Category = category,
                Color = color,
                Trend = trend,
                TrendValue = trendValue,
                TrendLabel = trendValue >= 0 ? $"+{trendValue}%" : $"{trendValue}%",
                PreviousValue = previous,
                AlertMessage = alert,
                DrillDownUrl = $"/Kpi/{code}"
            });
        }

        return cards;
    }

    public async Task<List<KpiBadgeDto>> GetModuleKpiBadgesAsync(Guid tenantId)
    {
        var alerts = await _db.Set<StockItemEntity>()
            .Where(s => s.TenantId == tenantId && s.Quantity <= s.MinStock)
            .CountAsync();

        var pendingApprovals = await _db.ApprovalRequests
            .CountAsync(a => a.TenantId == tenantId && a.Status == "Pending");

        var pendingPos = await _db.PurchaseOrders
            .CountAsync(p => p.TenantId == tenantId && p.Status == "Pending");

        var efficiency = await CalculateProductionEfficiencyAsync(tenantId);
        var qcPass = await CalculateQcPassRateAsync(tenantId);
        var tat = await CalculateLimsSampleTatAsync(tenantId);

        return new List<KpiBadgeDto>
        {
            new() { ModuleCode = "MM", Label = "Stock Alerts", Value = alerts.ToString(), Color = alerts > 5 ? "red" : alerts > 2 ? "yellow" : "green", Trend = alerts > 5 ? "up" : "flat" },
            new() { ModuleCode = "APP", Label = "Pending", Value = pendingApprovals.ToString(), Color = pendingApprovals > 10 ? "red" : pendingApprovals > 3 ? "yellow" : "green", Trend = "flat" },
            new() { ModuleCode = "PP", Label = "Efficiency", Value = $"%{efficiency:F0}", Color = efficiency >= 90 ? "green" : efficiency >= 70 ? "yellow" : "red", Trend = efficiency >= 90 ? "up" : "down" },
            new() { ModuleCode = "QM", Label = "QC Pass", Value = $"%{qcPass:F0}", Color = qcPass >= 95 ? "green" : qcPass >= 80 ? "yellow" : "red", Trend = qcPass >= 95 ? "up" : "down" },
            new() { ModuleCode = "LIMS", Label = "TAT", Value = $"{tat:F1}h", Color = tat <= 24 ? "green" : tat <= 48 ? "yellow" : "red", Trend = tat <= 24 ? "down" : "up" },
            new() { ModuleCode = "PO", Label = "Open", Value = pendingPos.ToString(), Color = pendingPos > 10 ? "yellow" : "green", Trend = "flat" },
        };
    }

    public async Task<KpiCardDto> GetDrillDownKpiAsync(Guid tenantId, string kpiCode)
    {
        var current = (await CalculateKpiAsync(tenantId, kpiCode)).GetValueOrDefault(kpiCode, 0);
        var previous = await GetPreviousSnapshotAsync(tenantId, kpiCode);
        var trend = current > previous ? "up" : current < previous ? "down" : "flat";
        var trendValue = previous != 0 ? Math.Round((current - previous) / Math.Abs(previous) * 100, 1) : 0;
        var (display, unit, icon, category, color, alert) = FormatKpiCard(kpiCode, current);

        return new KpiCardDto
        {
            Code = kpiCode,
            Label = _definitions.FirstOrDefault(d => d.Code == kpiCode)?.Name ?? kpiCode,
            Value = current,
            DisplayValue = display,
            Unit = unit,
            Icon = icon,
            Category = category,
            Color = color,
            Trend = trend,
            TrendValue = trendValue,
            TrendLabel = trendValue >= 0 ? $"+{trendValue}%" : $"{trendValue}%",
            PreviousValue = previous,
            AlertMessage = alert,
            DrillDownUrl = $"/Kpi/{kpiCode}"
        };
    }

    public async Task<List<KpiCardDto>> GetHistoricalKpiSnapshotsAsync(Guid tenantId, string kpiCode, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var snapshots = await _db.KpiSnapshots
            .Where(s => s.TenantId == tenantId && s.KpiCode == kpiCode && s.SnapshotAt >= since)
            .OrderBy(s => s.SnapshotAt)
            .ToListAsync();

        return snapshots.Select(s => new KpiCardDto
        {
            Code = s.KpiCode,
            Value = s.Value,
            DisplayValue = s.Value.ToString("N0"),
            Trend = "flat"
        }).ToList();
    }

    public Task<Dictionary<string, decimal>> GetThresholdsAsync()
    {
        return Task.FromResult(new Dictionary<string, decimal>
        {
            ["InventoryHealth_Green"] = 80,  // >=80% healthy
            ["InventoryHealth_Yellow"] = 60,
            ["SalesToday_Green"] = 10000,
            ["SalesToday_Yellow"] = 5000,
            ["ProductionEfficiency_Green"] = 90,
            ["ProductionEfficiency_Yellow"] = 75,
            ["QcPassRate_Green"] = 95,
            ["QcPassRate_Yellow"] = 80,
            ["LimsSampleTat_Green"] = 24,  // <=24h is green
            ["LimsSampleTat_Yellow"] = 48,
        });
    }

    public async Task<string> GetKpiColorCodeAsync(decimal value, decimal? thresholdGreen, decimal? thresholdYellow)
    {
        if (thresholdGreen == null && thresholdYellow == null) return "green";
        var thresholds = await GetThresholdsAsync();
        return value >= (thresholdGreen ?? 80) ? "green"
             : value >= (thresholdYellow ?? 60) ? "yellow"
             : "red";
    }

    // ── Private helpers ──

    private async Task<decimal> GetPreviousSnapshotAsync(Guid tenantId, string kpiCode)
    {
        var prev = await _db.KpiSnapshots
            .Where(s => s.TenantId == tenantId && s.KpiCode == kpiCode)
            .OrderByDescending(s => s.SnapshotAt)
            .Skip(1)
            .FirstOrDefaultAsync();
        return prev?.Value ?? 0;
    }

    private (string display, string unit, string icon, string category, string color, string? alert) FormatKpiCard(string code, decimal value)
    {
        var thresholds = GetThresholdsAsync().Result;
        return code switch
        {
            "InventoryHealth" => (
                $"%{value:F1}",
                "%",
                "bi-boxes",
                "Inventory",
                value >= thresholds.GetValueOrDefault("InventoryHealth_Green", 80) ? "green"
                    : value >= thresholds.GetValueOrDefault("InventoryHealth_Yellow", 60) ? "yellow" : "red",
                value < 60 ? "Critical stock levels detected" : value < 80 ? "Some items below reorder point" : null
            ),
            "SalesToday" => (
                $"${value:N0}",
                "USD",
                "bi-cart3",
                "Sales",
                value >= thresholds.GetValueOrDefault("SalesToday_Green", 10000) ? "green"
                    : value >= thresholds.GetValueOrDefault("SalesToday_Yellow", 5000) ? "yellow" : "red",
                value < 5000 ? "Sales below daily target" : null
            ),
            "ProductionEfficiency" => (
                $"%{value:F1}",
                "%",
                "bi-gear",
                "Production",
                value >= thresholds.GetValueOrDefault("ProductionEfficiency_Green", 90) ? "green"
                    : value >= thresholds.GetValueOrDefault("ProductionEfficiency_Yellow", 75) ? "yellow" : "red",
                value < 75 ? "Efficiency below threshold" : null
            ),
            "QcPassRate" => (
                $"%{value:F1}",
                "%",
                "bi-clipboard-check",
                "Quality",
                value >= thresholds.GetValueOrDefault("QcPassRate_Green", 95) ? "green"
                    : value >= thresholds.GetValueOrDefault("QcPassRate_Yellow", 80) ? "yellow" : "red",
                value < 80 ? "QC failure rate elevated" : null
            ),
            "LimsSampleTat" => (
                $"{value:F1}h",
                "Hours",
                "bi-flask",
                "LIMS",
                value <= thresholds.GetValueOrDefault("LimsSampleTat_Green", 24) ? "green"
                    : value <= thresholds.GetValueOrDefault("LimsSampleTat_Yellow", 48) ? "yellow" : "red",
                value > 48 ? "Sample TAT exceeds SLA" : null
            ),
            _ => (value.ToString("N2"), "", "bi-graph-up", "", "green", null)
        };
    }

    // ── KPI calculation methods ──

    private async Task<decimal> CalculateInventoryHealthAsync(Guid tenantId)
    {
        var total = await _db.Set<StockItemEntity>().CountAsync(s => s.TenantId == tenantId);
        if (total == 0) return 100;
        var belowMin = await _db.Set<StockItemEntity>().CountAsync(s => s.TenantId == tenantId && s.Quantity <= s.MinStock);
        return Math.Round((decimal)(total - belowMin) / total * 100, 1);
    }

    private async Task<decimal> CalculateSalesTodayAsync(Guid tenantId)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Set<SalesOrderEntity>()
            .Where(s => s.OrderDate >= today && s.OrderDate < today.AddDays(1))
            .SumAsync(s => s.Amount);
    }

    private async Task<decimal> CalculateProductionEfficiencyAsync(Guid tenantId)
    {
        var planned = await _db.Set<ProductionOrderEntity>()
            .Where(p => p.Status == "Completed" || p.Status == "InProgress")
            .SumAsync(p => p.Quantity);
        if (planned == 0) return 100;
        var actual = await _db.Set<ProductionOrderEntity>()
            .Where(p => p.Status == "Completed" && p.EndDate >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(p => p.Quantity);
        return Math.Round(actual / planned * 100, 1);
    }

    private async Task<decimal> CalculateQcPassRateAsync(Guid tenantId)
    {
        var inspected = await _db.Set<InspectionLotEntity>().SumAsync(i => i.Inspected);
        if (inspected == 0) return 100;
        var passed = await _db.Set<InspectionLotEntity>().SumAsync(i => i.Passed);
        return Math.Round((decimal)passed / inspected * 100, 1);
    }

    private async Task<decimal> CalculateLimsSampleTatAsync(Guid tenantId)
    {
        var samples = await _db.Set<SampleEntity>()
            .Where(s => s.Status == "Completed")
            .ToListAsync();
        if (samples.Count == 0) return 0;
        return Math.Round((decimal)samples.Average(s => (s.CreatedAt - s.CollectionDate).TotalHours), 1);
    }

    private async Task<decimal> CalculateMonthlyRevenueAsync(Guid tenantId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.BillingDocuments
            .Where(b => b.TenantId == tenantId && b.Status == "Paid" && b.CreatedAt >= startOfMonth)
            .SumAsync(b => b.Amount);
    }

    private async Task<decimal> CountOpenPurchaseOrdersAsync(Guid tenantId)
        => await _db.PurchaseOrders.CountAsync(p => p.TenantId == tenantId && p.Status == "Pending");

    private async Task<decimal> CountPendingApprovalsAsync(Guid tenantId)
        => await _db.ApprovalRequests.CountAsync(a => a.TenantId == tenantId && a.Status == "Pending");

    private async Task<decimal> CalculateStockTurnoverAsync(Guid tenantId)
    {
        var cogs = await _db.GeneralLedgerEntries
            .Where(g => g.TenantId == tenantId && g.AccountCode == "COGS")
            .SumAsync(g => g.Debit);
        var avgStock = await _db.Set<StockItemEntity>().Where(s => s.TenantId == tenantId).AverageAsync(s => s.Quantity);
        return avgStock > 0 ? cogs / avgStock : 0;
    }

    private async Task<decimal> CountActiveEmployeesAsync(Guid tenantId)
        => await _db.Employees.CountAsync(e => e.TenantId == tenantId && e.Status == "Active");
}
