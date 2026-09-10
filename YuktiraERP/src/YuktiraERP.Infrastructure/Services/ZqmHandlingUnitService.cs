using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ZqmHandlingUnitService : IZqmHandlingUnitService
{
    private readonly YuktiraDbContext _db;

    public ZqmHandlingUnitService(YuktiraDbContext db) => _db = db;

    public async Task<HandlingUnitResult> CreateHandlingUnitAsync(HandlingUnitCreateRequest request)
    {
        var result = new HandlingUnitResult();

        var lot = await _db.InspectionLots.FirstOrDefaultAsync(l => l.LotNumber == request.InspectionLotNumber);
        if (lot != null && lot.Status != "UsageDecisionCompleted" && lot.Status != "Completed")
        {
            result.Errors.Add($"Lot {request.InspectionLotNumber} must have a completed usage decision before creating HU.");
            return result;
        }

        var huNumber = await GenerateHUNumberAsync();
        var hu = new HandlingUnitEntity
        {
            HUNumber = huNumber,
            MaterialCode = request.MaterialCode,
            MaterialName = request.MaterialName,
            BatchNumber = request.BatchNumber,
            Plant = request.Plant,
            StorageLocation = request.StorageLocation,
            WarehouseNumber = request.WarehouseNumber,
            PackagingMaterial = request.PackagingMaterial,
            Quantity = request.Quantity,
            BaseUOM = request.BaseUOM,
            GrossWeight = request.GrossWeight,
            NetWeight = request.NetWeight,
            WeightUOM = request.WeightUOM,
            Volume = request.Volume,
            VolumeUOM = request.VolumeUOM,
            PackageType = request.PackageType,
            Status = "Created",
            InspectionLotNumber = request.InspectionLotNumber,
            QualityStatus = lot?.Status ?? "",
            CreatedAt = DateTime.UtcNow
        };

        _db.HandlingUnits.Add(hu);
        await _db.SaveChangesAsync();

        result.Success = true;
        result.HUNumber = huNumber;
        result.HUId = hu.Id;
        result.Status = "Created";
        result.WarehouseQueueUrl = $"/Areas/WM/Pages/Packaging/PackQueue?hu={huNumber}";

        return result;
    }

    public async Task<HandlingUnitResult> ReleaseHandlingUnitAsync(Guid huId, string userId)
    {
        var result = new HandlingUnitResult();

        var hu = await _db.HandlingUnits.FindAsync(huId);
        if (hu == null)
        {
            result.Errors.Add($"Handling unit {huId} not found.");
            return result;
        }

        if (hu.Status != "Packed" && hu.Status != "Weighed")
        {
            result.Errors.Add($"HU {hu.HUNumber} is in status '{hu.Status}'. Must be Packed or Weighed to release.");
            return result;
        }

        hu.Status = "Released";
        hu.ReleasedAt = DateTime.UtcNow;
        hu.ReleasedBy = userId;
        hu.UpdatedAt = DateTime.UtcNow;
        _db.HandlingUnits.Update(hu);
        await _db.SaveChangesAsync();

        result.Success = true;
        result.HUNumber = hu.HUNumber;
        result.HUId = hu.Id;
        result.Status = "Released";

        return result;
    }

    public async Task<List<HandlingUnitDto>> GetHandlingUnitsByLotAsync(string lotNumber, Guid tenantId)
    {
        return await _db.HandlingUnits
            .Where(h => h.InspectionLotNumber == lotNumber)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new HandlingUnitDto
            {
                Id = h.Id, HUNumber = h.HUNumber, MaterialCode = h.MaterialCode, MaterialName = h.MaterialName,
                BatchNumber = h.BatchNumber, Plant = h.Plant, WarehouseNumber = h.WarehouseNumber,
                Quantity = h.Quantity, BaseUOM = h.BaseUOM, GrossWeight = h.GrossWeight, NetWeight = h.NetWeight,
                Status = h.Status, InspectionLotNumber = h.InspectionLotNumber, CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<HandlingUnitDto>> GetPackagingQueueAsync(string warehouseNumber, Guid tenantId)
    {
        return await _db.HandlingUnits
            .Where(h => h.WarehouseNumber == warehouseNumber &&
                       (h.Status == "Packed" || h.Status == "Weighed" || h.Status == "Created"))
            .OrderBy(h => h.CreatedAt)
            .Select(h => new HandlingUnitDto
            {
                Id = h.Id, HUNumber = h.HUNumber, MaterialCode = h.MaterialCode, MaterialName = h.MaterialName,
                BatchNumber = h.BatchNumber, Plant = h.Plant, WarehouseNumber = h.WarehouseNumber,
                Quantity = h.Quantity, BaseUOM = h.BaseUOM, GrossWeight = h.GrossWeight, NetWeight = h.NetWeight,
                Status = h.Status, InspectionLotNumber = h.InspectionLotNumber, CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }

    private async Task<string> GenerateHUNumberAsync()
    {
        var lastHU = await _db.HandlingUnits
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => h.HUNumber)
            .FirstOrDefaultAsync();

        if (lastHU != null && lastHU.StartsWith("HU") && int.TryParse(lastHU.AsSpan(2), out var lastNum))
        {
            return $"HU{(lastNum + 1):D10}";
        }
        return $"HU{DateTime.UtcNow:yyyyMMdd}{new Random().Next(100, 999)}";
    }
}
