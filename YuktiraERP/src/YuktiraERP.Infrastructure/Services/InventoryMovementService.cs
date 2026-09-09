using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class InventoryMovementService : IInventoryMovementService
{
    private readonly YuktiraDbContext _db;

    public InventoryMovementService(YuktiraDbContext db) => _db = db;

    public async Task<PostGoodsMovementResultDto> PostGoodsReceiptAsync(PostGoodsMovementRequestDto request, Guid tenantId, string userId)
    {
        request.MovementType = request.MovementType == 0 ? 101 : request.MovementType;
        return await PostMovementAsync(request, tenantId, userId);
    }

    public async Task<PostGoodsMovementResultDto> PostGoodsIssueAsync(PostGoodsMovementRequestDto request, Guid tenantId, string userId)
    {
        request.MovementType = request.MovementType == 0 ? 201 : request.MovementType;
        return await PostMovementAsync(request, tenantId, userId);
    }

    public async Task<PostGoodsMovementResultDto> PostTransferPostingAsync(PostGoodsMovementRequestDto request, Guid tenantId, string userId)
    {
        request.MovementType = request.MovementType == 0 ? 311 : request.MovementType;
        return await PostMovementAsync(request, tenantId, userId);
    }

    public async Task<PostGoodsMovementResultDto> PostMovementAsync(PostGoodsMovementRequestDto request, Guid tenantId, string userId)
    {
        var result = new PostGoodsMovementResultDto();
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var docNumber = GenerateDocumentNumber(tenantId);
            var now = DateTime.UtcNow;

            var header = new MaterialDocumentHeaderEntity
            {
                TenantId = tenantId.ToString(),
                DocumentNumber = docNumber,
                PostingDate = DateTime.TryParse(request.PostingDate, out var pd) ? pd : now,
                DocumentDate = DateTime.TryParse(request.DocumentDate, out var dd) ? dd : now,
                MovementType = request.MovementType,
                MovementTypeDescription = GetMovementTypeDescription(request.MovementType),
                HeaderText = request.HeaderText,
                RefDocument = request.Reference,
                UserId = userId,
                PostedBy = userId,
                PostedAt = now,
                Status = "Posted"
            };
            _db.MaterialDocumentHeaders.Add(header);
            await _db.SaveChangesAsync();

            var lineNum = 0;
            foreach (var line in request.Lines)
            {
                lineNum++;
                var item = new MaterialDocumentItemEntity
                {
                    TenantId = tenantId.ToString(),
                    MaterialDocumentHeaderId = header.Id.ToString(),
                    LineNumber = lineNum,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    Plant = line.Plant,
                    StorageLocation = line.StorageLocation,
                    BatchNumber = line.BatchNumber,
                    MovementType = request.MovementType,
                    Quantity = line.Quantity,
                    UnitOfMeasure = line.UOM,
                    StockBucket = DetermineStockBucket(request.MovementType),
                    UnitPrice = line.UnitPrice ?? 0,
                    ValuationAmount = line.Quantity * (line.UnitPrice ?? 0),
                    VendorCode = line.VendorCode,
                    VendorName = line.VendorName,
                    CustomerCode = line.CustomerCode,
                    CustomerName = line.CustomerName,
                    ProductionOrderNo = line.ProductionOrderNo,
                    PurchaseOrderNo = line.PurchaseOrderNo,
                    SalesOrderNo = line.SalesOrderNo,
                    CostCenter = line.CostCenter,
                    ProfitCenter = line.ProfitCenter,
                    GLAccount = line.GLAccount,
                    SpecialStockIndicator = line.SpecialStockIndicator,
                    ItemText = line.ItemText
                };
                _db.MaterialDocumentItems.Add(item);

                var impact = await UpdateStockBalanceAsync(
                    tenantId, line.MaterialCode, line.MaterialName,
                    line.Plant, line.StorageLocation, line.BatchNumber ?? "",
                    request.MovementType, line.Quantity, line.UOM,
                    line.UnitPrice ?? 0, userId);

                result.StockImpacts.Add(impact);
                result.TotalQuantity += line.Quantity;
                result.TotalValue += line.Quantity * (line.UnitPrice ?? 0);

                var history = new StockMovementHistoryEntity
                {
                    TenantId = tenantId.ToString(),
                    DocumentNumber = docNumber,
                    MaterialDocumentHeaderId = header.Id.ToString(),
                    LineNumber = lineNum,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    MovementType = request.MovementType,
                    MovementTypeDescription = GetMovementTypeDescription(request.MovementType),
                    Quantity = line.Quantity,
                    UOM = line.UOM,
                    UnitPrice = line.UnitPrice ?? 0,
                    TotalValue = line.Quantity * (line.UnitPrice ?? 0),
                    Plant = line.Plant,
                    StorageLocation = line.StorageLocation,
                    BatchNumber = line.BatchNumber,
                    StockBucket = DetermineStockBucket(request.MovementType),
                    StockBefore = impact.QuantityBefore,
                    StockAfter = impact.QuantityAfter,
                    ValueBefore = impact.ValueBefore,
                    ValueAfter = impact.ValueAfter,
                    Reference = request.Reference,
                    PostedBy = userId,
                    MovementDate = now,
                    VendorCode = line.VendorCode,
                    CustomerCode = line.CustomerCode,
                    ProductionOrderNo = line.ProductionOrderNo,
                    PurchaseOrderNo = line.PurchaseOrderNo,
                    SalesOrderNo = line.SalesOrderNo,
                    CostCenter = line.CostCenter,
                    Status = "Posted"
                };
                _db.StockMovementHistory.Add(history);

                if (request.MovementType == 101)
                {
                    var inspectionRequired = await _db.MovementTypes
                        .AsNoTracking()
                        .Where(mt => mt.MovementType == 101 && mt.TenantId == tenantId)
                        .FirstOrDefaultAsync();

                    if (inspectionRequired?.QualityInspectionRequired == true)
                    {
                        var lot = new InspectionLotEntity
                        {
                            LotNumber = $"LOT-{docNumber}-{lineNum}",
                            MaterialCode = line.MaterialCode,
                            MaterialName = line.MaterialName,
                            Plant = line.Plant,
                            StorageLocation = line.StorageLocation,
                            BatchNumber = line.BatchNumber ?? "",
                            InspectionType = "01",
                            Quantity = line.Quantity.ToString(),
                            BaseUOM = line.UOM,
                            Status = "Created",
                            ReferenceOrderNumber = line.PurchaseOrderNo ?? docNumber
                        };
                        _db.InspectionLots.Add(lot);
                        await _db.SaveChangesAsync();
                        result.InspectionLotId = lot.Id;
                        result.InspectionLotNumber = lot.LotNumber;
                    }
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.DocumentNumber = docNumber;
            result.PostingDate = header.PostingDate;
            result.MovementType = request.MovementType;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.Errors.Add($"Movement posting failed: {ex.Message}");
        }

        return result;
    }

    public async Task<PostGoodsMovementResultDto> ReverseMaterialDocumentAsync(string documentNumber, string reason, Guid tenantId, string userId)
    {
        var header = await _db.MaterialDocumentHeaders
            .FirstOrDefaultAsync(h => h.DocumentNumber == documentNumber && h.TenantId == tenantId.ToString());

        if (header == null)
            return new PostGoodsMovementResultDto { Errors = new() { $"Document '{documentNumber}' not found." } };

        var reversalType = GetReversalMovementType(header.MovementType);
        if (reversalType == null)
            return new PostGoodsMovementResultDto { Errors = new() { $"No reversal movement type defined for {header.MovementType}." } };

        var lines = await _db.MaterialDocumentItems
            .Where(i => i.MaterialDocumentHeaderId == header.Id.ToString())
            .ToListAsync();

        var reversalRequest = new PostGoodsMovementRequestDto
        {
            MovementType = reversalType.Value,
            HeaderText = $"Reversal of {documentNumber}: {reason}",
            Reference = documentNumber,
            Lines = lines.Select(l => new PostGoodsMovementLineDto
            {
                MaterialCode = l.MaterialCode,
                MaterialName = l.MaterialName,
                Plant = l.Plant,
                StorageLocation = l.StorageLocation,
                BatchNumber = l.BatchNumber,
                Quantity = l.Quantity,
                UOM = l.UnitOfMeasure,
                UnitPrice = l.UnitPrice,
                VendorCode = l.VendorCode,
                CustomerCode = l.CustomerCode,
                ProductionOrderNo = l.ProductionOrderNo,
                PurchaseOrderNo = l.PurchaseOrderNo,
                SalesOrderNo = l.SalesOrderNo,
                CostCenter = l.CostCenter,
                ProfitCenter = l.ProfitCenter,
                GLAccount = l.GLAccount
            }).ToList()
        };

        var result = await PostMovementAsync(reversalRequest, tenantId, userId);
        if (result.Success)
        {
            header.IsReversal = true;
            header.Status = "Reversed";
            await _db.SaveChangesAsync();
        }
        return result;
    }

    public async Task<Mb51DocumentDto?> GetMaterialDocumentAsync(string documentNumber, Guid tenantId)
    {
        var header = await _db.MaterialDocumentHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.DocumentNumber == documentNumber && h.TenantId == tenantId.ToString());

        if (header == null) return null;

        var firstItem = await _db.MaterialDocumentItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.MaterialDocumentHeaderId == header.Id.ToString());

        return new Mb51DocumentDto
        {
            DocumentNumber = header.DocumentNumber,
            PostingDate = header.PostingDate,
            DocumentDate = header.DocumentDate,
            MovementType = header.MovementType,
            MovementTypeDescription = header.MovementTypeDescription,
            MaterialCode = firstItem?.MaterialCode ?? "",
            MaterialName = firstItem?.MaterialName ?? "",
            Plant = header.Plant,
            StorageLocation = header.StorageLocation,
            BatchNumber = firstItem?.BatchNumber,
            Quantity = header.TotalQuantity,
            UOM = firstItem?.UnitOfMeasure ?? "",
            TotalValue = header.TotalValue,
            Reference = header.RefDocument,
            HeaderText = header.HeaderText,
            PostedBy = header.PostedBy
        };
    }

    private async Task<StockImpactDto> UpdateStockBalanceAsync(
        Guid tenantId, string materialCode, string materialName,
        string plant, string storageLocation, string batchNumber,
        int movementType, decimal quantity, string uom,
        decimal unitPrice, string userId)
    {
        var isReceipt = movementType is 101 or 103 or 561 or 601;
        var qtyDelta = isReceipt ? quantity : -quantity;
        var valueDelta = qtyDelta * unitPrice;

        var balance = await _db.StockBalances
            .FirstOrDefaultAsync(s =>
                s.TenantId == tenantId &&
                s.MaterialCode == materialCode &&
                s.Plant == plant &&
                s.StorageLocation == storageLocation &&
                s.BatchNumber == batchNumber);

        var impact = new StockImpactDto
        {
            MaterialCode = materialCode,
            Plant = plant,
            StorageLocation = storageLocation,
            BatchNumber = batchNumber,
            StockBucket = Enum.Parse<StockBucket>(DetermineStockBucket(movementType))
        };

        if (balance != null)
        {
            impact.QuantityBefore = balance.Quantity;
            impact.ValueBefore = balance.TotalValue;
            balance.Quantity += qtyDelta;
            balance.TotalValue += valueDelta;
            balance.UnitPrice = balance.Quantity > 0 ? balance.TotalValue / balance.Quantity : unitPrice;
            balance.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            balance = new StockBalanceEntity
            {
                TenantId = tenantId,
                MaterialCode = materialCode,
                MaterialName = materialName,
                Plant = plant,
                StorageLocation = storageLocation,
                BatchNumber = batchNumber,
                StockType = DetermineStockBucket(movementType),
                Quantity = qtyDelta,
                UOM = uom,
                UnitPrice = unitPrice,
                TotalValue = valueDelta,
                Status = "Active"
            };
            _db.StockBalances.Add(balance);
            impact.QuantityBefore = 0;
            impact.ValueBefore = 0;
        }

        impact.QuantityAfter = balance.Quantity;
        impact.ValueAfter = balance.TotalValue;
        return impact;
    }

    private static string DetermineStockBucket(int movementType) => movementType switch
    {
        101 or 561 => "QualityInspection",
        321 or 349 or 411 => "Unrestricted",
        322 or 343 => "Blocked",
        103 => "GRBlocked",
        _ => "Unrestricted"
    };

    private static string GetMovementTypeDescription(int mt) => mt switch
    {
        101 => "Goods Receipt for PO",
        102 => "Reversal of GR for PO",
        103 => "GR into GR Blocked Stock",
        201 => "Goods Issue for Cost Center",
        202 => "Reversal of GI for Cost Center",
        261 => "Goods Issue for Production Order",
        262 => "Reversal of GI for Production Order",
        301 => "Transfer Plant to Plant",
        311 => "Transfer SLoc to SLoc",
        321 => "Transfer QI to Unrestricted",
        322 => "Transfer Unrestricted to QI",
        343 => "Transfer Unrestricted to Blocked",
        349 => "Transfer Blocked to Unrestricted",
        411 => "Transfer Consignment to Unrestricted",
        451 => "GI for Return to Vendor",
        541 => "GI for Subcontracting Order",
        561 => "Initial Stock",
        601 => "Goods Receipt for Delivery",
        _ => $"Movement Type {mt}"
    };

    private static int? GetReversalMovementType(int mt) => mt switch
    {
        101 => 102, 102 => 101,
        201 => 202, 202 => 201,
        261 => 262, 262 => 261,
        301 => 302, 311 => 312,
        321 => 322, 322 => 321,
        343 => 344, 349 => 343,
        411 => 412, 451 => 452,
        541 => 542, 561 => 562,
        601 => 602,
        _ => null
    };

    private string GenerateDocumentNumber(Guid tenantId)
    {
        var prefix = $"MB{DateTime.UtcNow:yyyyMMdd}";
        var count = _db.MaterialDocumentHeaders
            .Count(h => h.DocumentNumber.StartsWith(prefix) && h.TenantId == tenantId.ToString());
        return $"{prefix}-{(count + 1):D4}";
    }
}
