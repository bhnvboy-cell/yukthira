using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IInspectionResultService
{
    Task<InspectionResultDetailEntity> RecordResultAsync(InspectionResultDetailEntity result, string userId);
    Task<InspectionResultDetailEntity> RecordDefectAsync(string lotNumber, string defectCodeGroup, string defectCode, string defectDescription, string defectCategory, int quantity, string reportType, string userId);
    Task<List<InspectionResultDetailEntity>> GetResultsByLotAsync(string lotNumber);
    Task<UsageDecisionDetailEntity> ConfirmCertificateAsync(string lotNumber, string plant, string origin, string status, string userId);
    Task<UsageDecisionDetailEntity> RecordUsageDecisionAsync(string lotNumber, string udCode, string udDescription, string stockProposal, string userId);
    Task<List<UsageDecisionDetailEntity>> GetAllUsageDecisionsAsync(string? status = null, int take = 50);
    Task<UsageDecisionDetailEntity?> GetUsageDecisionByLotAsync(string lotNumber);
    Task<UDReversalResult> ReverseUsageDecisionAsync(Guid inspectionLotId, string reversalReason, Guid tenantId, string userId);
    Task<InspectionLotEntity?> GetInspectionLotByIdAsync(Guid inspectionLotId);
    Task<List<StockBalanceEntity>> GetStockBalancesAsync(Guid tenantId, string? materialCode = null, string? batchNumber = null);
}

public class InspectionResultService : IInspectionResultService
{
    private readonly YuktiraDbContext _db;

    public InspectionResultService(YuktiraDbContext db) => _db = db;

    public async Task<InspectionResultDetailEntity> RecordResultAsync(InspectionResultDetailEntity result, string userId)
    {
        result.RecordedBy = userId;
        result.RecordedAt = DateTime.UtcNow;
        result.ResultStatus = "RECORDED";
        result.CreatedAt = DateTime.UtcNow;
        _db.InspectionResultDetails.Add(result);
        await _db.SaveChangesAsync();
        return result;
    }

