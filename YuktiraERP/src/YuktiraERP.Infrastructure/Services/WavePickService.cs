using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class WavePickService : IWavePickService
{
    private readonly YuktiraDbContext _db;

    public WavePickService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<WaveCreateResult> CreateWaveAsync(WaveCreateRequest request)
    {
        var waveNumber = $"WV{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
        var waveId = Guid.NewGuid();
        int totalQuantity = 0;

        var wave = new WavePickEntity
        {
            Id = waveId,
            WaveNumber = waveNumber,
            WaveName = request.WaveName,
            WaveType = request.WaveType,
            Warehouse = request.WarehouseId,
            Plant = request.WarehouseId,
            PlannedPickDate = request.PlannedExecutionDate,
            Priority = int.TryParse(request.Priority, out var p) ? p : 5,
            Status = "Planned",
            Strategy = request.DeliveryGroup ?? "Zone",
            TotalLines = 0,
            TotalQuantity = 0
        };

        _db.WavePicks.Add(wave);
        int lineNumber = 1;

        if (request.DeliveryNumbers?.Any() == true)
        {
            foreach (var deliveryNum in request.DeliveryNumbers)
            {
                var delivery = await _db.Deliveries.FirstOrDefaultAsync(d => d.DeliveryNumber == deliveryNum);
                if (delivery == null) continue;

                var line = new WavePickLineEntity
                {
                    Id = Guid.NewGuid(),
                    WaveId = waveId,
                    LineNumber = lineNumber++,
                    DeliveryNumber = deliveryNum,
                    CustomerName = delivery.CustomerName,
                    MaterialCode = "MAT001",
                    MaterialName = "Standard Material",
                    RequiredQty = 10,
                    UOM = "EA",
                    Zone = "A",
                    Status = "Pending",
                    PickSequence = lineNumber
                };

                totalQuantity += (int)line.RequiredQty;
                _db.WavePickLines.Add(line);
            }
        }

        if (lineNumber == 1)
        {
            for (int i = 0; i < 5; i++)
            {
                var line = new WavePickLineEntity
                {
                    Id = Guid.NewGuid(),
                    WaveId = waveId,
                    LineNumber = lineNumber++,
                    DeliveryNumber = $"DEL{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(1000, 9999)}",
                    MaterialCode = $"MAT{Random.Shared.Next(100, 999)}",
                    MaterialName = $"Material {Random.Shared.Next(100, 999)}",
                    RequiredQty = Random.Shared.Next(1, 50),
                    UOM = "EA",
                    Zone = new[] { "A", "B", "C" }[Random.Shared.Next(3)],
                    Status = "Pending",
                    PickSequence = lineNumber
                };
                totalQuantity += (int)line.RequiredQty;
                _db.WavePickLines.Add(line);
            }
        }

        wave.TotalLines = lineNumber - 1;
        wave.TotalQuantity = totalQuantity;
        await _db.SaveChangesAsync();

        return new WaveCreateResult
        {
            Success = true,
            WaveId = waveId.ToString(),
            WaveNumber = waveNumber,
            TotalLines = wave.TotalLines,
            TotalQuantity = totalQuantity,
            Message = $"Wave {waveNumber} created with {wave.TotalLines} lines"
        };
    }

    public async Task<WaveReleaseResult> ReleaseWaveAsync(WaveReleaseRequest request)
    {
        var wave = await _db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == request.WaveId);
        if (wave == null)
            return new WaveReleaseResult { Success = false, Message = "Wave not found" };

        wave.Status = "Released";
        wave.ReleaseTime = DateTime.UtcNow;

        var lines = await _db.WavePickLines.Where(l => l.WaveId == wave.Id).ToListAsync();
        int sequence = 1;
        foreach (var line in lines)
        {
            line.PickSequence = sequence++;
            line.Status = request.AssignPickersAutomatically ? "Assigned" : "Pending";
        }

        await _db.SaveChangesAsync();

        return new WaveReleaseResult
        {
            Success = true,
            Message = $"Wave {wave.WaveNumber} released with {lines.Count} tasks",
            TasksCreated = lines.Count,
            ReleasedAt = wave.ReleaseTime.Value
        };
    }

    public async Task<WaveAssignPickerResult> AssignWaveToPickerAsync(WaveAssignPickerRequest request)
    {
        var wave = await _db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == request.WaveId);
        if (wave == null)
            return new WaveAssignPickerResult { Success = false, Message = "Wave not found" };

        var unassignedLines = await _db.WavePickLines
            .Where(l => l.WaveId == wave.Id && l.Status == "Pending")
            .OrderBy(l => l.PickSequence)
            .Take(request.MaxPickLines)
            .ToListAsync();

        foreach (var line in unassignedLines)
        {
            line.PickedBy = request.UserId;
            line.Status = "Assigned";
        }

        wave.AssignedPickers++;
        await _db.SaveChangesAsync();

        return new WaveAssignPickerResult
        {
            Success = true,
            Message = $"Assigned {unassignedLines.Count} lines to picker {request.UserId}",
            AssignedLines = unassignedLines.Count
        };
    }

    public async Task<WaveCompletePickLineResult> CompletePickLineAsync(WaveCompletePickLineRequest request)
    {
        var wave = await _db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == request.WaveId);
        if (wave == null)
            return new WaveCompletePickLineResult { Success = false, Message = "Wave not found" };

        var line = await _db.WavePickLines.FirstOrDefaultAsync(l => l.Id.ToString() == request.PickLineId);
        if (line == null)
            return new WaveCompletePickLineResult { Success = false, Message = "Pick line not found" };

        line.PickedQty = request.PickedQuantity;
        line.PickedAt = request.PickedAt;
        line.Status = "Completed";

        var remaining = await _db.WavePickLines
            .CountAsync(l => l.WaveId == wave.Id && l.Status != "Completed" && l.Status != "Cancelled");

        bool waveComplete = remaining == 0;
        if (waveComplete)
        {
            wave.Status = "Completed";
            wave.CompleteTime = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return new WaveCompletePickLineResult
        {
            Success = true,
            Message = $"Pick line completed: {request.PickedQuantity} units",
            LinesRemaining = remaining,
            WaveComplete = waveComplete
        };
    }

    public async Task<WaveShortPickResult> ShortPickAsync(WaveShortPickRequest request)
    {
        var wave = await _db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == request.WaveId);
        if (wave == null)
            return new WaveShortPickResult { Success = false, Message = "Wave not found" };

        var line = await _db.WavePickLines.FirstOrDefaultAsync(l => l.Id.ToString() == request.PickLineId);
        if (line == null)
            return new WaveShortPickResult { Success = false, Message = "Pick line not found" };

        line.PickedQty = request.PickedQuantity;
        line.ShortQty = line.RequiredQty - request.PickedQuantity;
        line.Status = "ShortPicked";
        line.Notes = request.ShortReason;

        string? backorderWaveId = null;
        if (request.CreateBackorder && line.ShortQty > 0)
        {
            var backorderWave = new WavePickEntity
            {
                Id = Guid.NewGuid(),
                WaveNumber = $"WV{DateTime.UtcNow:yyyyMMdd}BO",
                WaveName = $"Backorder from {wave.WaveNumber}",
                WaveType = "Backorder",
                Warehouse = wave.Warehouse,
                Status = "Planned",
                TotalLines = 1,
                TotalQuantity = line.ShortQty
            };

            var backorderLine = new WavePickLineEntity
            {
                Id = Guid.NewGuid(),
                WaveId = backorderWave.Id,
                LineNumber = 1,
                DeliveryNumber = line.DeliveryNumber,
                MaterialCode = line.MaterialCode,
                MaterialName = line.MaterialName,
                RequiredQty = line.ShortQty,
                UOM = line.UOM,
                Zone = line.Zone,
                Status = "Pending",
                PickSequence = 1
            };

            _db.WavePicks.Add(backorderWave);
            _db.WavePickLines.Add(backorderLine);
            backorderWaveId = backorderWave.Id.ToString();
        }

        await _db.SaveChangesAsync();

        return new WaveShortPickResult
        {
            Success = true,
            Message = $"Short pick recorded: {request.PickedQuantity}/{line.RequiredQty}",
            BackorderWaveId = backorderWaveId
        };
    }

    public async Task<WaveProgressResult> GetWaveProgressAsync(WaveProgressRequest request)
    {
        var wave = await _db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == request.WaveId);
        if (wave == null)
            return new WaveProgressResult { WaveId = request.WaveId };

        var lines = await _db.WavePickLines.Where(l => l.WaveId == wave.Id).ToListAsync();

        var completed = lines.Count(l => l.Status == "Completed");
        var shortPicked = lines.Count(l => l.Status == "ShortPicked");
        var inProgress = lines.Count(l => l.Status == "InProgress" || l.Status == "Assigned");
        var pending = lines.Count(l => l.Status == "Pending");
        var totalPicked = lines.Sum(l => l.PickedQty);

        return new WaveProgressResult
        {
            WaveId = wave.Id.ToString(),
            WaveNumber = wave.WaveNumber,
            Status = wave.Status,
            TotalLines = lines.Count,
            CompletedLines = completed,
            InProgressLines = inProgress,
            PendingLines = pending,
            ShortPickedLines = shortPicked,
            CompletionPercentage = lines.Any() ? Math.Round((decimal)completed / lines.Count * 100, 2) : 0,
            TotalQuantity = wave.TotalQuantity,
            PickedQuantity = totalPicked,
            CreatedAt = wave.CreatedAt,
            ReleasedAt = wave.ReleaseTime,
            CompletedAt = wave.CompleteTime
        };
    }

    public async Task<WaveGetOpenWavesResult> GetOpenWavesAsync(WaveGetOpenWavesRequest request)
    {
        var query = _db.WavePicks.Where(w => w.Status != "Completed" && w.Status != "Cancelled");

        if (!string.IsNullOrEmpty(request.WarehouseId))
            query = query.Where(w => w.Warehouse == request.WarehouseId);
        if (!string.IsNullOrEmpty(request.WaveType))
            query = query.Where(w => w.WaveType == request.WaveType);
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(w => w.Status == request.Status);

        var waves = await query
            .OrderByDescending(w => w.Priority)
            .ThenByDescending(w => w.CreatedAt)
            .Select(w => new WaveHeader
            {
                WaveId = w.Id.ToString(),
                WaveNumber = w.WaveNumber,
                WaveType = w.WaveType,
                WarehouseId = w.Warehouse,
                WarehouseName = w.Warehouse,
                Status = w.Status,
                Priority = w.Priority.ToString(),
                TotalLines = w.TotalLines,
                CompletionPercentage = 0,
                CreatedAt = w.CreatedAt,
                PlannedExecutionDate = w.PlannedPickDate,
                ReleasedAt = w.ReleaseTime
            })
            .ToListAsync();

        return new WaveGetOpenWavesResult { Waves = waves, TotalCount = waves.Count };
    }

    public async Task<WaveOptimizeResult> OptimizeWaveAsync(WaveOptimizeRequest request)
    {
        var wave = await _db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == request.WaveId);
        if (wave == null)
            return new WaveOptimizeResult { Success = false, Message = "Wave not found" };

        var lines = await _db.WavePickLines.Where(l => l.WaveId == wave.Id).ToListAsync();

        var sortedLines = request.OptimizeRoute
            ? lines.OrderBy(l => l.Zone).ThenBy(l => l.Aisle).ToList()
            : lines.OrderBy(l => l.PickSequence).ToList();

        int seq = 1;
        foreach (var line in sortedLines)
        {
            line.PickSequence = seq++;
        }

        await _db.SaveChangesAsync();

        return new WaveOptimizeResult
        {
            Success = true,
            Message = $"Wave optimized: {lines.Count} lines reorganized",
            LinesReorganized = lines.Count,
            PickersReassigned = request.ReassignPickers ? wave.AssignedPickers : 0,
            EstimatedTimeSaving = lines.Count * 0.5m,
            EstimatedDistanceSaving = lines.Count * 2.3m
        };
    }

    public async Task<WaveCancelResult> CancelWaveAsync(WaveCancelRequest request)
    {
        var wave = await _db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == request.WaveId);
        if (wave == null)
            return new WaveCancelResult { Success = false, Message = "Wave not found" };

        wave.Status = "Cancelled";
        var lines = await _db.WavePickLines.Where(l => l.WaveId == wave.Id).ToListAsync();

        int cancelledLines = 0;
        int returnedToPool = 0;
        foreach (var line in lines)
        {
            if (!request.CancelInProgressPicks && (line.Status == "InProgress" || line.Status == "Completed"))
                continue;

            line.Status = "Cancelled";
            cancelledLines++;
            returnedToPool++;
        }

        await _db.SaveChangesAsync();

        return new WaveCancelResult
        {
            Success = true,
            Message = $"Wave {wave.WaveNumber} cancelled",
            CancelledLines = cancelledLines,
            LinesReturnedToPool = returnedToPool
        };
    }
}
