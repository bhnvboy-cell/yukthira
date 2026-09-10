using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ZqmUdReversalEngineService : IZqmUdReversalEngineService
{
    private readonly YuktiraDbContext _db;

    public ZqmUdReversalEngineService(YuktiraDbContext db) => _db = db;

    public async Task<UDReversalResult> ReverseUdAsync(UdReversalEngineRequest request)
    {
        var result = new UDReversalResult();

        var lot = await _db.InspectionLots.FindAsync(request.InspectionLotId);
        if (lot == null)
        {
            result.Errors.Add($"Inspection lot {request.InspectionLotId} not found.");
            return result;
        }

        if (lot.Status != "UsageDecisionCompleted" && lot.Status != "Completed" && lot.Status != "USAGE_DECIDED")
        {
            result.Errors.Add($"Lot {lot.LotNumber} is in status '{lot.Status}'. Only completed lots can be reversed.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.ReversalReason))
        {
            result.Errors.Add("Reversal reason is required.");
            return result;
        }

        var ud = await _db.UsageDecisions.FirstOrDefaultAsync(u => u.LotNumber == lot.LotNumber);
        var previousUDCode = ud?.UDCode ?? "";
        var previousStatus = lot.Status;

        var previousStockType = ud?.UDCode?.ToUpperInvariant() switch
        {
            "ACCEPTED" or "A" => "Unrestricted",
            "REJECTED" or "R" => "Blocked",
            "REWORK" => "QualityInspection",
            "SCRAP" => "Scrapped",
            _ => "Unrestricted"
        };

        var reversedStockType = "QualityInspection";
        var movementType = request.MovementType ?? "322";

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            lot.Status = "InInspection";
            lot.UpdatedAt = DateTime.UtcNow;
            _db.InspectionLots.Update(lot);

            if (ud != null)
            {
                ud.UDCode = "REVERSED";
                ud.Decision = $"Reversed from {previousUDCode}";
                ud.UpdatedAt = DateTime.UtcNow;
                _db.UsageDecisions.Update(ud);
            }

            var existingBalance = await _db.StockBalances.FirstOrDefaultAsync(s =>
                s.MaterialCode == lot.MaterialCode &&
                s.BatchNumber == lot.BatchNumber &&
                s.Plant == lot.Plant &&
                s.StockType == previousStockType);

            decimal quantityToMove = 0m;
            if (existingBalance != null)
            {
                quantityToMove = Math.Min(existingBalance.Quantity, decimal.TryParse(lot.Quantity, out var qty) ? qty : 0);
                existingBalance.Quantity -= quantityToMove;
                existingBalance.TotalValue = existingBalance.Quantity * existingBalance.UnitPrice;
                existingBalance.UpdatedAt = DateTime.UtcNow;
                _db.StockBalances.Update(existingBalance);
            }

            var reversedBalance = await _db.StockBalances.FirstOrDefaultAsync(s =>
                s.MaterialCode == lot.MaterialCode &&
                s.BatchNumber == lot.BatchNumber &&
                s.Plant == lot.Plant &&
                s.StockType == reversedStockType);

            if (reversedBalance != null)
            {
                reversedBalance.Quantity += quantityToMove;
                reversedBalance.TotalValue = reversedBalance.Quantity * reversedBalance.UnitPrice;
                reversedBalance.UpdatedAt = DateTime.UtcNow;
                _db.StockBalances.Update(reversedBalance);
            }
            else if (quantityToMove > 0)
            {
                _db.StockBalances.Add(new StockBalanceEntity
                {
                    MaterialCode = lot.MaterialCode,
                    MaterialName = lot.MaterialName,
                    Plant = lot.Plant,
                    BatchNumber = lot.BatchNumber,
                    StockType = reversedStockType,
                    Quantity = quantityToMove,
                    UOM = lot.BaseUOM,
                    UnitPrice = existingBalance?.UnitPrice ?? 0,
                    TotalValue = quantityToMove * (existingBalance?.UnitPrice ?? 0)
                });
            }

            var audit = new InspectionLotAuditEntity
            {
                InspectionLotId = lot.Id,
                LotNumber = lot.LotNumber,
                Action = "UD_REVERSAL",
                PreviousStatus = previousStatus,
                NewStatus = lot.Status,
                PreviousUDCode = previousUDCode,
                NewUDCode = "REVERSED",
                StockMovementType = movementType,
                StockQuantityMoved = quantityToMove,
                StockFromLocation = previousStockType,
                StockToLocation = reversedStockType,
                Reason = request.ReversalReason,
                UserId = request.UserId,
                ActionTimestamp = DateTime.UtcNow,
                Notes = request.Notes
            };
            _db.InspectionLotAudits.Add(audit);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.InspectionLotNumber = lot.LotNumber;
            result.PreviousStatus = previousStatus;
            result.NewStatus = lot.Status;
            result.PreviousUDCode = previousUDCode;
            result.StockMovementType = movementType;
            result.StockQuantityMoved = quantityToMove;
            result.StockFromType = previousStockType;
            result.StockToType = reversedStockType;
            result.AuditId = audit.Id;
            result.ReversalTimestamp = audit.ActionTimestamp;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Errors.Add($"Reversal failed: {ex.Message}");
        }

        return result;
    }
}