    public async Task<InspectionResultDetailEntity> RecordDefectAsync(string lotNumber, string defectCodeGroup, string defectCode, string defectDescription, string defectCategory, int quantity, string reportType, string userId)
    {
        var existing = await _db.InspectionResultDetails.FirstOrDefaultAsync(r => r.LotNumber == lotNumber && r.ResultStatus == "RECORDED");
        if (existing != null)
        {
            existing.DefectCodeGroup = defectCodeGroup;
            existing.DefectCode = defectCode;
            existing.DefectDescription = defectDescription;
            existing.DefectCategory = defectCategory;
            existing.ReportType = reportType;
            existing.DefectiveQuantity = quantity;
            existing.ResultStatus = "DEFECT_RECORDED";
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var defect = new InspectionResultDetailEntity
            {
                LotNumber = lotNumber,
                DefectCodeGroup = defectCodeGroup,
                DefectCode = defectCode,
                DefectDescription = defectDescription,
                DefectCategory = defectCategory,
                ReportType = reportType,
                DefectiveQuantity = quantity,
                ResultStatus = "DEFECT_RECORDED",
                RecordedBy = userId,
                RecordedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _db.InspectionResultDetails.Add(defect);
        }
        await _db.SaveChangesAsync();
        return existing ?? await _db.InspectionResultDetails.FirstOrDefaultAsync(r => r.LotNumber == lotNumber && r.ResultStatus == "DEFECT_RECORDED")!;
    }

    public async Task<List<InspectionResultDetailEntity>> GetResultsByLotAsync(string lotNumber) =>
        await _db.InspectionResultDetails.Where(r => r.LotNumber == lotNumber).OrderByDescending(r => r.RecordedAt).ToListAsync();

    public async Task<UsageDecisionDetailEntity> ConfirmCertificateAsync(string lotNumber, string plant, string origin, string status, string userId)
    {
        var ud = await _db.UsageDecisionDetails.FirstOrDefaultAsync(u => u.LotNumber == lotNumber);
        if (ud == null)
        {
            ud = new UsageDecisionDetailEntity
            {
                LotNumber = lotNumber,
                Plant = plant,
                InspectionLotOrigin = origin,
                ResultRecordingStatus = status,
                CertificateReceived = "Yes",
                Status = "CERT_CONFIRMED",
                DecidedBy = userId,
                DecisionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _db.UsageDecisionDetails.Add(ud);
        }
        else
        {
            ud.CertificateReceived = "Yes";
            ud.ResultRecordingStatus = status;
            ud.Status = "CERT_CONFIRMED";
            ud.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return ud;
    }

    public async Task<UsageDecisionDetailEntity> RecordUsageDecisionAsync(string lotNumber, string udCode, string udDescription, string stockProposal, string userId)
    {
        var ud = await _db.UsageDecisionDetails.FirstOrDefaultAsync(u => u.LotNumber == lotNumber);
        if (ud == null)
        {
            ud = new UsageDecisionDetailEntity
            {
                LotNumber = lotNumber,
                UDCode = udCode,
                UDDescription = udDescription,
                StockProposal = stockProposal,
                Status = "UD_RECORDED",
                DecidedBy = userId,
                DecisionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _db.UsageDecisionDetails.Add(ud);
        }
        else
        {
            ud.UDCode = udCode;
            ud.UDDescription = udDescription;
            ud.StockProposal = stockProposal;
            ud.Status = "UD_RECORDED";
            ud.DecisionDate = DateTime.UtcNow;
            ud.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return ud;
    }

    public async Task<List<UsageDecisionDetailEntity>> GetAllUsageDecisionsAsync(string? status = null, int take = 50)
    {
        var q = _db.UsageDecisionDetails.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(u => u.Status == status);
        return await q.OrderByDescending(u => u.CreatedAt).Take(take).ToListAsync();
    }

    public async Task<UsageDecisionDetailEntity?> GetUsageDecisionByLotAsync(string lotNumber) =>
        await _db.UsageDecisionDetails.FirstOrDefaultAsync(u => u.LotNumber == lotNumber);

    public async Task<InspectionLotEntity?> GetInspectionLotByIdAsync(Guid inspectionLotId) =>
        await _db.InspectionLots.FindAsync(inspectionLotId);

    public async Task<List<StockBalanceEntity>> GetStockBalancesAsync(Guid tenantId, string? materialCode = null, string? batchNumber = null)
    {
        var q = _db.StockBalances.Where(s => s.TenantId == tenantId && s.Status == "Active");
        if (!string.IsNullOrEmpty(materialCode))
            q = q.Where(s => s.MaterialCode == materialCode || s.MaterialName.Contains(materialCode));
        if (!string.IsNullOrEmpty(batchNumber))
            q = q.Where(s => s.BatchNumber == batchNumber);
        return await q.ToListAsync();
    }

    public async Task<UDReversalResult> ReverseUsageDecisionAsync(Guid inspectionLotId, string reversalReason, Guid tenantId, string userId)
    {
        var result = new UDReversalResult();

        var lot = await _db.InspectionLots.FindAsync(inspectionLotId);
        if (lot == null)
        {
            result.Errors.Add($"Inspection lot {inspectionLotId} not found.");
            return result;
        }

        if (lot.Status != "UsageDecisionCompleted" && lot.Status != "Completed" && lot.Status != "USAGE_DECIDED")
        {
            result.Errors.Add($"Inspection lot {lot.LotNumber} is in status '{lot.Status}'. Only lots in 'UsageDecisionCompleted' status can be reversed.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(reversalReason))
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

        var reversedStockType = previousStockType switch
        {
            "Unrestricted" => "QualityInspection",
            "Blocked" => "QualityInspection",
            "Scrapped" => "QualityInspection",
            "QualityInspection" => "QualityInspection",
            _ => "QualityInspection"
        };

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
                s.TenantId == tenantId &&
                s.MaterialCode == lot.MaterialCode &&
                s.BatchNumber == lot.BatchNumber &&
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
                s.TenantId == tenantId &&
                s.MaterialCode == lot.MaterialCode &&
                s.BatchNumber == lot.BatchNumber &&
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
                    TenantId = tenantId,
                    MaterialCode = lot.MaterialCode,
                    MaterialName = lot.MaterialName,
                    Plant = lot.Plant,
                    StorageLocation = lot.StorageLocation,
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
                TenantId = tenantId,
                InspectionLotId = lot.Id,
                LotNumber = lot.LotNumber,
                Action = "UD_REVERSAL",
                PreviousStatus = previousStatus,
                NewStatus = lot.Status,
                PreviousUDCode = previousUDCode,
                NewUDCode = "REVERSED",
                StockMovementType = "322",
                StockQuantityMoved = quantityToMove,
                StockFromLocation = previousStockType,
                StockToLocation = reversedStockType,
                Reason = reversalReason,
                UserId = userId,
                ActionTimestamp = DateTime.UtcNow,
                Notes = $"UD Reversal: {previousUDCode} -> QualityInspection. Lot {lot.LotNumber} returned to inspection."
            };
            _db.InspectionLotAudits.Add(audit);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.InspectionLotNumber = lot.LotNumber;
            result.PreviousStatus = previousStatus;
            result.NewStatus = lot.Status;
            result.PreviousUDCode = previousUDCode;
            result.StockMovementType = "322";
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

public class UDReversalResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string InspectionLotNumber { get; set; } = "";
    public string PreviousStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public string PreviousUDCode { get; set; } = "";
    public string StockMovementType { get; set; } = "";
    public decimal StockQuantityMoved { get; set; }
    public string StockFromType { get; set; } = "";
    public string StockToType { get; set; } = "";
    public Guid AuditId { get; set; }
    public DateTime ReversalTimestamp { get; set; }
}
