using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IStockOverviewService
{
    Task<StockOverviewResultDto> GetStockOverviewHierarchyAsync(StockOverviewFilterDto filter, Guid tenantId);
}
