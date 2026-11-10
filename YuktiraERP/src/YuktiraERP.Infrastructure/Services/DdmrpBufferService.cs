using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class DdmrpBufferService : IDdmrpBufferService
{
    private readonly YuktiraDbContext _db;

    public DdmrpBufferService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<DdmrpBufferZoneDto> CalculateBufferZonesAsync(Guid tenantId, string materialCode, string plant)
    {
        var targetPlant = string.IsNullOrWhiteSpace(plant) ? "1000" : plant;
        var material = await _db.MaterialMasters
            .FirstOrDefaultAsync(m => m.Code == materialCode);

        var matName = material?.Name ?? materialCode;
        var leadTimeDays = 7; // Default decoupled lead time
        var moq = 100m;

        // Calculate ADU from recent historical stock movements (past 90 days)
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var recentUsage = await _db.StockMovements
            .Where(m => m.TenantId == tenantId && m.MaterialCode == materialCode && m.CreatedAt >= cutoff && (m.MovementType == "201" || m.MovementType == "601"))
            .SumAsync(m => (decimal?)m.Quantity) ?? 0m;

        var adu = recentUsage > 0 ? Math.Round(recentUsage / 90m, 2) : 25m; // Fallback baseline of 25 units/day

        // DDMRP Decoupled Buffer Formulas
        var leadTimeFactor = 0.5m;
        var variabilityFactor = 0.4m;

        var yellow = Math.Round(adu * leadTimeDays, 2);
        var red = Math.Round(yellow * leadTimeFactor * (1.0m + variabilityFactor), 2);
        var green = Math.Max(moq, Math.Round(yellow * leadTimeFactor, 2));

        // Evaluate Current Stock & Pipeline Positions
        var onHand = await _db.StockBalances
            .Where(s => s.TenantId == tenantId && s.MaterialCode == materialCode && s.Plant == targetPlant)
            .SumAsync(s => (decimal?)s.Quantity) ?? 0m;

        var openPoSupply = await _db.PurchaseOrderItems
            .Where(p => p.MaterialCode == materialCode)
            .SumAsync(p => (decimal?)p.Quantity) ?? 0m;

        var openDemand = await _db.SalesOrderLines
            .Where(s => s.MaterialName == materialCode)
            .SumAsync(s => (decimal?)s.Quantity) ?? 0m;

        var topOfRed = red;
        var topOfYellow = red + yellow;
        var topOfGreen = red + yellow + green;

        var netFlow = onHand + openPoSupply - openDemand;

        string status;
        decimal recommendedQty = 0m;

        if (netFlow <= topOfRed)
        {
            status = "Critical-Red";
            recommendedQty = Math.Max(moq, topOfGreen - netFlow);
        }
        else if (netFlow <= topOfYellow)
        {
            status = "Reorder-Yellow";
            recommendedQty = Math.Max(moq, topOfGreen - netFlow);
        }
        else if (netFlow > topOfGreen * 1.25m)
        {
            status = "Overstock-Blue";
        }
        else
        {
            status = "Healthy-Green";
        }

        return new DdmrpBufferZoneDto
        {
            MaterialCode = materialCode,
            MaterialName = matName,
            Plant = targetPlant,
            AverageDailyUsage = adu,
            DecoupledLeadTimeDays = leadTimeDays,
            MinimumOrderQuantity = moq,
            LeadTimeFactor = leadTimeFactor,
            VariabilityFactor = variabilityFactor,
            RedZone = red,
            YellowZone = yellow,
            GreenZone = green,
            OnHandStock = onHand,
            OpenSupplyQuantity = openPoSupply,
            QualifiedDemandSpikes = openDemand,
            BufferStatus = status,
            RecommendedOrderQuantity = recommendedQty
        };
    }

    public async Task<List<DdmrpBufferZoneDto>> EvaluateAllBuffersAsync(Guid tenantId, string plant)
    {
        var targetPlant = string.IsNullOrWhiteSpace(plant) ? "1000" : plant;
        var materials = await _db.MaterialMasters
            .Take(50)
            .ToListAsync();

        var results = new List<DdmrpBufferZoneDto>();
        foreach (var mat in materials)
        {
            results.Add(await CalculateBufferZonesAsync(tenantId, mat.Code, targetPlant));
        }

        return results;
    }

    public async Task<List<DdmrpBufferZoneDto>> GetReplenishmentProposalsAsync(Guid tenantId, string plant)
    {
        var all = await EvaluateAllBuffersAsync(tenantId, plant);
        return all.Where(b => b.BufferStatus == "Critical-Red" || b.BufferStatus == "Reorder-Yellow").ToList();
    }
}
