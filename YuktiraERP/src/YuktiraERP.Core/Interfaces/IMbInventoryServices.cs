using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IInventoryMovementService
{
    Task<PostGoodsMovementResultDto> PostGoodsReceiptAsync(PostGoodsMovementRequestDto request, Guid tenantId, string userId);
    Task<PostGoodsMovementResultDto> PostGoodsIssueAsync(PostGoodsMovementRequestDto request, Guid tenantId, string userId);
    Task<PostGoodsMovementResultDto> PostTransferPostingAsync(PostGoodsMovementRequestDto request, Guid tenantId, string userId);
    Task<PostGoodsMovementResultDto> PostMovementAsync(PostGoodsMovementRequestDto request, Guid tenantId, string userId);
    Task<PostGoodsMovementResultDto> ReverseMaterialDocumentAsync(string documentNumber, string reason, Guid tenantId, string userId);
    Task<Mb51DocumentDto?> GetMaterialDocumentAsync(string documentNumber, Guid tenantId);
}

public interface IStockReservationService
{
    Task<ReservationResultDto> CreateReservationAsync(CreateReservationDto request, Guid tenantId, string userId);
    Task<ReservationResultDto> UpdateReservationAsync(string reservationNumber, CreateReservationDto request, Guid tenantId, string userId);
    Task<ReservationResultDto> DeleteReservationAsync(string reservationNumber, string reason, Guid tenantId, string userId);
    Task<ReservationResultDto> IssueAgainstReservationAsync(string reservationNumber, List<CreateReservationLineDto> issuedLines, Guid tenantId, string userId);
    Task<decimal> GetAvailableQuantityAsync(string materialCode, string plant, string? storageLocation, string? batchNumber, Guid tenantId);
    Task<List<ReservationHeaderDto>> GetReservationsAsync(string? materialCode, string? plant, Guid tenantId);
}

public interface IInventoryReportingService
{
    Task<List<Mb51DocumentDto>> GetMb51DocumentsAsync(Mb51FilterDto filter, Guid tenantId);
    Task<List<Mb52StockOverviewDto>> GetMb52StockOverviewAsync(Mb52StockOverviewFilterDto filter, Guid tenantId);
    Task<List<Mb5BHistoricalStockDto>> GetMb5BHistoricalStockAsync(string materialCode, string plant, DateTime fromDate, DateTime toDate, Guid tenantId);
    Task<List<Mb5LReconciliationDto>> GetMb5LReconciliationAsync(string? materialCode, string? plant, Guid tenantId);
    Task<byte[]> ExportToExcelAsync(MbExcelExportRequestDto request, Guid tenantId);
}

public interface IInventoryValuationService
{
    Task<decimal> CalculateMovingAveragePriceAsync(string materialCode, string plant, Guid tenantId);
    Task RevaluateMaterialAsync(string materialCode, string plant, Guid tenantId, string userId);
    Task<List<ValuationLedgerDto>> GetValuationLedgerAsync(string materialCode, string plant, DateTime fromDate, DateTime toDate, Guid tenantId);
}

public class ReservationHeaderDto
{
    public string ReservationNumber { get; set; } = "";
    public DateTime RequirementDate { get; set; }
    public string? CostCenter { get; set; }
    public string? OrderNumber { get; set; }
    public string Plant { get; set; } = "";
    public decimal TotalReservedQuantity { get; set; }
    public decimal TotalIssuedQuantity { get; set; }
    public bool IsCompleted { get; set; }
    public string Status { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ValuationLedgerDto
{
    public string MaterialCode { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string? BatchNumber { get; set; }
    public DateTime ValuationDate { get; set; }
    public string DocumentNumber { get; set; } = "";
    public decimal OpeningQuantity { get; set; }
    public decimal OpeningValue { get; set; }
    public decimal ClosingQuantity { get; set; }
    public decimal ClosingValue { get; set; }
    public decimal MovingAveragePrice { get; set; }
    public string PriceControl { get; set; } = "";
}
