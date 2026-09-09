using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class StockOverviewService : IStockOverviewService
{
    private readonly YuktiraDbContext _db;

    public StockOverviewService(YuktiraDbContext db) => _db = db;

    public async Task<StockOverviewResultDto> GetStockOverviewHierarchyAsync(
        StockOverviewFilterDto filter, Guid tenantId)
    {
        var query = _db.StockItems.Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(filter.MaterialCode))
            query = query.Where(s => s.MaterialName.Contains(filter.MaterialCode));
        if (!string.IsNullOrWhiteSpace(filter.StorageLocationId))
            query = query.Where(s => s.Bin == filter.StorageLocationId);
        if (!string.IsNullOrWhiteSpace(filter.BatchNumber))
            query = query.Where(s => s.Lot == filter.BatchNumber);

        var stockItems = await query.ToListAsync();

        var reservations = await _db.StockReservations
            .Where(r => r.Status == "Active")
            .ToListAsync();

        var materials = await _db.MaterialMasters
            .Where(m => m.Status == "Active")
            .ToListAsync();

        var plants = await _db.Plants
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .ToListAsync();

        var storageLocations = await _db.StorageLocations
            .Where(s => s.Status == "Active")
            .ToListAsync();

        var materialDict = materials.ToDictionary(m => m.Name, m => m);
        var plantDict = plants.ToDictionary(p => p.Code, p => p);

        var nodes = new List<StockOverviewNodeDto>();
        var materialNodes = new Dictionary<string, StockOverviewNodeDto>();

        var groupedByMaterial = stockItems
            .GroupBy(s => s.MaterialName)
            .OrderBy(g => g.Key);

        foreach (var matGroup in groupedByMaterial)
        {
            var materialName = matGroup.Key;
            materialDict.TryGetValue(materialName, out var material);

            var matNode = new StockOverviewNodeDto
            {
                Id = Guid.NewGuid(),
                Level = StockOverviewLevel.Client,
                EntityCode = material?.Code ?? materialName,
                EntityName = materialName,
                HasChildren = true,
                IsExpanded = true,
                MaterialCode = material?.Code ?? "",
                MaterialName = materialName,
                MaterialType = material?.Type ?? "",
                UOM = material?.UOM ?? "",
                UnitPrice = material?.Price ?? 0
            };

            var plantGroups = matGroup.GroupBy(s => DerivePlantCode(s.Bin, plants));

            foreach (var plantGroup in plantGroups)
            {
                var plantCode = plantGroup.Key;
                plantDict.TryGetValue(plantCode, out var plant);

                var plantNode = new StockOverviewNodeDto
                {
                    Id = Guid.NewGuid(),
                    Level = StockOverviewLevel.Plant,
                    EntityCode = plantCode,
                    EntityName = plant?.Name ?? plantCode,
                    ParentNodeId = matNode.Id.ToString(),
                    HasChildren = true,
                    MaterialCode = matNode.MaterialCode,
                    MaterialName = materialName,
                    MaterialType = matNode.MaterialType,
                    UOM = matNode.UOM,
                    UnitPrice = matNode.UnitPrice
                };

                var slGroups = plantGroup.GroupBy(s => s.Bin);

                foreach (var slGroup in slGroups)
                {
                    var binCode = slGroup.Key;
                    var slName = storageLocations.FirstOrDefault(sl => sl.Code == binCode)?.Name ?? binCode;

                    var slNode = new StockOverviewNodeDto
                    {
                        Id = Guid.NewGuid(),
                        Level = StockOverviewLevel.StorageLocation,
                        EntityCode = binCode,
                        EntityName = slName,
                        ParentNodeId = plantNode.Id.ToString(),
                        HasChildren = slGroup.Any(s => !string.IsNullOrEmpty(s.Lot)),
                        MaterialCode = matNode.MaterialCode,
                        MaterialName = materialName,
                        MaterialType = matNode.MaterialType,
                        UOM = matNode.UOM,
                        UnitPrice = matNode.UnitPrice
                    };

                    foreach (var stockItem in slGroup)
                    {
                        var batchNode = new StockOverviewNodeDto
                        {
                            Id = Guid.NewGuid(),
                            Level = StockOverviewLevel.Batch,
                            EntityCode = stockItem.Lot ?? "NO-BATCH",
                            EntityName = stockItem.Lot ?? "No Batch",
                            ParentNodeId = slNode.Id.ToString(),
                            HasChildren = false,
                            MaterialCode = matNode.MaterialCode,
                            MaterialName = materialName,
                            MaterialType = matNode.MaterialType,
                            UOM = matNode.UOM,
                            UnitPrice = matNode.UnitPrice
                        };

                        var cat = ClassifyStockBin(binCode);
                        SetCategoryQuantity(batchNode, cat, stockItem.Quantity);

                        AccumulateToNode(slNode, batchNode);
                        nodes.Add(batchNode);
                    }

                    var slHasAnyStock = slNode.TotalStock > 0;
                    if (slHasAnyStock || filter.IncludeZeroStocks)
                    {
                        AccumulateToNode(plantNode, slNode);
                        plantNode.HasChildren = true;
                        nodes.Add(slNode);
                    }
                }

                if (plantNode.TotalStock > 0 || filter.IncludeZeroStocks)
                {
                    AccumulateToNode(matNode, plantNode);
                    matNode.HasChildren = true;
                    nodes.Add(plantNode);
                }
            }

            if (matNode.TotalStock > 0 || filter.IncludeZeroStocks)
            {
                materialNodes[materialName] = matNode;
                nodes.Add(matNode);
            }
        }

        var result = new StockOverviewResultDto
        {
            Filter = filter,
            Nodes = nodes,
            TotalMaterials = materialNodes.Count,
            TotalStorageLocations = nodes.Count(n => n.Level == StockOverviewLevel.StorageLocation),
            TotalBatches = nodes.Count(n => n.Level == StockOverviewLevel.Batch),
            GrandTotalQuantity = materialNodes.Values.Sum(n => n.TotalStock),
            GrandTotalValue = materialNodes.Values.Sum(n => n.TotalValue)
        };

        return result;
    }

    private static string DerivePlantCode(string bin, List<PlantEntity> plants)
    {
        if (plants.Count == 0) return "1000";

        var match = plants.FirstOrDefault(p => bin.StartsWith(p.Code, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match.Code;

        if (bin.StartsWith("SL-", StringComparison.OrdinalIgnoreCase))
        {
            var parts = bin.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[1], out _))
                return "1000";
        }

        return plants.First().Code;
    }

    private static string ClassifyStockBin(string bin)
    {
        var upper = bin.ToUpperInvariant();
        if (upper.Contains("QI") || upper.Contains("QUARANTINE") || upper == "SL-01")
            return "QI";
        if (upper.Contains("BLOCKED") || upper.Contains("BLK"))
            return "BLOCKED";
        if (upper.Contains("RESERVED") || upper.Contains("RES"))
            return "RESERVED";
        if (upper.Contains("RETURNS") || upper.Contains("RET"))
            return "RETURNS";
        if (upper.Contains("TRANSIT") || upper.Contains("TRN"))
            return "TRANSIT";
        if (upper.Contains("FG") || upper.Contains("FINISHED"))
            return "UNRESTRICTED";
        if (upper.Contains("PROD") || upper.Contains("FLOOR"))
            return "UNRESTRICTED";
        return "UNRESTRICTED";
    }

    private static void SetCategoryQuantity(StockOverviewNodeDto node, string category, decimal qty)
    {
        switch (category)
        {
            case "QI": node.QualityInspection += qty; break;
            case "BLOCKED": node.Blocked += qty; break;
            case "RESERVED": node.Reserved += qty; break;
            case "RETURNS": node.Returns += qty; break;
            case "TRANSIT": node.TransferStockPlant += qty; break;
            default: node.UnrestrictedUse += qty; break;
        }
    }

    private static void AccumulateToNode(StockOverviewNodeDto parent, StockOverviewNodeDto child)
    {
        parent.UnrestrictedUse += child.UnrestrictedUse;
        parent.QualityInspection += child.QualityInspection;
        parent.Reserved += child.Reserved;
        parent.ReceiptReserved += child.ReceiptReserved;
        parent.ScheduledForDelivery += child.ScheduledForDelivery;
        parent.Returns += child.Returns;
        parent.Blocked += child.Blocked;
        parent.TransferStockPlant += child.TransferStockPlant;
        parent.TransferStockSL += child.TransferStockSL;
    }
}
