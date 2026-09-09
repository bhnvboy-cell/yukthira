using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class InventoryValuationService : IInventoryValuationService
{
    private readonly YuktiraDbContext _db;

    public InventoryValuationService(YuktiraDbContext db) => _db = db;

    public async Task<decimal> CalculateMovingAveragePriceAsync(string materialCode, string plant, Guid tenantId)
    {
        var balances = await _db.StockBalances
            .AsNoTracking()
            .Where(s =>
                s.TenantId == tenantId &&
                s.MaterialCode == materialCode &&
                s.Plant == plant)
            .ToListAsync();

        var totalQty = balances.Sum(s => s.Quantity);
        var totalValue = balances.Sum(s => s.TotalValue);

        return totalQty > 0 ? totalValue / totalQty : 0;
    }

    public async Task RevaluateMaterialAsync(string materialCode, string plant, Guid tenantId, string userId)
    {
        var map = await CalculateMovingAveragePriceAsync(materialCode, plant, tenantId);

        var balances = await _db.StockBalances
            .Where(s =>
                s.TenantId == tenantId &&
                s.MaterialCode == materialCode &&
                s.Plant == plant)
            .ToListAsync();

        foreach (var balance in balances)
        {
            var oldValue = balance.TotalValue;
            balance.UnitPrice = map;
            balance.TotalValue = balance.Quantity * map;
            balance.UpdatedAt = DateTime.UtcNow;

            var ledger = new InventoryValuationLedgerEntity
            {
                TenantId = tenantId.ToString(),
                MaterialCode = materialCode,
                MaterialName = balance.MaterialName,
                Plant = plant,
                StorageLocation = balance.StorageLocation,
                BatchNumber = balance.BatchNumber,
                ValuationDate = DateTime.UtcNow,
                DocumentNumber = $"REV-{DateTime.UtcNow:yyyyMMddHHmmss}",
                MovementType = 0,
                OpeningQuantity = balance.Quantity,
                OpeningValue = oldValue,
                ClosingQuantity = balance.Quantity,
                ClosingValue = balance.TotalValue,
                MovingAveragePrice = map,
                PriceControl = "V",
                Status = "Active"
            };
            _db.InventoryValuationLedger.Add(ledger);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<ValuationLedgerDto>> GetValuationLedgerAsync(string materialCode, string plant, DateTime fromDate, DateTime toDate, Guid tenantId)
    {
        var entries = await _db.InventoryValuationLedger
            .AsNoTracking()
            .Where(l =>
                l.TenantId == tenantId.ToString() &&
                l.MaterialCode == materialCode &&
                l.Plant == plant &&
                l.ValuationDate >= fromDate &&
                l.ValuationDate <= toDate)
            .OrderBy(l => l.ValuationDate)
            .ToListAsync();

        return entries.Select(l => new ValuationLedgerDto
        {
            MaterialCode = l.MaterialCode,
            Plant = l.Plant,
            StorageLocation = l.StorageLocation,
            BatchNumber = l.BatchNumber,
            ValuationDate = l.ValuationDate,
            DocumentNumber = l.DocumentNumber,
            OpeningQuantity = l.OpeningQuantity,
            OpeningValue = l.OpeningValue,
            ClosingQuantity = l.ClosingQuantity,
            ClosingValue = l.ClosingValue,
            MovingAveragePrice = l.MovingAveragePrice,
            PriceControl = l.PriceControl
        }).ToList();
    }
}
