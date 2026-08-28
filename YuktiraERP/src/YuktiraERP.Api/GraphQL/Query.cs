using HotChocolate;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.GraphQL;

public class Query
{
    [UseFiltering]
    [UseSorting]
    public IQueryable<MaterialMasterEntity> GetMaterials([Service] YuktiraDbContext db)
        => db.MaterialMasters.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<CustomerEntity> GetCustomers([Service] YuktiraDbContext db)
        => db.Customers.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<VendorEntity> GetVendors([Service] YuktiraDbContext db)
        => db.Vendors.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<SalesOrderEntity> GetSalesOrders([Service] YuktiraDbContext db)
        => db.SalesOrders.Include(s => s.Lines).AsNoTracking();

    public async Task<SalesOrderEntity?> GetSalesOrderByIdAsync(
        Guid id, [Service] YuktiraDbContext db)
        => await db.SalesOrders.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == id);

    [UseFiltering]
    [UseSorting]
    public IQueryable<PurchaseOrderEntity> GetPurchaseOrders([Service] YuktiraDbContext db)
        => db.PurchaseOrders.Include(p => p.Items).AsNoTracking();

    public async Task<PurchaseOrderEntity?> GetPurchaseOrderByIdAsync(
        Guid id, [Service] YuktiraDbContext db)
        => await db.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);

    [UseFiltering]
    [UseSorting]
    public IQueryable<ProductionOrderEntity> GetProductionOrders([Service] YuktiraDbContext db)
        => db.ProductionOrders.AsNoTracking();

    public async Task<ProductionOrderEntity?> GetProductionOrderByIdAsync(
        Guid id, [Service] YuktiraDbContext db)
        => await db.ProductionOrders.FindAsync(id);

    [UseFiltering]
    [UseSorting]
    public IQueryable<StockItemEntity> GetStockItems([Service] YuktiraDbContext db)
        => db.StockItems.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<StockMovementEntity> GetStockMovements([Service] YuktiraDbContext db)
        => db.StockMovements.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<BatchEntity> GetBatches([Service] YuktiraDbContext db)
        => db.Batches.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<QualityNotificationEntity> GetQualityNotifications([Service] YuktiraDbContext db)
        => db.QualityNotifications.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<InspectionLotEntity> GetInspectionLots([Service] YuktiraDbContext db)
        => db.InspectionLots.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<UniversalJournalEntity> GetJournalEntries([Service] YuktiraDbContext db)
        => db.UniversalJournals.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<MaintenanceOrderEntity> GetMaintenanceOrders([Service] YuktiraDbContext db)
        => db.MaintenanceOrders.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<EquipmentEntity> GetEquipments([Service] YuktiraDbContext db)
        => db.Equipments.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<WavePickEntity> GetWavePicks([Service] YuktiraDbContext db)
        => db.WavePicks.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<VelocitySlottingEntity> GetVelocitySlottings([Service] YuktiraDbContext db)
        => db.VelocitySlottings.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<BinMasterEntity> GetBinMasters([Service] YuktiraDbContext db)
        => db.BinMasters.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<ImmutableAuditTrailEntity> GetAuditTrails([Service] YuktiraDbContext db)
        => db.ImmutableAuditTrails.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<SoxViolationEntity> GetSoxViolations([Service] YuktiraDbContext db)
        => db.SoxViolations.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<AiForecastEntity> GetAiForecasts([Service] YuktiraDbContext db)
        => db.AiForecasts.AsNoTracking();

    [UseFiltering]
    [UseSorting]
    public IQueryable<AiAnomalyEntity> GetAiAnomalies([Service] YuktiraDbContext db)
        => db.AiAnomalies.AsNoTracking();

    public async Task<DashboardSummaryType> GetDashboardAsync([Service] YuktiraDbContext db)
    {
        var materials = await db.MaterialMasters.AsNoTracking().ToListAsync();
        var stockItems = await db.StockItems.AsNoTracking().ToListAsync();
        var salesOrders = await db.SalesOrders.AsNoTracking().ToListAsync();
        var purchaseOrders = await db.PurchaseOrders.AsNoTracking().ToListAsync();
        var prodOrders = await db.ProductionOrders.AsNoTracking().ToListAsync();
        var qmLots = await db.InspectionLots.AsNoTracking().ToListAsync();
        var journalEntries = await db.UniversalJournals.AsNoTracking().ToListAsync();
        var movements = await db.StockMovements.AsNoTracking().ToListAsync();

        var totalStockValue = stockItems.Sum(s => s.Value);
        var lowStock = stockItems.Where(s => s.Quantity <= s.MinStock).ToList();
        var totalRevenue = salesOrders.Sum(o => o.Amount);
        var pendingOrders = salesOrders.Count(o => o.Status == "Pending");
        var completedOrders = salesOrders.Count(o => o.Status == "Completed" || o.Status == "Delivered");
        var prodInProgress = prodOrders.Count(o => o.Status == "IN_PROGRESS");
        var prodCompleted = prodOrders.Count(o => o.Status == "COMPLETED");
        var totalDebits = journalEntries.Sum(j => j.DebitAmount);
        var totalCredits = journalEntries.Sum(j => j.CreditAmount);
        var passedLots = qmLots.Sum(l => l.Passed);
        var totalInspected = qmLots.Sum(l => l.Inspected);

        return new DashboardSummaryType
        {
            Kpis = new List<DashboardKpiType>
            {
                new() { Name = "Total Materials", Value = materials.Count, Unit = "count", Trend = "up" },
                new() { Name = "Stock Value", Value = totalStockValue, Unit = "INR", Trend = "up" },
                new() { Name = "Total Revenue", Value = totalRevenue, Unit = "INR", Trend = "up" },
                new() { Name = "Open Orders", Value = pendingOrders, Unit = "count", Trend = pendingOrders > 10 ? "down" : "up" },
                new() { Name = "Production Active", Value = prodInProgress, Unit = "count", Trend = "up" },
                new() { Name = "QM Pass Rate", Value = totalInspected > 0 ? Math.Round((decimal)passedLots / totalInspected * 100, 1) : 0, Unit = "%", Trend = "up" },
                new() { Name = "Journal Entries", Value = journalEntries.Count, Unit = "count", Trend = "up" },
                new() { Name = "Stock Movements", Value = movements.Count, Unit = "count", Trend = "up" },
            },
            Inventory = new InventorySummaryType
            {
                TotalMaterials = materials.Count,
                TotalStockValue = totalStockValue,
                LowStockCount = lowStock.Count,
                LowStockItems = lowStock.Take(10).Select(s => new StockItemEntity
                {
                    MaterialName = s.MaterialName,
                    Quantity = s.Quantity,
                    UOM = s.UOM,
                    Value = s.Value,
                    Bin = s.Bin,
                    Lot = s.Lot
                }).ToList()
            },
            Sales = new SalesSummaryType
            {
                TotalOrders = salesOrders.Count,
                TotalRevenue = totalRevenue,
                AverageOrderValue = salesOrders.Count > 0 ? totalRevenue / salesOrders.Count : 0,
                PendingOrders = pendingOrders,
                CompletedOrders = completedOrders
            },
            Production = new ProductionSummaryType
            {
                TotalOrders = prodOrders.Count,
                PlannedOrders = prodOrders.Count(o => o.Status == "PLANNED"),
                InProgressOrders = prodInProgress,
                CompletedOrders = prodCompleted,
                TotalPlannedCost = prodOrders.Sum(o => o.PlannedCost),
                TotalActualCost = prodOrders.Sum(o => o.ActualCost),
                CostVariance = prodOrders.Sum(o => o.PlannedCost - o.ActualCost)
            },
            Quality = new QualitySummaryType
            {
                TotalLots = qmLots.Count,
                PendingInspection = qmLots.Count(l => l.Status == "Pending"),
                PassedLots = passedLots,
                FailedLots = qmLots.Sum(l => l.Failed),
                PassRate = totalInspected > 0 ? Math.Round((decimal)passedLots / totalInspected * 100, 1) : 0
            },
            Procurement = new ProcurementSummaryType
            {
                TotalPOs = purchaseOrders.Count,
                TotalValue = purchaseOrders.Sum(p => p.Amount),
                PendingGR = purchaseOrders.Count(p => p.Status == "Pending"),
                PendingIR = purchaseOrders.Count(p => p.Status == "GR Done")
            },
            Financial = new FinancialSummaryType
            {
                TotalDebits = totalDebits,
                TotalCredits = totalCredits,
                NetBalance = totalDebits - totalCredits,
                JournalEntries = journalEntries.Count,
                PostedEntries = journalEntries.Count(j => j.Status == "Posted")
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<List<MaterialStockAlertType>> GetStockAlertsAsync(
        [Service] YuktiraDbContext db)
    {
        var stockItems = await db.StockItems.AsNoTracking().ToListAsync();
        return stockItems
            .Where(s => s.Quantity <= s.MinStock || s.Quantity >= s.MaxStock)
            .Select(s => new MaterialStockAlertType
            {
                MaterialCode = s.Bin,
                MaterialName = s.MaterialName,
                CurrentStock = s.Quantity,
                MinStock = s.MinStock,
                MaxStock = s.MaxStock,
                AlertLevel = s.Quantity <= s.MinStock ? "LOW" : "OVER"
            })
            .ToList();
    }
}
