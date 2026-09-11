using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class OfflineQueueService : IOfflineQueueService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<OfflineQueueService> _logger;
    private readonly IMobileNotificationService _notificationService;

    public OfflineQueueService(
        YuktiraDbContext db,
        ILogger<OfflineQueueService> logger,
        IMobileNotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<OfflineQueueProcessResult> ProcessOfflineQueueAsync(IEnumerable<OfflineTransactionDto> queue, Guid tenantId)
    {
        var result = new OfflineQueueProcessResult();
        var processedIds = new HashSet<Guid>();

        foreach (var txn in queue)
        {
            if (processedIds.Contains(txn.Id))
            {
                _logger.LogDebug("Skipping duplicate transaction {TxnId}", txn.Id);
                continue;
            }

            processedIds.Add(txn.Id);

            try
            {
                var txnResult = await ProcessSingleTransactionAsync(txn, tenantId);
                result.Results.Add(txnResult);

                if (txnResult.Status == OfflineTransactionStatus.Completed)
                    result.SucceededCount++;
                else if (txnResult.Status == OfflineTransactionStatus.ConflictDetected)
                    result.ConflictsDetected++;
                else
                    result.FailedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process offline transaction {TxnId}", txn.Id);
                result.Results.Add(new OfflineTransactionResult
                {
                    TransactionId = txn.Id,
                    Status = OfflineTransactionStatus.Failed,
                    ErrorMessage = ex.Message
                });
                result.FailedCount++;
            }
        }

        result.Success = result.FailedCount == 0;
        result.TotalProcessed = processedIds.Count;

        _logger.LogInformation(
            "Offline queue processed: Total={Total}, Succeeded={Succeeded}, Failed={Failed}, Conflicts={Conflicts}",
            result.TotalProcessed, result.SucceededCount, result.FailedCount, result.ConflictsDetected);

        return result;
    }

    public async Task<OfflineConflictResult> ResolveConflictAsync(OfflineConflictDto conflict, Guid tenantId)
    {
        try
        {
            return conflict.Strategy switch
            {
                ConflictResolutionStrategy.ServerWins => new OfflineConflictResult
                {
                    Success = true,
                    AppliedStrategy = ConflictResolutionStrategy.ServerWins,
                    MergedPayload = conflict.ServerVersion
                },
                ConflictResolutionStrategy.ClientWins => new OfflineConflictResult
                {
                    Success = true,
                    AppliedStrategy = ConflictResolutionStrategy.ClientWins,
                    MergedPayload = conflict.ClientVersion
                },
                ConflictResolutionStrategy.Merge => await MergeConflictAsync(conflict, tenantId),
                ConflictResolutionStrategy.ManualReview => new OfflineConflictResult
                {
                    Success = true,
                    AppliedStrategy = ConflictResolutionStrategy.ManualReview,
                    MergedPayload = $"CONFLICT_REVIEW_REQUIRED: Entity={conflict.EntityName}, Id={conflict.EntityId}"
                },
                _ => new OfflineConflictResult { Success = false, Errors = { "Unknown resolution strategy" } }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conflict resolution failed for transaction {TxnId}", conflict.TransactionId);
            return new OfflineConflictResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<List<OfflineTransactionDto>> GetPendingTransactionsAsync(Guid tenantId, string deviceId)
    {
        return Task.FromResult(new List<OfflineTransactionDto>());
    }

    private async Task<OfflineTransactionResult> ProcessSingleTransactionAsync(OfflineTransactionDto txn, Guid tenantId)
    {
        var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(txn.Payload) ?? new();

        switch (txn.TransactionType)
        {
            case OfflineTransactionType.GoodsReceipt:
                return await ProcessGoodsReceiptAsync(txn, payload, tenantId);
            case OfflineTransactionType.GoodsIssue:
                return await ProcessGoodsIssueAsync(txn, payload, tenantId);
            case OfflineTransactionType.StockTransfer:
                return await ProcessStockTransferAsync(txn, payload, tenantId);
            case OfflineTransactionType.PhysicalCount:
                return await ProcessPhysicalCountAsync(txn, payload, tenantId);
            case OfflineTransactionType.InspectionResult:
                return await ProcessInspectionResultAsync(txn, payload, tenantId);
            case OfflineTransactionType.UsageDecision:
                return await ProcessUsageDecisionAsync(txn, payload, tenantId);
            default:
                return new OfflineTransactionResult
                {
                    TransactionId = txn.Id,
                    Status = OfflineTransactionStatus.Failed,
                    ErrorMessage = $"Unsupported transaction type: {txn.TransactionType}"
                };
        }
    }

    private async Task<OfflineTransactionResult> ProcessGoodsReceiptAsync(
        OfflineTransactionDto txn, Dictionary<string, object?> payload, Guid tenantId)
    {
        var materialCode = payload["MaterialCode"]?.ToString() ?? "";
        var plant = payload["Plant"]?.ToString() ?? "";
        var quantity = Convert.ToDecimal(payload["Quantity"] ?? 0);
        var storageLocation = payload["StorageLocation"]?.ToString() ?? "";

        var stock = await _db.StockBalances.FirstOrDefaultAsync(s =>
            s.TenantId == tenantId && s.MaterialCode == materialCode &&
            s.Plant == plant && s.StorageLocation == storageLocation);

        if (stock == null)
        {
            stock = new StockBalanceEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                MaterialCode = materialCode,
                Plant = plant,
                StorageLocation = storageLocation,
                Quantity = quantity,
                UOM = payload["UOM"]?.ToString() ?? "EA",
                CreatedAt = DateTime.UtcNow
            };
            _db.StockBalances.Add(stock);
        }
        else
        {
            stock.Quantity += quantity;
            stock.UpdatedAt = DateTime.UtcNow;
        }

        await RecordStockMovementAsync(tenantId, materialCode, plant, storageLocation,
            quantity, 101, txn.IdempotencyKey, txn.ClientTimestamp);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Offline GR processed: Material={Material}, Qty={Qty}, Plant={Plant}",
            materialCode, quantity, plant);

        return new OfflineTransactionResult
        {
            TransactionId = txn.Id,
            Status = OfflineTransactionStatus.Completed,
            ServerDocumentId = stock.Id.ToString()
        };
    }

    private async Task<OfflineTransactionResult> ProcessGoodsIssueAsync(
        OfflineTransactionDto txn, Dictionary<string, object?> payload, Guid tenantId)
    {
        var materialCode = payload["MaterialCode"]?.ToString() ?? "";
        var plant = payload["Plant"]?.ToString() ?? "";
        var quantity = Convert.ToDecimal(payload["Quantity"] ?? 0);
        var storageLocation = payload["StorageLocation"]?.ToString() ?? "";

        var stock = await _db.StockBalances.FirstOrDefaultAsync(s =>
            s.TenantId == tenantId && s.MaterialCode == materialCode &&
            s.Plant == plant && s.StorageLocation == storageLocation);

        if (stock == null || stock.Quantity < quantity)
        {
            return new OfflineTransactionResult
            {
                TransactionId = txn.Id,
                Status = OfflineTransactionStatus.Failed,
                ErrorMessage = $"Insufficient stock: Available={stock?.Quantity ?? 0}, Requested={quantity}"
            };
        }

        stock.Quantity -= quantity;
        stock.UpdatedAt = DateTime.UtcNow;

        await RecordStockMovementAsync(tenantId, materialCode, plant, storageLocation,
            -quantity, 601, txn.IdempotencyKey, txn.ClientTimestamp);

        await _db.SaveChangesAsync();

        if (stock.Quantity <= stock.ReorderPoint)
        {
            await _notificationService.SendLowStockAlertAsync(new LowStockNotificationDto
            {
                MaterialCode = materialCode,
                MaterialName = stock.MaterialName,
                Plant = plant,
                StorageLocation = storageLocation,
                CurrentQuantity = stock.Quantity,
                MinimumQuantity = stock.MinStock,
                Unit = stock.UOM
            }, tenantId);
        }

        return new OfflineTransactionResult
        {
            TransactionId = txn.Id,
            Status = OfflineTransactionStatus.Completed,
            ServerDocumentId = stock.Id.ToString()
        };
    }

    private async Task<OfflineTransactionResult> ProcessStockTransferAsync(
        OfflineTransactionDto txn, Dictionary<string, object?> payload, Guid tenantId)
    {
        var materialCode = payload["MaterialCode"]?.ToString() ?? "";
        var fromPlant = payload["FromPlant"]?.ToString() ?? "";
        var toPlant = payload["ToPlant"]?.ToString() ?? "";
        var fromLocation = payload["FromStorageLocation"]?.ToString() ?? "";
        var toLocation = payload["ToStorageLocation"]?.ToString() ?? "";
        var quantity = Convert.ToDecimal(payload["Quantity"] ?? 0);

        var sourceStock = await _db.StockBalances.FirstOrDefaultAsync(s =>
            s.TenantId == tenantId && s.MaterialCode == materialCode &&
            s.Plant == fromPlant && s.StorageLocation == fromLocation);

        if (sourceStock == null || sourceStock.Quantity < quantity)
        {
            return new OfflineTransactionResult
            {
                TransactionId = txn.Id,
                Status = OfflineTransactionStatus.Failed,
                ErrorMessage = $"Insufficient source stock: Available={sourceStock?.Quantity ?? 0}"
            };
        }

        sourceStock.Quantity -= quantity;
        sourceStock.UpdatedAt = DateTime.UtcNow;

        var targetStock = await _db.StockBalances.FirstOrDefaultAsync(s =>
            s.TenantId == tenantId && s.MaterialCode == materialCode &&
            s.Plant == toPlant && s.StorageLocation == toLocation);

        if (targetStock == null)
        {
            targetStock = new StockBalanceEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                MaterialCode = materialCode,
                Plant = toPlant,
                StorageLocation = toLocation,
                Quantity = quantity,
                UOM = sourceStock.UOM,
                CreatedAt = DateTime.UtcNow
            };
            _db.StockBalances.Add(targetStock);
        }
        else
        {
            targetStock.Quantity += quantity;
            targetStock.UpdatedAt = DateTime.UtcNow;
        }

        await RecordStockMovementAsync(tenantId, materialCode, fromPlant, fromLocation,
            -quantity, 301, txn.IdempotencyKey, txn.ClientTimestamp);
        await RecordStockMovementAsync(tenantId, materialCode, toPlant, toLocation,
            quantity, 301, null, DateTime.UtcNow);

        await _db.SaveChangesAsync();

        return new OfflineTransactionResult
        {
            TransactionId = txn.Id,
            Status = OfflineTransactionStatus.Completed,
            ServerDocumentId = targetStock.Id.ToString()
        };
    }

    private async Task<OfflineTransactionResult> ProcessPhysicalCountAsync(
        OfflineTransactionDto txn, Dictionary<string, object?> payload, Guid tenantId)
    {
        var materialCode = payload["MaterialCode"]?.ToString() ?? "";
        var plant = payload["Plant"]?.ToString() ?? "";
        var countedQty = Convert.ToDecimal(payload["CountedQuantity"] ?? 0);
        var storageLocation = payload["StorageLocation"]?.ToString() ?? "";

        var stock = await _db.StockBalances.FirstOrDefaultAsync(s =>
            s.TenantId == tenantId && s.MaterialCode == materialCode &&
            s.Plant == plant && s.StorageLocation == storageLocation);

        if (stock == null)
            return new OfflineTransactionResult
            {
                TransactionId = txn.Id,
                Status = OfflineTransactionStatus.Failed,
                ErrorMessage = "Stock record not found for physical count"
            };

        var variance = countedQty - stock.Quantity;
        stock.Quantity = countedQty;
        stock.UpdatedAt = DateTime.UtcNow;

        if (variance != 0)
        {
            await RecordStockMovementAsync(tenantId, materialCode, plant, storageLocation,
                variance, 701, txn.IdempotencyKey, txn.ClientTimestamp);
        }

        await _db.SaveChangesAsync();

        return new OfflineTransactionResult
        {
            TransactionId = txn.Id,
            Status = OfflineTransactionStatus.Completed,
            ServerDocumentId = stock.Id.ToString()
        };
    }

    private async Task<OfflineTransactionResult> ProcessInspectionResultAsync(
        OfflineTransactionDto txn, Dictionary<string, object?> payload, Guid tenantId)
    {
        var lotNumber = payload["InspectionLotNumber"]?.ToString() ?? "";
        var result = payload["Result"]?.ToString() ?? "Pass";

        _logger.LogInformation("Offline inspection result: Lot={Lot}, Result={Result}", lotNumber, result);

        return new OfflineTransactionResult
        {
            TransactionId = txn.Id,
            Status = OfflineTransactionStatus.Completed,
            ServerDocumentId = lotNumber
        };
    }

    private async Task<OfflineTransactionResult> ProcessUsageDecisionAsync(
        OfflineTransactionDto txn, Dictionary<string, object?> payload, Guid tenantId)
    {
        var lotNumber = payload["InspectionLotNumber"]?.ToString() ?? "";
        var decision = payload["Decision"]?.ToString() ?? "Accepted";

        _logger.LogInformation("Offline usage decision: Lot={Lot}, Decision={Decision}", lotNumber, decision);

        return new OfflineTransactionResult
        {
            TransactionId = txn.Id,
            Status = OfflineTransactionStatus.Completed,
            ServerDocumentId = lotNumber
        };
    }

    private async Task<OfflineConflictResult> MergeConflictAsync(OfflineConflictDto conflict, Guid tenantId)
    {
        try
        {
            var clientData = JsonSerializer.Deserialize<Dictionary<string, object?>>(conflict.ClientVersion) ?? new();
            var serverData = JsonSerializer.Deserialize<Dictionary<string, object?>>(conflict.ServerVersion) ?? new();

            var merged = new Dictionary<string, object?>(serverData);

            foreach (var kv in clientData)
            {
                if (!merged.ContainsKey(kv.Key) || kv.Value != null)
                    merged[kv.Key] = kv.Value;
            }

            merged["ConflictResolved"] = true;
            merged["ResolvedAt"] = DateTime.UtcNow.ToString("O");

            return new OfflineConflictResult
            {
                Success = true,
                AppliedStrategy = ConflictResolutionStrategy.Merge,
                MergedPayload = JsonSerializer.Serialize(merged)
            };
        }
        catch (Exception ex)
        {
            return new OfflineConflictResult { Success = false, Errors = { $"Merge failed: {ex.Message}" } };
        }
    }

    private async Task RecordStockMovementAsync(
        Guid tenantId, string materialCode, string plant, string storageLocation,
        decimal quantity, int movementType, string? idempotencyKey, DateTime timestamp)
    {
        var movement = new StockMovementHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.ToString(),
            MaterialCode = materialCode,
            Plant = plant,
            StorageLocation = storageLocation,
            Quantity = quantity,
            MovementType = movementType,
            UOM = "EA",
            MovementDate = timestamp,
            CreatedAt = timestamp
        };
        _db.StockMovementHistory.Add(movement);
    }

    private static string GenerateIdempotencyKey(Guid tenantId, string materialCode, DateTime timestamp)
    {
        var input = $"{tenantId}:{materialCode}:{timestamp:O}";
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)))[..32];
    }
}
