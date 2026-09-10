using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ZqmUsageDecisionEngineService : IZqmUsageDecisionEngineService
{
    private readonly YuktiraDbContext _db;

    public ZqmUsageDecisionEngineService(YuktiraDbContext db) => _db = db;

    public async Task<bool> ValidateUdCompletenessAsync(string lotNumber, Guid tenantId)
    {
        var lot = await _db.InspectionLots.FirstOrDefaultAsync(l => l.LotNumber == lotNumber);
        if (lot == null) return false;

        var results = await _db.InspectionResults.Where(r => r.LotNumber == lotNumber).ToListAsync();
        var allRecorded = results.All(r => r.Status != "Pending");
        var hasResults = results.Count > 0;

        return hasResults && allRecorded;
    }

    public async Task<UsageDecisionEngineResult> PostUsageDecisionAsync(UsageDecisionEngineRequest request)
    {
        var result = new UsageDecisionEngineResult();

        var lot = await _db.InspectionLots.FirstOrDefaultAsync(l => l.LotNumber == request.LotNumber);
        if (lot == null)
        {
            result.Errors.Add($"Inspection lot {request.LotNumber} not found.");
            return result;
        }

        if (lot.Status == "UsageDecisionCompleted" || lot.Status == "USAGE_DECIDED")
        {
            result.Errors.Add($"Lot {request.LotNumber} already has a usage decision.");
            return result;
        }

        var totalQty = decimal.TryParse(lot.Quantity, out var q) ? q : 0;
        var sumPosted = request.UnrestrictedQty + request.BlockedQty + request.ScrapQty + request.ReworkQty;

        if (sumPosted > totalQty)
        {
            result.Errors.Add($"Stock split total ({sumPosted}) exceeds lot quantity ({totalQty}).");
            return result;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var qualityScore = CalculateQualityScore(lot, request.QualityScoreMethod);

            var movementType = request.UDCode.ToUpperInvariant() switch
            {
                "ACCEPTED" or "A" => "321",
                "REJECTED" or "R" => "343",
                "SCRAP" => "551",
                "REWORK" => "322",
                _ => "321"
            };

            if (request.UnrestrictedQty > 0)
            {
                await PostStockTransferAsync(request.TenantId, lot.MaterialCode, lot.MaterialName,
                    lot.Plant, lot.BatchNumber, "QualityInspection", "Unrestricted",
                    request.UnrestrictedQty, lot.BaseUOM, "321");
            }

            if (request.BlockedQty > 0)
            {
                await PostStockTransferAsync(request.TenantId, lot.MaterialCode, lot.MaterialName,
                    lot.Plant, lot.BatchNumber, "QualityInspection", "Blocked",
                    request.BlockedQty, lot.BaseUOM, "343");
            }

            if (request.ScrapQty > 0)
            {
                await PostStockTransferAsync(request.TenantId, lot.MaterialCode, lot.MaterialName,
                    lot.Plant, lot.BatchNumber, "QualityInspection", "Scrapped",
                    request.ScrapQty, lot.BaseUOM, "551");
            }

            if (request.ReworkQty > 0)
            {
                await PostStockTransferAsync(request.TenantId, lot.MaterialCode, lot.MaterialName,
                    lot.Plant, lot.BatchNumber, "QualityInspection", "QualityInspection",
                    request.ReworkQty, lot.BaseUOM, "322");
            }

            var ud = new UsageDecisionEntity
            {
                LotNumber = request.LotNumber,
                MaterialName = lot.MaterialName,
                UDCode = request.UDCode,
                Decision = request.UDDescription,
                QualityScore = qualityScore,
                InspectorID = request.UserId,
                UnrestrictedStock = request.UnrestrictedQty,
                BlockedStock = request.BlockedQty,
                ScrapQuantity = request.ScrapQty,
                Notes = request.Notes,
                DecisionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _db.UsageDecisions.Add(ud);

            lot.Status = "UsageDecisionCompleted";
            lot.UpdatedAt = DateTime.UtcNow;
            _db.InspectionLots.Update(lot);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.UDCode = request.UDCode;
            result.QualityScore = qualityScore;
            result.UnrestrictedPosted = request.UnrestrictedQty;
            result.BlockedPosted = request.BlockedQty;
            result.ScrapPosted = request.ScrapQty;
            result.ReworkPosted = request.ReworkQty;
            result.StockMovementType = movementType;
            result.LotStatus = lot.Status;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Errors.Add($"UD posting failed: {ex.Message}");
        }

        return result;
    }

    private static decimal CalculateQualityScore(InspectionLotEntity lot, string method)
    {
        var total = lot.Inspected;
        if (total == 0) return 0;

        return method switch
        {
            "SingleFigure" => (decimal)lot.Passed / total * 100,
            "PercentageDefective" => (decimal)lot.Failed / total * 100,
            _ => (decimal)lot.Passed / total * 100
        };
    }

    private async Task PostStockTransferAsync(Guid tenantId, string materialCode, string materialName,
        string plant, string batchNumber, string fromType, string toType, decimal qty, string uom, string movementType)
    {
        var fromBalance = await _db.StockBalances.FirstOrDefaultAsync(s =>
            s.MaterialCode == materialCode && s.BatchNumber == batchNumber &&
            s.Plant == plant && s.StockType == fromType);

        if (fromBalance != null)
        {
            fromBalance.Quantity -= qty;
            fromBalance.TotalValue = fromBalance.Quantity * fromBalance.UnitPrice;
            fromBalance.UpdatedAt = DateTime.UtcNow;
            _db.StockBalances.Update(fromBalance);
        }

        var toBalance = await _db.StockBalances.FirstOrDefaultAsync(s =>
            s.MaterialCode == materialCode && s.BatchNumber == batchNumber &&
            s.Plant == plant && s.StockType == toType);

        if (toBalance != null)
        {
            toBalance.Quantity += qty;
            toBalance.TotalValue = toBalance.Quantity * toBalance.UnitPrice;
            toBalance.UpdatedAt = DateTime.UtcNow;
            _db.StockBalances.Update(toBalance);
        }
        else
        {
            _db.StockBalances.Add(new StockBalanceEntity
            {
                MaterialCode = materialCode,
                MaterialName = materialName,
                Plant = plant,
                BatchNumber = batchNumber,
                StockType = toType,
                Quantity = qty,
                UOM = uom,
                UnitPrice = fromBalance?.UnitPrice ?? 0,
                TotalValue = qty * (fromBalance?.UnitPrice ?? 0)
            });
        }
    }
}
