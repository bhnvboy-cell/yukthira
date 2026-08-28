using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class RFWarehouseService : IRFWarehouseService
{
    private readonly YuktiraDbContext _db;

    public RFWarehouseService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<RFSessionStartResult> StartSessionAsync(RFSessionStartRequest request)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
        var session = new RFSessionEntity
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = request.UserId,
            TerminalId = request.DeviceId,
            Plant = request.StationId,
            Warehouse = request.WarehouseId,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            Status = "Active",
            DeviceType = "Mobile",
            TransactionCount = 0
        };

        _db.RFSessions.Add(session);
        await _db.SaveChangesAsync();

        return new RFSessionStartResult
        {
            Success = true,
            SessionId = sessionId,
            WarehouseName = request.WarehouseId,
            StartedAt = session.StartedAt
        };
    }

    public async Task<RFSessionEndResult> EndSessionAsync(RFSessionEndRequest request)
    {
        var session = await _db.RFSessions.FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
        if (session == null)
            return new RFSessionEndResult { Success = false, Message = "Session not found" };

        session.Status = "Ended";
        session.EndedAt = DateTime.UtcNow;
        var completedTasks = await _db.RFPickTasks
            .CountAsync(t => t.AssignedTo == session.UserId && t.Status == "Completed");

        await _db.SaveChangesAsync();

        return new RFSessionEndResult
        {
            Success = true,
            Message = "Session ended successfully",
            TasksCompleted = completedTasks
        };
    }

    public async Task<RFMenuResult> GetMenuAsync(RFMenuRequest request)
    {
        var menuItems = await _db.RFMenuItems
            .Where(m => m.IsActive)
            .OrderBy(m => m.SequenceOrder)
            .Select(m => new RFMenuItem
            {
                MenuItemId = m.MenuCode,
                Label = m.MenuName,
                Action = m.TransactionType,
                Icon = m.IconClass,
                IsAvailable = m.IsActive,
                Tooltip = m.Description
            })
            .ToListAsync();

        return new RFMenuResult
        {
            Items = menuItems,
            CurrentPath = request.MenuPath ?? "/",
            Breadcrumb = "Home"
        };
    }

    public async Task<RFScanValidateResult> ValidateScanAsync(RFScanValidateRequest request)
    {
        var session = await _db.RFSessions.FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
        if (session == null)
            return new RFScanValidateResult { IsValid = false, DisplayValue = "Invalid session" };

        session.LastActivityAt = DateTime.UtcNow;

        var material = await _db.MaterialMasters
            .FirstOrDefaultAsync(m => m.Code == request.ScannedCode || m.Name == request.ScannedCode);

        if (material != null)
        {
            var stock = await _db.StockItems.FirstOrDefaultAsync(s => s.MaterialName == material.Name);
            await _db.SaveChangesAsync();

            return new RFScanValidateResult
            {
                IsValid = true,
                CodeType = "Material",
                DisplayValue = material.Name,
                Material = material.Code,
                MaterialDescription = material.Name,
                Quantity = stock?.Quantity ?? 0,
                UnitOfMeasure = material.UOM,
                Messages = new List<string> { $"Material {material.Name} validated" }
            };
        }

        var bin = await _db.BinMasters
            .FirstOrDefaultAsync(b => b.BinCode == request.ScannedCode);

        if (bin != null)
        {
            await _db.SaveChangesAsync();
            return new RFScanValidateResult
            {
                IsValid = true,
                CodeType = "Bin",
                DisplayValue = bin.BinCode,
                WarehouseId = bin.Warehouse,
                StorageLocation = bin.Zone,
                Messages = new List<string> { $"Bin {bin.BinCode} in zone {bin.Zone}" }
            };
        }

        var stockItem = await _db.StockItems.FirstOrDefaultAsync(s => s.Bin == request.ScannedCode);
        if (stockItem != null)
        {
            await _db.SaveChangesAsync();
            return new RFScanValidateResult
            {
                IsValid = true,
                CodeType = "StockLocation",
                DisplayValue = stockItem.Bin,
                Material = stockItem.MaterialName,
                Quantity = stockItem.Quantity,
                UnitOfMeasure = stockItem.UOM,
                Messages = new List<string> { $"Stock at {stockItem.Bin}: {stockItem.Quantity} {stockItem.UOM}" }
            };
        }

        await _db.SaveChangesAsync();
        return new RFScanValidateResult
        {
            IsValid = false,
            CodeType = "Unknown",
            DisplayValue = request.ScannedCode,
            Messages = new List<string> { $"Code {request.ScannedCode} not recognized" }
        };
    }

    public async Task<RFPickPostResult> PostPickAsync(RFPickPostRequest request)
    {
        var session = await _db.RFSessions.FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
        if (session == null)
            return new RFPickPostResult { Success = false, Message = "Session not found" };

        var pickTask = await _db.RFPickTasks.FirstOrDefaultAsync(t => t.TaskId == request.TaskId);
        if (pickTask == null)
            return new RFPickPostResult { Success = false, Message = "Pick task not found" };

        pickTask.PickedQty += request.Quantity;
        pickTask.ScanCount++;
        pickTask.StartedAt ??= DateTime.UtcNow;

        if (pickTask.PickedQty >= pickTask.RequiredQty)
        {
            pickTask.Status = "Completed";
            pickTask.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            pickTask.Status = "InProgress";
        }

        var transaction = new RFTransactionEntity
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TransactionId = session.TransactionCount + 1,
            TransactionType = "PICK",
            MaterialCode = request.Material,
            MaterialName = request.Material,
            Quantity = request.Quantity,
            UOM = request.UnitOfMeasure,
            FromBin = request.SourceBin,
            ToBin = request.DestinationBin,
            BatchNumber = request.Batch ?? "",
            ScanTimestamp = DateTime.UtcNow,
            Success = true
        };

        session.TransactionCount++;
        session.LastActivityAt = DateTime.UtcNow;

        _db.RFTransactions.Add(transaction);
        await _db.SaveChangesAsync();

        return new RFPickPostResult
        {
            Success = true,
            Message = $"Picked {request.Quantity} {request.UnitOfMeasure} of {request.Material}",
            PickedQuantity = pickTask.PickedQty
        };
    }

    public async Task<RFPutawayPostResult> PostPutawayAsync(RFPutawayPostRequest request)
    {
        var session = await _db.RFSessions.FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
        if (session == null)
            return new RFPutawayPostResult { Success = false, Message = "Session not found" };

        var transaction = new RFTransactionEntity
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TransactionId = session.TransactionCount + 1,
            TransactionType = "PUTAWAY",
            MaterialCode = request.Material,
            MaterialName = request.Material,
            Quantity = request.Quantity,
            UOM = request.UnitOfMeasure,
            ToBin = request.DestinationBin,
            BatchNumber = request.Batch ?? "",
            ScanTimestamp = DateTime.UtcNow,
            Success = true
        };

        session.TransactionCount++;
        _db.RFTransactions.Add(transaction);
        await _db.SaveChangesAsync();

        return new RFPutawayPostResult
        {
            Success = true,
            Message = $"Putaway {request.Quantity} {request.UnitOfMeasure} to {request.DestinationBin}",
            PutawayConfirmation = $"PUT{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}"
        };
    }

    public async Task<RFTransferPostResult> PostTransferAsync(RFTransferPostRequest request)
    {
        var session = await _db.RFSessions.FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
        if (session == null)
            return new RFTransferPostResult { Success = false, Message = "Session not found" };

        var transfer = new WarehouseTransferEntity
        {
            TransferId = $"TO{DateTime.UtcNow:yyyyMMddHHmmss}",
            Date = DateTime.UtcNow,
            MaterialName = request.Material,
            FromBin = request.SourceBin,
            ToBin = request.DestinationBin,
            Quantity = request.Quantity,
            Status = "Completed"
        };

        var transaction = new RFTransactionEntity
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TransactionId = session.TransactionCount + 1,
            TransactionType = "TRANSFER",
            MaterialCode = request.Material,
            Quantity = request.Quantity,
            UOM = request.UnitOfMeasure,
            FromBin = request.SourceBin,
            ToBin = request.DestinationBin,
            BatchNumber = request.Batch ?? "",
            ScanTimestamp = DateTime.UtcNow,
            Success = true
        };

        session.TransactionCount++;
        _db.WarehouseTransfers.Add(transfer);
        _db.RFTransactions.Add(transaction);
        await _db.SaveChangesAsync();

        return new RFTransferPostResult
        {
            Success = true,
            Message = $"Transfer completed from {request.SourceBin} to {request.DestinationBin}",
            TransferOrder = transfer.TransferId
        };
    }

    public async Task<RFCycleCountPostResult> PostCycleCountAsync(RFCycleCountPostRequest request)
    {
        var stockItem = await _db.StockItems.FirstOrDefaultAsync(s =>
            s.MaterialName == request.Material && s.Bin == request.Bin);

        var systemQty = stockItem?.Quantity ?? 0;
        var variance = request.CountedQuantity - systemQty;

        var countTask = new RFCountTaskEntity
        {
            Id = Guid.NewGuid(),
            CycleCountId = request.CycleCountId,
            TaskId = $"CC{DateTime.UtcNow:yyyyMMddHHmmss}",
            Bin = request.Bin,
            MaterialCode = request.Material,
            MaterialName = request.Material,
            SystemQuantity = systemQty,
            CountedQuantity = request.CountedQuantity,
            Variance = variance,
            VariancePercent = systemQty != 0 ? Math.Round(variance / systemQty * 100, 2) : 0,
            CountedBy = request.SessionId,
            CountedAt = DateTime.UtcNow,
            Status = "Completed"
        };

        _db.RFCountTasks.Add(countTask);
        await _db.SaveChangesAsync();

        return new RFCycleCountPostResult
        {
            Success = true,
            Message = $"Count completed: System={systemQty}, Counted={request.CountedQuantity}",
            SystemQuantity = systemQty,
            Variance = variance,
            VarianceReason = variance != 0 ? "Variance detected" : "No variance"
        };
    }

    public async Task<RFGetOpenTasksResult> GetOpenTasksAsync(RFGetOpenTasksRequest request)
    {
        var query = _db.RFPickTasks.Where(t => t.Status != "Completed" && t.Status != "Cancelled");

        if (!string.IsNullOrEmpty(request.TaskType))
            query = query.Where(t => t.PickMethod == request.TaskType);

        var tasks = await query
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.SequenceOrder)
            .Select(t => new RFOpenTask
            {
                TaskId = t.TaskId,
                TaskType = t.PickMethod,
                Description = $"{t.MaterialCode} - {t.MaterialName}",
                Priority = t.Priority.ToString(),
                CreatedAt = t.CreatedAt,
                Material = t.MaterialCode,
                MaterialDescription = t.MaterialName,
                SourceBin = t.SourceBin,
                DestinationBin = t.DestinationBin,
                Quantity = t.RequiredQty - t.PickedQty,
                IsOverdue = false
            })
            .ToListAsync();

        return new RFGetOpenTasksResult
        {
            Tasks = tasks,
            TotalOpenCount = tasks.Count,
            OverdueCount = 0
        };
    }

    public async Task<RFGetTaskDetailsResult> GetTaskDetailsAsync(RFGetTaskDetailsRequest request)
    {
        var task = await _db.RFPickTasks.FirstOrDefaultAsync(t => t.TaskId == request.TaskId);
        if (task == null)
            return new RFGetTaskDetailsResult { TaskId = request.TaskId };

        return new RFGetTaskDetailsResult
        {
            TaskId = task.TaskId,
            TaskType = task.PickMethod,
            Description = $"{task.MaterialCode} - {task.MaterialName}",
            Priority = task.Priority.ToString(),
            Status = task.Status,
            Material = task.MaterialCode,
            MaterialDescription = task.MaterialName,
            SourceBin = task.SourceBin,
            DestinationBin = task.DestinationBin,
            Quantity = task.RequiredQty,
            UnitOfMeasure = task.UnitOfMeasure,
            Batch = task.BatchNumber,
            WaveId = task.WaveNumber,
            ScanInstructions = new List<RFScanInstruction>
            {
                new() { InstructionType = "SourceBin", ExpectedCode = task.SourceBin, Prompt = "Scan source bin", IsRequired = true },
                new() { InstructionType = "Material", ExpectedCode = task.MaterialCode, Prompt = "Scan material", IsRequired = true },
                new() { InstructionType = "DestinationBin", ExpectedCode = task.DestinationBin, Prompt = "Scan destination bin", IsRequired = true }
            }
        };
    }

    public async Task<RFGetSessionSummaryResult> GetSessionSummaryAsync(RFGetSessionSummaryRequest request)
    {
        var session = await _db.RFSessions.FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
        if (session == null)
            return new RFGetSessionSummaryResult { SessionId = request.SessionId };

        var transactions = await _db.RFTransactions
            .Where(t => t.SessionId == session.Id)
            .ToListAsync();

        return new RFGetSessionSummaryResult
        {
            SessionId = session.SessionId,
            UserId = session.UserId,
            WarehouseId = session.Warehouse,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            TasksStarted = transactions.Count(t => t.TransactionType == "PICK"),
            TasksCompleted = transactions.Count(t => t.TransactionType == "PICK" && t.Success),
            UnitsPicked = (int)transactions.Where(t => t.TransactionType == "PICK").Sum(t => t.Quantity),
            UnitsPutaway = (int)transactions.Where(t => t.TransactionType == "PUTAWAY").Sum(t => t.Quantity),
            TransfersCompleted = transactions.Count(t => t.TransactionType == "TRANSFER"),
            CycleCountsCompleted = transactions.Count(t => t.TransactionType == "COUNT"),
            ScanCount = transactions.Count,
            ErrorCount = transactions.Count(t => !t.Success)
        };
    }

    public async Task<RFGetWarehouseMapResult> GetWarehouseMapAsync(RFGetWarehouseMapRequest request)
    {
        var bins = await _db.BinMasters
            .Where(b => b.Warehouse == request.WarehouseId)
            .ToListAsync();

        var zones = bins
            .GroupBy(b => b.Zone)
            .Select(g => new WarehouseZone
            {
                ZoneId = g.Key,
                ZoneName = g.Key,
                ZoneType = "Storage",
                Bins = g.Select(b => new WarehouseBin
                {
                    BinId = b.BinCode,
                    BinType = b.BinType,
                    IsOccupied = b.CurrentOccupancy > 0,
                    IsAvailable = b.Status == "Active",
                    CapacityUsed = b.CurrentOccupancy,
                    CapacityTotal = b.Capacity
                }).ToList(),
                CapacityUtilization = g.Any() ? g.Sum(b => b.CurrentOccupancy) / g.Sum(b => b.Capacity) * 100 : 0
            })
            .ToList();

        var totalBins = bins.Count;
        var occupiedBins = bins.Count(b => b.CurrentOccupancy > 0);

        return new RFGetWarehouseMapResult
        {
            WarehouseId = request.WarehouseId,
            WarehouseName = request.WarehouseId,
            Zones = zones,
            TotalBins = totalBins,
            OccupiedBins = occupiedBins,
            UtilizationPercentage = totalBins > 0 ? Math.Round((decimal)occupiedBins / totalBins * 100, 2) : 0
        };
    }
}
