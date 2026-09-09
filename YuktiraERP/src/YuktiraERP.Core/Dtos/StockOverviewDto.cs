namespace YuktiraERP.Core.Dtos;

public enum StockOverviewLevel
{
    Client = 0,
    Plant = 1,
    StorageLocation = 2,
    Batch = 3,
    SpecialStock = 4
}

public class StockOverviewFilterDto
{
    public string? MaterialCode { get; set; }
    public string? PlantId { get; set; }
    public string? StorageLocationId { get; set; }
    public string? BatchNumber { get; set; }
    public bool IncludeZeroStocks { get; set; }
    public List<string>? StockTypes { get; set; }
}

public class StockOverviewNodeDto
{
    public Guid Id { get; set; }
    public StockOverviewLevel Level { get; set; }
    public string EntityCode { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? ParentNodeId { get; set; }
    public bool HasChildren { get; set; }
    public bool IsExpanded { get; set; }

    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string MaterialType { get; set; } = "";
    public string UOM { get; set; } = "";

    public decimal UnrestrictedUse { get; set; }
    public decimal QualityInspection { get; set; }
    public decimal Reserved { get; set; }
    public decimal ReceiptReserved { get; set; }
    public decimal ScheduledForDelivery { get; set; }
    public decimal Returns { get; set; }
    public decimal Blocked { get; set; }
    public decimal TransferStockPlant { get; set; }
    public decimal TransferStockSL { get; set; }

    public decimal TotalStock => UnrestrictedUse + QualityInspection + Reserved
        + ReceiptReserved + ScheduledForDelivery + Returns + Blocked
        + TransferStockPlant + TransferStockSL;

    public decimal UnitPrice { get; set; }
    public decimal TotalValue => TotalStock * UnitPrice;
}

public class StockOverviewResultDto
{
    public StockOverviewFilterDto Filter { get; set; } = new();
    public List<StockOverviewNodeDto> Nodes { get; set; } = new();
    public int TotalMaterials { get; set; }
    public int TotalStorageLocations { get; set; }
    public int TotalBatches { get; set; }
    public decimal GrandTotalQuantity { get; set; }
    public decimal GrandTotalValue { get; set; }
}
