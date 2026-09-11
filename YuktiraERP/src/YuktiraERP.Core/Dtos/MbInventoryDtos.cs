using YuktiraERP.Core.Enums;

namespace YuktiraERP.Core.Dtos;

public class PostGoodsMovementRequestDto
{
    public int MovementType { get; set; }
    public string PostingDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string DocumentDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string? HeaderText { get; set; }
    public string? Reference { get; set; }
    public List<PostGoodsMovementLineDto> Lines { get; set; } = new();
}

public class PostGoodsMovementLineDto
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "1000";
    public string StorageLocation { get; set; } = "";
    public string? BatchNumber { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal? UnitPrice { get; set; }
    public string? VendorCode { get; set; }
    public string? VendorName { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? ProductionOrderNo { get; set; }
    public string? PurchaseOrderNo { get; set; }
    public string? SalesOrderNo { get; set; }
    public string? CostCenter { get; set; }
    public string? ProfitCenter { get; set; }
    public string? GLAccount { get; set; }
    public string? SpecialStockIndicator { get; set; }
    public string? ItemText { get; set; }
    public string? ToPlant { get; set; }
    public string? ToStorageLocation { get; set; }
}

public class PostGoodsMovementResultDto
{
    public bool Success { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime PostingDate { get; set; }
    public int MovementType { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public List<StockImpactDto> StockImpacts { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public Guid? InspectionLotId { get; set; }
    public string? InspectionLotNumber { get; set; }
}

public class StockImpactDto
{
    public string MaterialCode { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string? BatchNumber { get; set; }
    public StockBucket StockBucket { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal QuantityAfter { get; set; }
    public decimal ValueBefore { get; set; }
    public decimal ValueAfter { get; set; }
}

public class CreateReservationDto
{
    public string RequirementDate { get; set; } = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
    public string? CostCenter { get; set; }
    public string? OrderNumber { get; set; }
    public string? ProductionOrderNo { get; set; }
    public string? SalesOrderNo { get; set; }
    public string? WBSElement { get; set; }
    public string? HeaderText { get; set; }
    public MbReservationType ReservationType { get; set; } = MbReservationType.Manual;
    public List<CreateReservationLineDto> Lines { get; set; } = new();
}

public class CreateReservationLineDto
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "1000";
    public string StorageLocation { get; set; } = "";
    public string? BatchNumber { get; set; }
    public decimal RequiredQuantity { get; set; }
    public string UOM { get; set; } = "EA";
    public string? ItemText { get; set; }
}

public class ReservationResultDto
{
    public bool Success { get; set; }
    public string? ReservationNumber { get; set; }
    public DateTime RequirementDate { get; set; }
    public decimal TotalReservedQuantity { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class Mb51FilterDto
{
    public string? MaterialCode { get; set; }
    public string? MaterialName { get; set; }
    public string? Plant { get; set; }
    public string? StorageLocation { get; set; }
    public string? BatchNumber { get; set; }
    public int? MovementType { get; set; }
    public DateTime? PostingDateFrom { get; set; }
    public DateTime? PostingDateTo { get; set; }
    public DateTime? DocumentDateFrom { get; set; }
    public DateTime? DocumentDateTo { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Reference { get; set; }
    public string? PostedBy { get; set; }
    public int PageSize { get; set; } = 100;
    public int PageNumber { get; set; } = 1;
}

public class Mb51DocumentDto
{
    public string DocumentNumber { get; set; } = "";
    public DateTime PostingDate { get; set; }
    public DateTime DocumentDate { get; set; }
    public int MovementType { get; set; }
    public string MovementTypeDescription { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string? BatchNumber { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public string? VendorCode { get; set; }
    public string? VendorName { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? Reference { get; set; }
    public string? HeaderText { get; set; }
    public string? PostedBy { get; set; }
    public string StockBucket { get; set; } = "";
}

public class Mb52StockOverviewDto
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string PlantName { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string StorageLocationName { get; set; } = "";
    public string? BatchNumber { get; set; }
    public decimal UnrestrictedQty { get; set; }
    public decimal QualityInspectionQty { get; set; }
    public decimal BlockedQty { get; set; }
    public decimal GRBlockedQty { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal TotalQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal MinStock { get; set; }
    public decimal MaxStock { get; set; }
    public bool IsBelowMinStock { get; set; }
    public bool IsAboveMaxStock { get; set; }
}

public class Mb5BHistoricalStockDto
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string? BatchNumber { get; set; }
    public DateTime ValuationDate { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal OpeningValue { get; set; }
    public decimal ReceiptQuantity { get; set; }
    public decimal ReceiptValue { get; set; }
    public decimal IssueQuantity { get; set; }
    public decimal IssueValue { get; set; }
    public decimal ClosingQuantity { get; set; }
    public decimal ClosingValue { get; set; }
    public decimal MovingAveragePrice { get; set; }
    public List<Mb5BDailyMovementDto> DailyMovements { get; set; } = new();
}

public class Mb5BDailyMovementDto
{
    public DateTime Date { get; set; }
    public int MovementType { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public decimal RunningBalance { get; set; }
    public decimal RunningValue { get; set; }
}

public class Mb5LReconciliationDto
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string? BatchNumber { get; set; }
    public decimal MmStockQuantity { get; set; }
    public decimal MmStockValue { get; set; }
    public decimal FiGlDebit { get; set; }
    public decimal FiGlCredit { get; set; }
    public decimal FiGlBalance { get; set; }
    public decimal Difference { get; set; }
    public bool IsReconciled { get; set; }
    public string? DiscrepancyReason { get; set; }
}

public class MbExcelExportRequestDto
{
    public string ReportType { get; set; } = "MB51";
    public Mb51FilterDto? Mb51Filter { get; set; }
    public Mb52StockOverviewFilterDto? Mb52Filter { get; set; }
    public bool IncludeHeaders { get; set; } = true;
    public bool FreezePanes { get; set; } = true;
    public string? Currency { get; set; }
}

public class Mb52StockOverviewFilterDto
{
    public string? MaterialCode { get; set; }
    public string? Plant { get; set; }
    public string? StorageLocation { get; set; }
    public string? BatchNumber { get; set; }
    public bool IncludeZeroStock { get; set; } = true;
    public bool IncludeNegativeStock { get; set; } = true;
    public MbStockOverviewLevel GroupBy { get; set; } = MbStockOverviewLevel.Material;
}
