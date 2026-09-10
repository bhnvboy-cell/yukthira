using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ZqmAutoLotGeneratorService : IZqmAutoLotGeneratorService
{
    private readonly YuktiraDbContext _db;

    public ZqmAutoLotGeneratorService(YuktiraDbContext db) => _db = db;

    public async Task<bool> IsQmEnabledMaterialAsync(string materialCode, string plant, Guid tenantId)
    {
        return await _db.QMMasterDatas.AnyAsync(m =>
            m.MaterialCode == materialCode &&
            m.Plant == plant &&
            m.IsActive);
    }

    public async Task<InspectionLotAutoGenResult> GenerateInspectionLotAsync(InspectionLotAutoGenRequest request)
    {
        var result = new InspectionLotAutoGenResult();

        var qmConfig = await _db.QMInspectionConfigs.FirstOrDefaultAsync(c =>
            c.ConfigName == request.MaterialCode &&
            c.Plant == request.Plant &&
            c.Status == "ACTIVE");

        if (qmConfig == null)
        {
            qmConfig = await _db.QMInspectionConfigs.FirstOrDefaultAsync(c =>
                c.Plant == request.Plant &&
                c.Status == "ACTIVE");
        }

        var masterData = await _db.QMMasterDatas.FirstOrDefaultAsync(m =>
            m.MaterialCode == request.MaterialCode &&
            m.Plant == request.Plant);

        if (masterData == null)
        {
            result.Errors.Add($"No QM master data found for material {request.MaterialCode} at plant {request.Plant}.");
            return result;
        }

        var origin = request.MovementType switch
        {
            101 or 103 => "01",
            4 or 5 => "04",
            8 => "08",
            _ => "01"
        };

        var inspectionType = masterData.InspectionType ?? qmConfig?.InspectionType ?? "01";
        var sampleSize = CalculateSampleSize(request.Quantity, masterData.SampleProcedure);

        var lotNumber = await GenerateLotNumberAsync();
        var inspector = masterData.CreatedBy ?? "AUTO";

        var lot = new InspectionLotEntity
        {
            LotNumber = lotNumber,
            MaterialCode = request.MaterialCode,
            MaterialName = request.MaterialName,
            Plant = request.Plant,
            BatchNumber = request.BatchNumber,
            InspectionType = inspectionType,
            Quantity = request.Quantity.ToString("F2"),
            BaseUOM = request.BaseUOM,
            SampleSize = sampleSize,
            Status = "Created",
            AssignedInspector = inspector,
            ReferenceOrderNumber = request.PurchaseOrderNumber,
            CreatedAt = DateTime.UtcNow
        };

        _db.InspectionLots.Add(lot);
        await _db.SaveChangesAsync();

        var plan = await _db.InspectionPlans.FirstOrDefaultAsync(p =>
            p.MaterialCode == lot.MaterialCode && p.Status == "Active");

        if (plan != null)
        {
            var resultEntity = new InspectionResultEntity
            {
                LotNumber = lot.LotNumber,
                Characteristic = plan.Characteristic,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _db.InspectionResults.Add(resultEntity);
            await _db.SaveChangesAsync();
        }

        result.Success = true;
        result.LotNumber = lotNumber;
        result.LotId = lot.Id;
        result.InspectionType = inspectionType;
        result.Origin = origin;
        result.SampleSize = sampleSize;
        result.AssignedInspector = inspector;

        return result;
    }

    private static int CalculateSampleSize(decimal quantity, string? procedure)
    {
        return procedure?.ToUpperInvariant() switch
        {
            "AQL" => quantity <= 50 ? 5 : quantity <= 500 ? 20 : quantity <= 5000 ? 50 : 80,
            "SKIP" => 0,
            "FIXED" => 10,
            _ => quantity <= 50 ? 5 : quantity <= 500 ? 20 : 50
        };
    }

    private async Task<string> GenerateLotNumberAsync()
    {
        var lastLot = await _db.InspectionLots
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => l.LotNumber)
            .FirstOrDefaultAsync();

        if (lastLot != null && lastLot.StartsWith("Q") && int.TryParse(lastLot.AsSpan(1), out var lastNum))
        {
            return $"Q{(lastNum + 1):D9}";
        }
        return $"Q{DateTime.UtcNow:yyyyMMdd}{new Random().Next(100, 999)}";
    }
}
