using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class PutawayStrategyService : IPutawayStrategyService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<PutawayStrategyService> _logger;

    public PutawayStrategyService(YuktiraDbContext db, ILogger<PutawayStrategyService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PutawayResult> ExecutePutawayAsync(PutawayRequest request)
    {
        try
        {
            var bins = await _db.Bins
                .Where(b => b.WarehouseNumber == request.WarehouseNumber && b.Status == "Active")
                .ToListAsync();

            var selectedBin = request.Strategy switch
            {
                PutawayStrategy.FixedBin => bins.FirstOrDefault(b => b.BinType == "Fixed" && b.CurrentVolume < b.MaxVolume),
                PutawayStrategy.NearPickArea => bins.OrderBy(b => b.CurrentVolume).FirstOrDefault(b => b.CurrentVolume < b.MaxVolume),
                PutawayStrategy.ABCClassification => bins.OrderByDescending(b => b.MaxWeight).FirstOrDefault(b => b.CurrentVolume < b.MaxVolume),
                PutawayStrategy.RandomStorage => bins.FirstOrDefault(b => b.CurrentVolume < b.MaxVolume * 0.8m),
                PutawayStrategy.Consolidation => bins.FirstOrDefault(b => b.CurrentVolume > 0 && b.CurrentVolume < b.MaxVolume),
                _ => bins.FirstOrDefault(b => b.CurrentVolume < b.MaxVolume)
            };

            if (selectedBin == null)
                return new PutawayResult { Success = false, Errors = { "No suitable bin found for putaway" } };

            selectedBin.CurrentVolume += request.Quantity;
            selectedBin.CurrentWeight += request.Quantity;
            selectedBin.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var requiresConsolidation = selectedBin.CurrentVolume > selectedBin.MaxVolume * 0.9m;

            _logger.LogInformation("Putaway executed: Material={Material}, Bin={Bin}, Strategy={Strategy}",
                request.MaterialCode, selectedBin.BinCode, request.Strategy);

            return new PutawayResult
            {
                Success = true,
                AssignedBin = selectedBin.BinCode,
                Zone = selectedBin.StorageSection,
                BinCapacity = selectedBin.MaxVolume,
                RemainingCapacity = selectedBin.MaxVolume - selectedBin.CurrentVolume,
                RequiresConsolidation = requiresConsolidation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Putaway failed for material {Material}", request.MaterialCode);
            return new PutawayResult { Success = false, Errors = { ex.Message } };
        }
    }

    public async Task<List<PutawayResult>> BatchPutawayAsync(IEnumerable<PutawayRequest> requests, Guid tenantId)
    {
        var results = new List<PutawayResult>();
        foreach (var request in requests)
        {
            results.Add(await ExecutePutawayAsync(request));
        }
        return results;
    }
}

public class CrossDockService : ICrossDockService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<CrossDockService> _logger;

    public CrossDockService(YuktiraDbContext db, ILogger<CrossDockService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CrossDockResult> EvaluateCrossDockAsync(CrossDockRequest request)
    {
        try
        {
            var pendingDeliveries = await _db.Deliveries
                .Where(d => d.Status == "Pending" || d.Status == "InTransit")
                .ToListAsync();

            var matchingDemand = pendingDeliveries.Any();

            var eligible = request.Quantity > 0 && matchingDemand;

            return new CrossDockResult
            {
                Success = true,
                EligibleForCrossDock = eligible,
                AssignedBin = eligible ? "XDOCK-001" : null,
                DemandMatchQuantity = eligible ? Math.Min(request.Quantity, request.Quantity) : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cross-dock evaluation failed");
            return new CrossDockResult { Success = false, Errors = { ex.Message } };
        }
    }

    public async Task<List<CrossDockResult>> BatchCrossDockAsync(IEnumerable<CrossDockRequest> requests, Guid tenantId)
    {
        var results = new List<CrossDockResult>();
        foreach (var request in requests)
        {
            results.Add(await EvaluateCrossDockAsync(request));
        }
        return results;
    }
}
